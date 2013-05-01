/*
 | Version 10.1.1
 | Copyright 2012 Esri
 |
 | Licensed under the Apache License, Version 2.0 (the "License");
 | you may not use this file except in compliance with the License.
 | You may obtain a copy of the License at
 |
 |    http://www.apache.org/licenses/LICENSE-2.0
 |
 | Unless required by applicable law or agreed to in writing, software
 | distributed under the License is distributed on an "AS IS" BASIS,
 | WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 | See the License for the specific language governing permissions and
 | limitations under the License.
 */


using System;
using System.Collections.Generic;
using System.Text;

using ESRI.ArcGIS.Geodatabase;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Core;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.Editor;

namespace Esri_Telecom_Tools.Helpers
{
    /// <summary>
    /// This class does all the real work so that we can change the UI more easily if necessary
    /// </summary>
    public class FiberSpliceHelper
    {
        private static FiberSpliceHelper _instance = null;
        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private HookHelperExt _hookHelper = null;
        private IEditor3 _editor = null;

        /// <summary>
        /// Constructs a new FiberSpliceConfigHelper
        /// </summary>
        /// <param name="hook">Hook</param>
        /// <param name="editor">Editor</param>
        public FiberSpliceHelper(HookHelperExt hookHelper, IEditor3 editor)
//            : base(hook)
//            : base(hook, editor)
        {
            _hookHelper = hookHelper;
            _editor = editor;
        }

        //public static FiberSpliceConnectionHelper Instance(object hook, ESRI.ArcGIS.Editor.IEditor editor)
        //{
        //    if (_instance != null)
        //    {
        //        return _instance;
        //    }
        //    else
        //    {
        //        _instance = new FiberSpliceConnectionHelper(hook, editor);
        //        return _instance;
        //    }
        //}


        /// <summary>
        /// Gets a list of cable features that are connected to the splice closure in the geometric network
        /// </summary>
        /// <param name="spliceClosure">Splice to check</param>
        /// <returns>List of IFeature</returns>
        public List<ESRI.ArcGIS.Geodatabase.IFeature> GetConnectedCables(SpliceClosureWrapper spliceClosure)
        {
            // At the time of this comment, splicable means they are connected in the 
            // geometric network and the splice closure "touches" one of the ends of the cable
            List<ESRI.ArcGIS.Geodatabase.IFeature> result = new List<ESRI.ArcGIS.Geodatabase.IFeature>();

            if (null == spliceClosure)
            {
                throw new ArgumentNullException("spliceClosure");
            }

            ESRI.ArcGIS.Geometry.IRelationalOperator relOp = spliceClosure.Feature.Shape as ESRI.ArcGIS.Geometry.IRelationalOperator;
            if (relOp == null)
                throw new ArgumentNullException("spliceClosure");

            ESRI.ArcGIS.Geodatabase.ISimpleJunctionFeature junction = spliceClosure.Feature as ESRI.ArcGIS.Geodatabase.ISimpleJunctionFeature;
            if (null != junction)
            {
                for (int i = 0; i < junction.EdgeFeatureCount; i++)
                {
                    ESRI.ArcGIS.Geodatabase.IFeature connectedEdge = junction.get_EdgeFeature(i) as ESRI.ArcGIS.Geodatabase.IFeature;
                    ESRI.ArcGIS.Geodatabase.IDataset dataset = connectedEdge.Class as ESRI.ArcGIS.Geodatabase.IDataset;
                    string ftClassName = GdbUtils.ParseTableName(dataset);

                    if (0 == string.Compare(ftClassName, ConfigUtil.FiberCableFtClassName, true))
                    {
                        // Test feature edge is coincident with splice closure. Cables are 
                        // complex features so splice might lie half way along some cables 
                        // but will not be providing any splice connectivity to them.
                        if (relOp.Touches(connectedEdge.Shape)) // only true if point at either end of a line
                            result.Add(connectedEdge);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all cables that are considered splicable to another cable at a given splice closure. 
        /// </summary>
        /// <param name="cable">Cable to splice with</param>
        /// <param name="spliceClosure">Splice Closure to splice at</param>
        /// <returns>List of SpliceableCableWrapper</returns>
        public List<SpliceableCableWrapper> GetSpliceableCables(FiberCableWrapper cable, SpliceClosureWrapper spliceClosure)
        {
            // At the time of this comment, splicable means they are connected in the geometric network
            List<SpliceableCableWrapper> result = new List<SpliceableCableWrapper>();

            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }
            if (null == spliceClosure)
            {
                throw new ArgumentNullException("spliceClosure");
            }

            int searchOid = cable.Feature.OID;

            ESRI.ArcGIS.Geodatabase.IEdgeFeature cableFt = cable.Feature as ESRI.ArcGIS.Geodatabase.IEdgeFeature;
            ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName);
            int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

            if (null != cableFt)
            {
                // We assume it is simple. Complex junctions are not supported for splicing cables
                ESRI.ArcGIS.Geodatabase.ISimpleJunctionFeature junction = spliceClosure.Feature as ESRI.ArcGIS.Geodatabase.ISimpleJunctionFeature;

                if (null != junction)
                {
                    bool isAFromEnd = false; // would be in the case that junction.EID == cableFt.ToJunctionEID
                    if (junction.EID == cableFt.FromJunctionEID)
                    {
                        isAFromEnd = true;
                    }
                    else if (junction.EID != cableFt.ToJunctionEID)
                    {
                        // It isn't the from or the two? It shouldn't have been passed in as if it was coincident with the cable
                        return result;
//                        throw new InvalidOperationException("Given splice closure is not the junction for the given cable.");
                    }

                    int edgeCount = junction.EdgeFeatureCount;
                    for (int i = 0; i < edgeCount; i++)
                    {
                        ESRI.ArcGIS.Geodatabase.IEdgeFeature connectedEdge = junction.get_EdgeFeature(i);
                        ESRI.ArcGIS.Geodatabase.IFeature feature = (ESRI.ArcGIS.Geodatabase.IFeature)connectedEdge;
                        ESRI.ArcGIS.Geodatabase.IDataset dataset = feature.Class as ESRI.ArcGIS.Geodatabase.IDataset;
                        string featureClassName = GdbUtils.ParseTableName(dataset);

                        if (0 == string.Compare(featureClassName, ConfigUtil.FiberCableFtClassName)
                            && feature.OID != searchOid)
                        {
                            if (junction.EID == connectedEdge.FromJunctionEID)
                            {
                                result.Add(new SpliceableCableWrapper(feature, true, isAFromEnd, displayIdx));
                            }
                            else if (junction.EID == connectedEdge.ToJunctionEID)
                            {
                                result.Add(new SpliceableCableWrapper(feature, false, isAFromEnd, displayIdx));
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes all splices for a cable to any other at any splice closure
        /// </summary>
        /// <param name="cable">Cable</param>
        /// <param name="isExistingOperation">Are we already in an edit operation?</param>
        /// <returns>Success</returns>
        public bool BreakAllSplices(FiberCableWrapper cable, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            if (!isExistingOperation)
            {
                _editor.StartOperation();
                isOperationOpen = true;
            }

            try
            {
                ESRI.ArcGIS.Geodatabase.ITable spliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable spliceTable = GdbUtils.GetTable(cable.Feature.Class, ConfigUtil.FiberSpliceTableName);
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();

                // A and B is arbitrary, so we check the given combinations going both ways. The structure is:
                // Where either the A or B cableID is our cable ID
                filter.WhereClause = string.Format("{0}='{1}' OR {2}='{1}'",  
                    ConfigUtil.ACableIdFieldName,
                    cable.IPID,
                    ConfigUtil.BCableIdFieldName);
                    
                spliceTable.DeleteSearchedRows(filter);
                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(filter);

                if (isOperationOpen)
                {
                    _editor.StopOperation("Break Splices");
                    isOperationOpen = false;
                }
                
                success = true;
            }
            catch
            {
                if (isOperationOpen)
                {
                    _editor.AbortOperation();
                }

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Deletes all splices at a splice closure
        /// </summary>
        /// <param name="splice">Splice</param>
        /// <param name="isExistingOperation">Are we already in an edit operation?</param>
        /// <returns>Success</returns>
        public bool BreakAllSplices(SpliceClosureWrapper splice, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == splice)
            {
                throw new ArgumentNullException("splice");
            }

            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            if (!isExistingOperation)
            {
                _editor.StartOperation();
                isOperationOpen = true;
            }

            try
            {
                ESRI.ArcGIS.Geodatabase.ITable spliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable spliceTable = GdbUtils.GetTable(splice.Feature.Class, ConfigUtil.FiberSpliceTableName);
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();

                // A and B is arbitrary, so we check the given combinations going both ways. The structure is:
                // Where either the A or B cableID is our cable ID
                filter.WhereClause = string.Format("{0}='{1}'",
                    ConfigUtil.SpliceClosureIpidFieldName,    
                    splice.IPID);

                spliceTable.DeleteSearchedRows(filter);
                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(filter);

                if (isOperationOpen)
                {
                    _editor.StopOperation("Break Splices");
                    isOperationOpen = false;
                }

                success = true;
            }
            catch
            {
                if (isOperationOpen)
                {
                    _editor.AbortOperation();
                }

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Deletes given splices from one cable to the other, at a given splice closure
        /// </summary>
        /// <param name="cableA">Cable A</param>
        /// <param name="cableB">Cable B</param>
        /// <param name="splice">Splice Closure</param>
        /// <param name="strands">Strands to remove</param>
        /// <param name="isExistingOperation">Flag to control whether we need to wrap this in an edit operation</param>
        /// <returns>Success</returns>
        public bool BreakSplices(FiberCableWrapper cableA, SpliceableCableWrapper cableB, SpliceClosureWrapper splice, Dictionary<int,FiberSplice> strands, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == cableA)
            {
                throw new ArgumentNullException("cableA");
            }

            if (null == cableB)
            {
                throw new ArgumentNullException("cableB");
            }

            if (null == splice)
            {
                throw new ArgumentNullException("splice");
            }

            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            if (!isExistingOperation)
            {
                _editor.StartOperation();
                isOperationOpen = true;
            }

            try
            {
                ESRI.ArcGIS.Geodatabase.ITable spliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable spliceTable = GdbUtils.GetTable(splice.Feature.Class, ConfigUtil.FiberSpliceTableName);

                using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                {
                    ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                    releaser.ManageLifetime(filter);

                    // A and B is arbitrary, so we check the given combinations going both ways. The structure is:
                    // Where the splice closure IPID is ours and 
                    // ((the A Cable/Fiber matches our A cable and B Cable/Fiber matches our B)
                    //  OR (the A Cable/Fiber matches our B cable and B Cable/Fiber matches our A))
                    string format = "{0}='{1}' AND (({2}='{3}' AND {4}={5} AND {6}='{7}' AND {8}={9})" +
                        " OR ({2}='{7}' AND {4}={9} AND {6}='{3}' AND {8}={5}))";

                    foreach (KeyValuePair<int, FiberSplice> pair in strands)
                    {
                        filter.WhereClause = string.Format(format, 
                            ConfigUtil.SpliceClosureIpidFieldName, 
                            splice.IPID, 
                            ConfigUtil.ACableIdFieldName,
                            cableA.IPID, 
                            ConfigUtil.AFiberNumberFieldName,
                            pair.Key, 
                            ConfigUtil.BCableIdFieldName,
                            cableB.IPID, 
                            ConfigUtil.BFiberNumberFieldName,
                            pair.Value.BRange.Low);

                        spliceTable.DeleteSearchedRows(filter);
                    }

                    if (isOperationOpen)
                    {
                        _editor.StopOperation("Break Splices");
                        isOperationOpen = false;
                    }
                    
                    success = true;
                }
            }
            catch
            {
                if (isOperationOpen)
                {
                    _editor.AbortOperation();
                }

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Splices all available strands from one cable to the other, at a given splice closure
        /// </summary>
        /// <param name="cableA">Cable A</param>
        /// <param name="cableB">Cable B</param>
        /// <param name="splice">Splice Closure</param>
        /// <param name="strands">Strands to splice together</param>
        /// <param name="isExistingOperation">Flag to control whether we need to wrap this in an edit operation</param>
        /// <returns>Success</returns>
        public bool CreateSplices(FiberCableWrapper cableA, SpliceableCableWrapper cableB, SpliceClosureWrapper splice, Dictionary<int,FiberSplice> strands, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == cableA)
            {
                throw new ArgumentNullException("cableA");
            }

            if (null == cableB)
            {
                throw new ArgumentNullException("cableB");
            }

            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            if (!isExistingOperation)
            {
                _editor.StartOperation();
                isOperationOpen = true;
            }

            
            if (null == splice)
            {
                splice = GenerateSpliceClosure(cableA, cableB);
            }
            else if (0 == splice.IPID.Length)
            {
                // Populate an IPID; we will need it
                ESRI.ArcGIS.Geodatabase.IFeature spliceFt = splice.Feature;
                Guid g = Guid.NewGuid();

                spliceFt.set_Value(spliceFt.Fields.FindField(ConfigUtil.IpidFieldName), g.ToString("B").ToUpper());
                spliceFt.Store();
            }

            try
            {
                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = GdbUtils.GetTable(cableA.Feature.Class, ConfigUtil.FiberSpliceTableName);
                int aCableIdx = fiberSpliceTable.FindField(ConfigUtil.ACableIdFieldName);
                int bCableIdx = fiberSpliceTable.FindField(ConfigUtil.BCableIdFieldName);
                int aFiberNumIdx = fiberSpliceTable.FindField(ConfigUtil.AFiberNumberFieldName);
                int bFiberNumIdx = fiberSpliceTable.FindField(ConfigUtil.BFiberNumberFieldName);
                int spliceIpidIdx = fiberSpliceTable.FindField(ConfigUtil.SpliceClosureIpidFieldName);
                int isAFromIdx = fiberSpliceTable.FindField(ConfigUtil.IsAFromEndFieldName);
                int isBFromIdx = fiberSpliceTable.FindField(ConfigUtil.IsBFromEndFieldName);
                int lossIdx = fiberSpliceTable.FindField(ConfigUtil.LossFieldName);
                int typeIdx = fiberSpliceTable.FindField(ConfigUtil.TypeFieldName);
                ESRI.ArcGIS.Geodatabase.IField typeField = fiberSpliceTable.Fields.get_Field(typeIdx);

                string aCableId = cableA.IPID;
                string bCableId = cableB.IPID; 
                string isAFromEnd = cableB.IsOtherFromEnd ? "T" : "F";
                string isBFromEnd = cableB.IsThisFromEnd ? "T" : "F";
                string spliceIpid = splice.IPID;

                using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                {
                    // TODO cant use insert cursor since edit events wont fire.
                    // We need evetns to fire for dynamic values to get populated.
                    // ESRI.ArcGIS.Geodatabase.ICursor insertCursor = fiberSpliceTable.Insert(true);
                    // releaser.ManageLifetime(insertCursor);

                    foreach (KeyValuePair<int, FiberSplice> pair in strands)
                    {
                        IRow row = fiberSpliceTable.CreateRow();

                        releaser.ManageLifetime(row);

                        FiberSplice fiberSplice = pair.Value;

                        row.set_Value(aCableIdx, aCableId);
                        row.set_Value(bCableIdx, bCableId);
                        row.set_Value(aFiberNumIdx, pair.Key);
                        row.set_Value(bFiberNumIdx, fiberSplice.BRange.Low);
                        row.set_Value(spliceIpidIdx, spliceIpid);
                        row.set_Value(isAFromIdx, isAFromEnd);
                        row.set_Value(isBFromIdx, isBFromEnd);

                        if (null == fiberSplice.Loss)
                        {
                            row.set_Value(lossIdx, DBNull.Value);
                        }
                        else
                        {
                            row.set_Value(lossIdx, fiberSplice.Loss);
                        }

                        object typeValue = DBNull.Value;
                        if (null != fiberSplice.Type)
                        {
                            try
                            {
                                typeValue = GdbUtils.CheckForCodedValue(typeField, fiberSplice.Type);
                            }
                            catch
                            {
                                // TODO: Log a warning about why we can't set the default split splice type?
                            }
                        }

                        row.set_Value(typeIdx, typeValue);
                        row.Store();
                    }
                }

                if (isOperationOpen)
                {
                    _editor.StopOperation("Edit Splices");
                }
                
                success = true;
            }
            catch
            {
                if (isOperationOpen)
                {
                    _editor.AbortOperation();
                }

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Generates a splice closure at the coincident endpoint. Does not start an edit operation.
        /// </summary>
        /// <param name="cableA">Cable A</param>
        /// <param name="cableB">Cable B</param>
        /// <returns>SpliceClosureWRapper based on new feature</returns>
        private SpliceClosureWrapper GenerateSpliceClosure(FiberCableWrapper cableA, SpliceableCableWrapper cableB)
        {
            SpliceClosureWrapper wrapper = null;

            #region Validation
            if (null == cableA)
            {
                throw new ArgumentNullException("cableA");
            }

            if (null == cableB)
            {
                throw new ArgumentNullException("cableB");
            }

            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            // Populate an IPID; we will need it
            Guid g = Guid.NewGuid();
            string spliceIpid = g.ToString("B").ToUpper();

            try
            {
                ESRI.ArcGIS.Geometry.IPolyline line = cableB.Feature.Shape as ESRI.ArcGIS.Geometry.IPolyline;
                if (null != line)
                {
                    ESRI.ArcGIS.Geometry.IPoint splicePoint = null;

                    if (cableB.IsThisFromEnd)
                    {
                        splicePoint = line.FromPoint;
                    }
                    else
                    {
                        splicePoint = line.ToPoint;
                    }

                    ESRI.ArcGIS.Geodatabase.IFeatureClass spliceClosureFtClass = _wkspHelper.FindFeatureClass(ConfigUtil.SpliceClosureFtClassName);
//                    ESRI.ArcGIS.Geodatabase.IFeatureClass spliceClosureFtClass = GdbUtils.GetFeatureClass(cableA.Feature.Class, ConfigUtil.SpliceClosureFtClassName);
                    ESRI.ArcGIS.Geodatabase.IFeature spliceFt = spliceClosureFtClass.CreateFeature();

                    spliceFt.Shape = splicePoint;
                    spliceFt.set_Value(spliceClosureFtClass.Fields.FindField(ConfigUtil.IpidFieldName), spliceIpid);
                    spliceFt.Store();

                    wrapper = new SpliceClosureWrapper(spliceFt);
                }
            }
            catch
            {
                wrapper = null;
            }

            return wrapper;
        }
    }
}
