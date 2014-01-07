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
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Geodatabase;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Core;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.Carto;

namespace Esri_Telecom_Tools.Helpers 
{
    public class FiberTraceHelper
    {
        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private HookHelperExt _hookHelper = null;

        public event EventHandler TraceCompleted;
        public event EventHandler SelectionChanged;

        List<ESRI.ArcGIS.Geodatabase.IRow> _traceResults = new List<ESRI.ArcGIS.Geodatabase.IRow>();

        private int _aCableIdx = -1;
        private int _bCableIdx = -1;
        private int _aFiberNumIdx = -1;
        private int _bFiberNumIdx = -1;
        private int _isAFromIdx = -1;
        private int _isBFromIdx = -1;
        private int _spliceClosureIpidIdx = -1;

        private bool _startedOnFiber = true;
        private int _startFiberIdx = 0;


        /// <summary>
        /// Constructs a new FiberTraceHelper
        /// </summary>
        /// <param name="hookHelper">HookHelper</param>
        /// 
        public FiberTraceHelper(HookHelperExt hookHelper)
        {
            _hookHelper = hookHelper;

            // -------------------------------------------------
            // Listen for selections only when a valid telecom 
            // workspace has been opened. 
            // -------------------------------------------------
            _wkspHelper.ValidWorkspaceSelected += new EventHandler(_wkspHelper_ValidWorkspaceSelected);
            _wkspHelper.WorkspaceClosed += new EventHandler(_wkspHelper_WorkspaceClosed);
        }

        void _wkspHelper_WorkspaceClosed(object sender, EventArgs e)
        {
            _hookHelper.ActiveViewSelectionChanged -= new IActiveViewEvents_SelectionChangedEventHandler(_hookHelper_ActiveViewSelectionChanged);
        }

        void _wkspHelper_ValidWorkspaceSelected(object sender, EventArgs e)
        {
            _hookHelper.ActiveViewSelectionChanged += new IActiveViewEvents_SelectionChangedEventHandler(_hookHelper_ActiveViewSelectionChanged);
        }

        public List<ESRI.ArcGIS.Geodatabase.IRow> TraceResults
        {
            get
            {
                return _traceResults;
            }
        }

        public bool StartedOnFiber
        {
            get
            {
                return _startedOnFiber;
            }
        }

        public int StartedFiberIndex
        {
            get
            {
                return _startFiberIdx;
            }
        }

        /// <summary>
        /// Ripple down from the desired start point tracing and highlighting the results.
        /// </summary>
        public void TraceTriggered(FeatureWrapper feature, int unit, PortType port = PortType.Input)
        {
            try
            {
                if (null == feature)
                {
                    throw new ArgumentNullException("FeatureWrapper");
                }

                FiberCableWrapper fiberCableWrapper = feature as FiberCableWrapper;
                DeviceWrapper deviceWrapper = feature as DeviceWrapper;
                int fiberNumber = unit;
                
                _startedOnFiber = true;
                if (null != deviceWrapper)
                {
                    fiberCableWrapper = GetConnectedFiber(deviceWrapper, unit, port, out fiberNumber);
                    _startedOnFiber = false;
                }

                List<Range> traceRange = new List<Range>(new Range[] { new Range(fiberNumber, fiberNumber) });

                if (null != fiberCableWrapper)
                {
                    if (SpliceAndConnectionUtils.AreRangesWithinFiberCount(traceRange, fiberCableWrapper))
                    {
                        ESRI.ArcGIS.Geodatabase.IFeature ft = fiberCableWrapper.Feature;

                        _traceResults.Clear();
                        _traceResults = TracePath(ft, fiberNumber, true);
                        _traceResults.Reverse(); // This went down the "from end", so they are backwards

                        _startFiberIdx = _traceResults.Count;

                        // Now add ourselves
                        _traceResults.Add(ft);
                        _traceResults.Add(GetFiberRecord(ft, fiberNumber));

                        // Now add everything going the other way
                        List<ESRI.ArcGIS.Geodatabase.IRow> resultsAtToEnd = TracePath(ft, fiberNumber, false);
                        _traceResults.AddRange(resultsAtToEnd);

                        if (TraceCompleted != null)
                        {
                            TraceCompleted(this, null);
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Fiber strand number is not within the fiber cable's number of fibers.", "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("No fiber cable / strand was specified, or none was connected to the specified port.", "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }

                // ---------------------------------------
                // This causes a refresh of the selection 
                // and we see the results of the trace on 
                // the map
                // ---------------------------------------
                _hookHelper.ActiveView.PartialRefresh(ESRI.ArcGIS.Carto.esriViewDrawPhase.esriViewGeoSelection, null, null);
 //               ActiveView.PartialRefresh(ESRI.ArcGIS.Carto.esriViewDrawPhase.esriViewGeoSelection, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "ERROR");
                System.Diagnostics.Trace.WriteLine(ex.StackTrace, "DETAILS");
            }
            finally
            {
            }
        }

        public void SelectTracedDevices()
        {
            // -----------------------------------------------
            // Following section of code causes cable, splice 
            // and device features to be selected on the map 
            // using the IFeatureSelection & ISelectionSet 
            // interfaces
            // -----------------------------------------------
            
            // Remove any previous trace results.
            _hookHelper.FocusMap.ClearSelection();

            Dictionary<string, List<int>> deviceOidLists = new Dictionary<string, List<int>>();

            // First get set of OIDs that were traced.
            foreach (IRow traceItem in this._traceResults)
            {
                ESRI.ArcGIS.Geodatabase.IDataset dataset = traceItem.Table as ESRI.ArcGIS.Geodatabase.IDataset;
                string className = GdbUtils.ParseTableName(dataset);
                if(ConfigUtil.IsDeviceClassName(className))
                {
                    List<int> deviceOids = null;
                    if (deviceOidLists.ContainsKey(className))
                    {
                        deviceOids = deviceOidLists[className];
                    }
                    else
                    {
                        deviceOids = new List<int>();
                        deviceOidLists[className] = deviceOids;
                    }
                    deviceOids.Add(traceItem.OID);
                }
            }

            // Do the actual selections
            foreach (KeyValuePair<string, List<int>> deviceOidPair in deviceOidLists)
            {
                ESRI.ArcGIS.Carto.IFeatureSelection deviceFtSelection = _hookHelper.FindFeatureLayer(deviceOidPair.Key) as ESRI.ArcGIS.Carto.IFeatureSelection;
                if (null != deviceFtSelection)
                {
                    ESRI.ArcGIS.Geodatabase.ISelectionSet deviceSelectionSet = deviceFtSelection.SelectionSet;
                    List<int> deviceOidList = deviceOidPair.Value;
                    if (null != deviceSelectionSet && 0 < deviceOidList.Count)
                    {
                        int[] oidList = deviceOidList.ToArray();
                        deviceSelectionSet.AddList(deviceOidList.Count, ref oidList[0]);
                    }
                }
                else
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", deviceOidPair.Key + " not found.", "Layer removed from TOC?");
                }
            }
        }

        public void SelectTracedFiberCables()
        {
            // -----------------------------------------------
            // Following section of code causes cable, splice 
            // and device features to be selected on the map 
            // using the IFeatureSelection & ISelectionSet 
            // interfaces
            // -----------------------------------------------

            List<int> cableOidList = new List<int>();

            // First get set of OIDs that were traced.
            foreach (IRow traceItem in this._traceResults)
            {
                ESRI.ArcGIS.Geodatabase.IDataset dataset = traceItem.Table as ESRI.ArcGIS.Geodatabase.IDataset;
                string className = GdbUtils.ParseTableName(dataset);
                if (0 == string.Compare(className, ConfigUtil.FiberCableFtClassName, true))
                {
                    if (traceItem.HasOID) cableOidList.Add(traceItem.OID);
                }
            }

            // Do the actual selection
            ESRI.ArcGIS.Carto.IFeatureSelection cableFtSelection = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName) as ESRI.ArcGIS.Carto.IFeatureSelection;
            if (cableFtSelection != null)
            {
                ESRI.ArcGIS.Geodatabase.ISelectionSet cableSelectionSet = cableFtSelection.SelectionSet;
                if (null != cableSelectionSet
                    && 0 < cableOidList.Count)
                {
                    int[] oidList = cableOidList.ToArray();
                    cableSelectionSet.AddList(cableOidList.Count, ref oidList[0]);
                }
            }
            else
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", ConfigUtil.FiberCableFtClassName + " not found.", "Layer removed from TOC?");
            }
        }

        public void SelectTracedSpliceClosures()
        {
            // -----------------------------------------------
            // Following section of code causes cable, splice 
            // and device features to be selected on the map 
            // using the IFeatureSelection & ISelectionSet 
            // interfaces
            // -----------------------------------------------

            List<int> spliceOidList = new List<int>();
            
            // First get set of OIDs that were traced.
            foreach (IRow traceItem in this._traceResults)
            {
                ESRI.ArcGIS.Geodatabase.IDataset dataset = traceItem.Table as ESRI.ArcGIS.Geodatabase.IDataset;
                string className = GdbUtils.ParseTableName(dataset);
                if (0 == string.Compare(className, ConfigUtil.SpliceClosureFtClassName, true))
                {
                    if(traceItem.HasOID) spliceOidList.Add(traceItem.OID);
                }
            }

            // Do the actual selection
            ESRI.ArcGIS.Carto.IFeatureSelection scFtSelection = _hookHelper.FindFeatureLayer(ConfigUtil.SpliceClosureFtClassName) as ESRI.ArcGIS.Carto.IFeatureSelection;
            if (scFtSelection == null)
            {
                // No selection to be made as layer not in TOC
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", ConfigUtil.SpliceClosureFtClassName + " not found.", "Layer removed from TOC?");
            }
            else
            {
                ESRI.ArcGIS.Geodatabase.ISelectionSet scSelectionSet = scFtSelection.SelectionSet;
                if (null != scSelectionSet
                    && 0 < spliceOidList.Count)
                {
                    int[] oidList = spliceOidList.ToArray();
                    scSelectionSet.AddList(spliceOidList.Count, ref oidList[0]);
                }
            }
        }

        ///// <summary>
        ///// The active view has refreshed. Redraw our results, if we have any
        ///// </summary>
        ///// <param name="Display">Display to draw on</param>
        ///// <param name="phase"></param>
        //private void _arcMapWrapper_ActiveViewAfterDraw(ESRI.ArcGIS.Display.IDisplay Display, ESRI.ArcGIS.Carto.esriViewDrawPhase phase)
        //{
        //    if (phase == ESRI.ArcGIS.Carto.esriViewDrawPhase.esriViewGeoSelection)
        //    {
        //        // Draw after the selection
        //        if (null != _currentResults)
        //        {
        //            ESRI.ArcGIS.Display.ILineSymbol lineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbol();
        //            ESRI.ArcGIS.Display.IRgbColor color = new ESRI.ArcGIS.Display.RgbColorClass();
        //            color.Red = 255;
        //            color.Green = 0;
        //            color.Blue = 0;

        //            lineSymbol.Color = color;
        //            lineSymbol.Width = 4;

        //            ESRI.ArcGIS.Display.ISimpleMarkerSymbol markerSymbol = new ESRI.ArcGIS.Display.SimpleMarkerSymbolClass();
        //            markerSymbol.Color = color;
        //            markerSymbol.Style = ESRI.ArcGIS.Display.esriSimpleMarkerStyle.esriSMSCircle;
        //            markerSymbol.Size = 6;

        //            for (int i = 0; i < _currentResults.Count; i++)
        //            {
        //                ESRI.ArcGIS.Geometry.IGeometry geometry = _currentResults[i];
        //                if (geometry is ESRI.ArcGIS.Geometry.IPolyline)
        //                {
        //                    Display.SetSymbol((ESRI.ArcGIS.Display.ISymbol)lineSymbol);
        //                    Display.DrawPolyline((ESRI.ArcGIS.Geometry.IPolyline)geometry);
        //                }
        //                else if (geometry is ESRI.ArcGIS.Geometry.IPoint)
        //                {
        //                    Display.SetSymbol((ESRI.ArcGIS.Display.ISymbol)markerSymbol);
        //                    Display.DrawPoint((ESRI.ArcGIS.Geometry.IPoint)geometry);
        //                }
        //            }
        //        }
        //    }
        //}

        private List<ESRI.ArcGIS.Geodatabase.IRow> TracePath(ESRI.ArcGIS.Geodatabase.IFeature cableFeature, int fiberNumber, bool isStartingAtFromEnd)
        {
            List<ESRI.ArcGIS.Geodatabase.IRow> result = new List<ESRI.ArcGIS.Geodatabase.IRow>();

            string ipid = cableFeature.get_Value(cableFeature.Fields.FindField(ConfigUtil.IpidFieldName)).ToString();

            ESRI.ArcGIS.Geodatabase.IFeatureClass cableFtClass = (ESRI.ArcGIS.Geodatabase.IFeatureClass)cableFeature.Class;

            ESRI.ArcGIS.Geodatabase.IFeatureClass spliceFtClass = _wkspHelper.FindFeatureClass(ConfigUtil.SpliceClosureFtClassName);
            ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);

            ESRI.ArcGIS.Geodatabase.IFields spliceFields = fiberSpliceTable.Fields;

            string fiberClassName = ConfigUtil.FiberTableName;
            ESRI.ArcGIS.Geodatabase.IRelationshipClass fiberRelationship = GdbUtils.GetRelationshipClass(cableFtClass, ConfigUtil.FiberCableToFiberRelClassName);
            if (null != fiberRelationship && null != fiberRelationship.DestinationClass)
            {
                fiberClassName = GdbUtils.ParseTableName(fiberRelationship.DestinationClass as ESRI.ArcGIS.Geodatabase.IDataset);
            }

            ESRI.ArcGIS.Geodatabase.ITable fiberTable = _wkspHelper.FindTable(fiberClassName);

            _aCableIdx = spliceFields.FindField(ConfigUtil.ACableIdFieldName);
            _bCableIdx = spliceFields.FindField(ConfigUtil.BCableIdFieldName);
            _aFiberNumIdx = spliceFields.FindField(ConfigUtil.AFiberNumberFieldName);
            _bFiberNumIdx = spliceFields.FindField(ConfigUtil.BFiberNumberFieldName);
            _isAFromIdx = spliceFields.FindField(ConfigUtil.IsAFromEndFieldName);
            _isBFromIdx = spliceFields.FindField(ConfigUtil.IsBFromEndFieldName);
            _spliceClosureIpidIdx = spliceFields.FindField(ConfigUtil.SpliceClosureIpidFieldName);

            ESRI.ArcGIS.Geodatabase.IQueryFilter spliceFilter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
            spliceFilter.WhereClause = string.Format("({0}='{1}' AND {2}={3})"
                + " OR ({4}='{1}' AND {5}={3})",
                ConfigUtil.ACableIdFieldName,
                ipid,
                ConfigUtil.AFiberNumberFieldName,
                fiberNumber,
                ConfigUtil.BCableIdFieldName,
                ConfigUtil.BFiberNumberFieldName);

            int connections = fiberSpliceTable.RowCount(spliceFilter);
            if (2 < connections)
            {
                // TODO: warning?
                System.Windows.Forms.MessageBox.Show("Less than 2 connections were detected: " + fiberNumber, "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }

            string spliceClosureIpid = string.Empty;
            string nextCableId = string.Empty;
            int nextFiberNumber = -1;
            bool isNextFromEnd = false;

            // {{0}} causes the string.format to 
            string cableWhereFormat = string.Format("{0}='{{0}}'", ConfigUtil.IpidFieldName);
            string spliceWhereFormat = string.Format("{0}='{{0}}'", ConfigUtil.IpidFieldName);
            string fiberWhereFormat = string.Format("{0}='{{0}}' AND {1}={{1}}", fiberRelationship.OriginForeignKey, ConfigUtil.Fiber_NumberFieldName);

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                releaser.ManageLifetime(filter);

                // Ripple down the start cable's to end
                ESRI.ArcGIS.Geodatabase.IRow spliceRow = GetNextSplice(fiberSpliceTable, ipid, fiberNumber, isStartingAtFromEnd, out nextCableId, out nextFiberNumber, out spliceClosureIpid, out isNextFromEnd);
                while (null != spliceRow)
                {
                    ESRI.ArcGIS.Geodatabase.IFeature spliceClosure = null;

                    if (spliceClosureIpid.Equals(""))
                    {
                        System.Windows.Forms.MessageBox.Show("Found Splice with no SpliceClosure (ID/#) " + nextCableId + "/" + nextFiberNumber, "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    else
                    {
                        filter.WhereClause = string.Format(spliceWhereFormat, spliceClosureIpid);
                        ESRI.ArcGIS.Geodatabase.IFeatureCursor spliceCursor = spliceFtClass.Search(filter, false);
                        releaser.ManageLifetime(spliceCursor);
                        spliceClosure = spliceCursor.NextFeature();
                        if (spliceClosure == null)
                        {
                            System.Windows.Forms.MessageBox.Show("Invalid SpliceClosure referenced: (IPID)" + spliceClosureIpid, "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    }

                    filter.WhereClause = string.Format(cableWhereFormat, nextCableId);
                    ESRI.ArcGIS.Geodatabase.IFeatureCursor cableCursor = cableFtClass.Search(filter, false);
                    releaser.ManageLifetime(cableCursor);
                    ESRI.ArcGIS.Geodatabase.IFeature cable = cableCursor.NextFeature();
                    if (cable == null)
                    {
                        System.Windows.Forms.MessageBox.Show("Invalid cable ID referenced: (ID)" + nextCableId, "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }

                    filter.WhereClause = string.Format(fiberWhereFormat, nextCableId, nextFiberNumber);
                    ESRI.ArcGIS.Geodatabase.ICursor fiberCursor = fiberTable.Search(filter, false);
                    releaser.ManageLifetime(fiberCursor);
                    ESRI.ArcGIS.Geodatabase.IRow fiber = fiberCursor.NextRow();
                    if (fiber == null)
                    {
                        System.Windows.Forms.MessageBox.Show("Invalid Fiber Cable or # referenced: (ID/#) " + nextCableId + "/" + nextFiberNumber, "Telecom Trace", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }

                    if (isStartingAtFromEnd)
                    {
                        if (spliceRow != null) result.Add(spliceRow);
                        if (spliceClosure != null) result.Add(spliceClosure);
                        if (fiber != null) result.Add(fiber);
                        if (cable != null) result.Add(cable);
                    }
                    else
                    {
                        if (spliceClosure != null) result.Add(spliceClosure);
                        if (spliceRow != null) result.Add(spliceRow);
                        if (cable != null) result.Add(cable);
                        if (fiber != null) result.Add(fiber);
                    }

                    spliceRow = GetNextSplice(fiberSpliceTable, nextCableId, nextFiberNumber, !isNextFromEnd, out nextCableId, out nextFiberNumber, out spliceClosureIpid, out isNextFromEnd);
                }

                // See if there is a port for this one
                ESRI.ArcGIS.Geodatabase.IRow portRow = null;
                ESRI.ArcGIS.Geodatabase.IFeature deviceFt = null;
                if (GetConnectedPort(cableFtClass, nextCableId, nextFiberNumber, isNextFromEnd, out portRow, out deviceFt))
                {
                    if (isStartingAtFromEnd)
                    {
                        result.Add(portRow);
                        result.Add(deviceFt);
                    }
                    else
                    {
                        result.Add(deviceFt);
                        result.Add(portRow);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Looks for a fiber splice record at one end of a given cable
        /// </summary>
        /// <param name="fiberSpliceTable">Fiber splice table</param>
        /// <param name="cableId">Cable ID of the cable we are checking</param>
        /// <param name="fiberNumber">Fiber Number we are checking</param>
        /// <param name="checkFromEnd">Which end of the cable are we checking?</param>
        /// <param name="nextCableId">(out) Cable ID of the cable spliced on this end, or string.Empty if none</param>
        /// <param name="nextFiberNumber">(out) Fiber Number of spliced on this end, or -1 if none</param>
        /// <param name="spliceClosureIpid">(out) IPID of Splice Closure</param>
        /// <param name="isNextFromEnd">(out) Is the result cable spliced on its from end or its to end?</param>
        /// <returns>The splice record</returns>
        private ESRI.ArcGIS.Geodatabase.IRow GetNextSplice(ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable, string cableId, int fiberNumber, bool checkFromEnd, out string nextCableId, out int nextFiberNumber, out string spliceClosureIpid, out bool isNextFromEnd)
        {
            ESRI.ArcGIS.Geodatabase.IRow spliceRow = null;

            spliceClosureIpid = string.Empty;
            nextCableId = cableId;
            nextFiberNumber = fiberNumber;
            isNextFromEnd = checkFromEnd;

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                filter.WhereClause = string.Format("({0}='{1}' AND {2}={3} AND {4}='{5}')"
                    + " OR ({6}='{1}' AND {7}={3} AND {8}='{5}')",
                    ConfigUtil.ACableIdFieldName,
                    cableId,
                    ConfigUtil.AFiberNumberFieldName,
                    fiberNumber,
                    ConfigUtil.IsAFromEndFieldName,
                    (checkFromEnd ? "T" : "F"),
                    ConfigUtil.BCableIdFieldName,
                    ConfigUtil.BFiberNumberFieldName,
                    ConfigUtil.IsBFromEndFieldName);

                releaser.ManageLifetime(filter);

                // TODO: should we give a warning if the rowcount is more than 1? We should technically only find one splice
                // record on this end of the fiber...

                ESRI.ArcGIS.Geodatabase.ICursor search = fiberSpliceTable.Search(filter, false);
                releaser.ManageLifetime(search);

                spliceRow = search.NextRow();
                if (null != spliceRow)
                {
                    object scIpidValue = spliceRow.get_Value(_spliceClosureIpidIdx);
                    if (DBNull.Value != scIpidValue)
                    {
                        spliceClosureIpid = scIpidValue.ToString();
                    }

                    string aCableId = spliceRow.get_Value(_aCableIdx).ToString();
                    if (0 == string.Compare(aCableId, cableId))
                    {
                        // b is the one we want to return
                        nextCableId = spliceRow.get_Value(_bCableIdx).ToString();
                        nextFiberNumber = (int)spliceRow.get_Value(_bFiberNumIdx);
                        isNextFromEnd = spliceRow.get_Value(_isBFromIdx).ToString() == "T" ? true : false;
                    }
                    else
                    {
                        // a is the one we want to return
                        nextCableId = aCableId;
                        nextFiberNumber = (int)spliceRow.get_Value(_aFiberNumIdx);
                        isNextFromEnd = spliceRow.get_Value(_isAFromIdx).ToString() == "T" ? true : false;
                    }
                }
            }

            return spliceRow;
        }

        private FiberCableWrapper GetConnectedFiber(DeviceWrapper device, int portId, PortType portType, out int fiberNumber)
        {
            FiberCableWrapper result = null;
            fiberNumber = -1;

            ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = (ESRI.ArcGIS.Geodatabase.IFeatureClass)device.Feature.Class;
            ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
            if (null == deviceHasPorts)
            {
                throw new Exception("Device to port relationship is missing or cannot be opened.");
            }

            ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
            if (null == portTable)
            {
                throw new Exception("Port table is missing or cannot be opened.");
            }


            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                releaser.ManageLifetime(filter);
                filter.WhereClause = string.Format("{0}='{1}' AND {2}={3} AND {4}='{5}'",
                    deviceHasPorts.OriginForeignKey,
                    device.Feature.get_Value(deviceFtClass.FindField(deviceHasPorts.OriginPrimaryKey)),
                    ConfigUtil.PortIdFieldName,
                    portId,
                    ConfigUtil.PortTypeFieldName,
                    PortType.Input == portType ? 1 : 2);

                ESRI.ArcGIS.Geodatabase.ICursor cursor = portTable.Search(filter, false);
                releaser.ManageLifetime(cursor);
                ESRI.ArcGIS.Geodatabase.IRow portRow = cursor.NextRow();

                if (null != portRow)
                {
                    //releaser.ManageLifetime(portRow);

                    object cableIdValue = portRow.get_Value(portTable.FindField(ConfigUtil.ConnectedCableFieldName));
                    if (DBNull.Value != cableIdValue)
                    {
                        ESRI.ArcGIS.Geodatabase.IFeatureClass cableFtClass = _wkspHelper.FindFeatureClass(ConfigUtil.FiberCableFtClassName);
                        filter.WhereClause = string.Format("{0}='{1}'", ConfigUtil.IpidFieldName, cableIdValue);
                        ESRI.ArcGIS.Geodatabase.IFeatureCursor cableCursor = cableFtClass.Search(filter, false);
                        releaser.ManageLifetime(cableCursor);

                        ESRI.ArcGIS.Geodatabase.IFeature cable = cableCursor.NextFeature();
                        if (null != cable)
                        {
                            result = new FiberCableWrapper(cable);
                            object fiberNumberValue = portRow.get_Value(portTable.FindField(ConfigUtil.ConnectedFiberFieldName));
                            if (DBNull.Value != fiberNumberValue)
                            {
                                int.TryParse(fiberNumberValue.ToString(), out fiberNumber);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the connected device/port
        /// </summary>
        /// <param name="siblingFtClass">Any feature class from the workspace</param>
        /// <param name="cableId">Cable ID to check connx for</param>
        /// <param name="fiberNumber">Fiber Number to check connx for</param>
        /// <param name="isFromEnd">Whether to check the cable's from or to end</param>
        /// <param name="portRow">(out) result port</param>
        /// <param name="deviceFt">(out) result device</param>
        /// <returns>True if a connx was found</returns>
        private bool GetConnectedPort(ESRI.ArcGIS.Geodatabase.IFeatureClass siblingFtClass, string cableId, int fiberNumber, bool isFromEnd, out ESRI.ArcGIS.Geodatabase.IRow portRow, out ESRI.ArcGIS.Geodatabase.IFeature deviceFt)
        {
            portRow = null;
            deviceFt = null;

            bool result = false;

            string[] portTableNames = ConfigUtil.PortTableNames;
            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                filter.WhereClause = string.Format("{0}='{1}' AND {2}={3} AND {4}='{5}'",
                    ConfigUtil.ConnectedCableFieldName,
                    cableId,
                    ConfigUtil.ConnectedFiberFieldName,
                    fiberNumber,
                    ConfigUtil.ConnectedEndFieldName,
                    isFromEnd ? "T" : "F");
                releaser.ManageLifetime(filter);

                for (int i = 0; i < portTableNames.Length; i++)
                {
                    string portTableName = portTableNames[i];
                    ESRI.ArcGIS.Geodatabase.ITable portTable = _wkspHelper.FindTable(portTableName);
                    ESRI.ArcGIS.Geodatabase.ICursor cursor = portTable.Search(filter, false);
                    releaser.ManageLifetime(cursor);

                    portRow = cursor.NextRow();
                    if (null != portRow)
                    {
                        ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetDeviceRelationship(portTable);
                        if (null == deviceHasPorts)
                        {
                            throw new Exception("Device to port relationship is missing or cannot be opened.");
                        }

                        ESRI.ArcGIS.Geodatabase.IFeatureClass deviceClass = deviceHasPorts.OriginClass as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                        if (null == deviceClass)
                        {
                            throw new Exception("Device feature class is missing or cannot be opened.");
                        }

                        filter.WhereClause = string.Format("{0}='{1}'",
                            deviceHasPorts.OriginPrimaryKey,
                            portRow.get_Value(portTable.FindField(deviceHasPorts.OriginForeignKey)));
                        ESRI.ArcGIS.Geodatabase.IFeatureCursor deviceCursor = deviceClass.Search(filter, false);
                        deviceFt = deviceCursor.NextFeature();

                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the fiber record for a given fiber number on a given cable ft
        /// </summary>
        /// <param name="cableFeature"></param>
        /// <param name="fiberNumber"></param>
        /// <returns>IRow</returns>
        private ESRI.ArcGIS.Geodatabase.IRow GetFiberRecord(ESRI.ArcGIS.Geodatabase.IFeature cableFeature, int fiberNumber)
        {
            ESRI.ArcGIS.Geodatabase.IRow result = null;

            ESRI.ArcGIS.Geodatabase.IRelationshipClass fiberRelationship = GdbUtils.GetRelationshipClass(cableFeature.Class, ConfigUtil.FiberCableToFiberRelClassName);
            if (null != fiberRelationship && null != fiberRelationship.DestinationClass)
            {
                ESRI.ArcGIS.Geodatabase.ITable fiberTable = fiberRelationship.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                if (null != fiberTable)
                {
                    ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                    filter.WhereClause = string.Format("{0}='{1}' AND {2}={3}",
                        fiberRelationship.OriginForeignKey,
                        cableFeature.get_Value(cableFeature.Fields.FindField(fiberRelationship.OriginPrimaryKey)),
                        ConfigUtil.Fiber_NumberFieldName,
                        fiberNumber);

                    ESRI.ArcGIS.Geodatabase.ICursor cursor = null;
                    try
                    {
                        cursor = fiberTable.Search(filter, false);
                        result = cursor.NextRow();
                    }
                    finally
                    {
                        if (null != cursor)
                        {
                            ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(cursor);
                        }
                    }
                }
            }

            return result;
        }

        public void FlashFeature(IFeature feature)
        {
            if (_hookHelper != null)
            {
                _hookHelper.FlashFeature(feature);
            }
        }

        void _hookHelper_ActiveViewSelectionChanged()
        {
            if(this.SelectionChanged != null)
                SelectionChanged(this, null);
        }

        public List<FiberCableWrapper> SelectedCables
        {
            get
            {
                List<FiberCableWrapper> result = new List<FiberCableWrapper>();

                // Get the layer
                IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName);

                // Get the selected features
                List<ESRI.ArcGIS.Geodatabase.IFeature> selectedFts = _hookHelper.GetSelectedFeatures(ftLayer);

                // Get the display field index
                int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

                // Populate result with cables...
                for (int i = 0; i < selectedFts.Count; i++)
                {
                    result.Add(new FiberCableWrapper(selectedFts[i], displayIdx));
                }

                return result;
            }
        }

        public List<DeviceWrapper> SelectedDevices
        {
            get
            {
                List<DeviceWrapper> result = new List<DeviceWrapper>();

                string[] deviceClasses = ConfigUtil.DeviceFeatureClassNames;
                for (int i = 0; i < deviceClasses.Length; i++)
                {
                    string deviceClassName = deviceClasses[i];

                    // Get the layer
                    IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(deviceClassName);
                    if (ftLayer != null)  // What if layer has been removed from map!?
                    {
                        // Get the selected devices of this type
                        List<ESRI.ArcGIS.Geodatabase.IFeature> selectedFts = _hookHelper.GetSelectedFeatures(ftLayer);

                        // Get the display field for the layer
                        int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

                        // Populate result with devices
                        for (int j = 0; j < selectedFts.Count; j++)
                        {
                            ESRI.ArcGIS.Geodatabase.IFeature deviceFt = selectedFts[j];
                            result.Add(new DeviceWrapper(deviceFt, displayIdx));
                        }
                    }
                    else
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", deviceClassName + " not found.", "Layer removed from TOC?");
                    }
                } 
                
                return result;
            }
        }


    }
}
