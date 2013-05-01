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
//    public class FiberDeviceConnectionHelper : HookHelperExt
    public class FiberDeviceConnectionHelper 
    {
        private static FiberDeviceConnectionHelper _instance = null;
        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private HookHelperExt _hookHelper = null;
        private IEditor3 _editor = null;

        /// <summary>
        /// Constructs a new ConnectionEditorHelper
        /// </summary>
        /// <param name="hook">Hook</param>
        /// <param name="editor">Editor</param>
        public FiberDeviceConnectionHelper(HookHelperExt hookHelper, IEditor3 editor)
//            : base(hook)
//            : base(hook, editor)
        {
            _hookHelper = hookHelper;
            _editor = editor;
        }

        //public static FiberDeviceConnectionHelper Instance(HookHelperExt hookHelper, ESRI.ArcGIS.Editor.IEditor editor)
        //{
        //    if(_instance != null) 
        //    {
        //        return _instance;
        //    }
        //    else
        //    {
        //        _instance = new FiberDeviceConnectionHelper(hookHelper, editor);
        //        return _instance;
        //    }
        //}


        /// <summary>
        /// Returns cables which have an endpoint coincident with the device point
        /// </summary>
        /// <param name="deviceWrapper">Device to check</param>
        /// <returns>List of ConnectableCableWrapper</returns>
        public List<ConnectableCableWrapper> GetCoincidentCables(DeviceWrapper deviceWrapper)
        {
            List<ConnectableCableWrapper> result = new List<ConnectableCableWrapper>();

            if (null == deviceWrapper)
            {
                throw new ArgumentNullException("deviceWrapper");
            }

            ESRI.ArcGIS.Geometry.IPoint devicePoint = deviceWrapper.Feature.Shape as ESRI.ArcGIS.Geometry.IPoint;

            ESRI.ArcGIS.Carto.IFeatureLayer cableLayer = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName);
            ESRI.ArcGIS.Geodatabase.IFeatureClass cableFtClass = cableLayer.FeatureClass;
            int displayIdx = cableFtClass.FindField(cableLayer.DisplayField);

            double buffer = _hookHelper.ConvertPixelsToMapUnits(1);
            List<ESRI.ArcGIS.Geodatabase.IFeature> coincidentCables = GdbUtils.GetLinearsWithCoincidentEndpoints(devicePoint, cableFtClass, buffer);

            for (int i = 0; i < coincidentCables.Count; i++)
            {
                ESRI.ArcGIS.Geodatabase.IFeature ft = coincidentCables[i];
                ESRI.ArcGIS.Geometry.IPolyline line = ft.Shape as ESRI.ArcGIS.Geometry.IPolyline;
                ESRI.ArcGIS.Geometry.IRelationalOperator lineToPoint = line.ToPoint as ESRI.ArcGIS.Geometry.IRelationalOperator;

                bool isFromEnd = true;
                if (lineToPoint.Equals(devicePoint))
                {
                    isFromEnd = false;
                }

                result.Add(new ConnectableCableWrapper(ft, isFromEnd, displayIdx));
            }

            return result;
        }

        /// <summary>
        /// Returns devices at the endpoints of the given cable
        /// </summary>
        /// <param name="deviceWrapper">Cable to check</param>
        /// <returns>List of ConnectableDeviceWrapper</returns>
        public List<ConnectableDeviceWrapper> GetCoincidentDevices(FiberCableWrapper fiberCableWrapper)
        {
            List<ConnectableDeviceWrapper> result = new List<ConnectableDeviceWrapper>();

            if (null == fiberCableWrapper)
            {
                throw new ArgumentNullException("fiberCableWrapper");
            }

            ESRI.ArcGIS.Geometry.IPolyline cableGeometry = (ESRI.ArcGIS.Geometry.IPolyline)fiberCableWrapper.Feature.Shape;
            string[] deviceFtClassNames = ConfigUtil.DeviceFeatureClassNames;

            double buffer = _hookHelper.ConvertPixelsToMapUnits(1);

            for (int i = 0; i < deviceFtClassNames.Length; i++)
            {
                string ftClassName = deviceFtClassNames[i];

                ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ftClassName);
                if (ftLayer == null)
                {
                    throw new Exception("Feature class not found: " + ftClassName);
                }
                ESRI.ArcGIS.Geodatabase.IFeatureClass ftClass = ftLayer.FeatureClass;
                int displayIdx = ftClass.FindField(ftLayer.DisplayField);

                List<ESRI.ArcGIS.Geodatabase.IFeature> fromFts = GdbUtils.GetFeaturesWithCoincidentVertices(cableGeometry.FromPoint, ftClass, false, buffer);
                for (int fromIdx = 0; fromIdx < fromFts.Count; fromIdx++)
                {
                    result.Add(new ConnectableDeviceWrapper(fromFts[fromIdx], true, displayIdx));
                }

                List<ESRI.ArcGIS.Geodatabase.IFeature> toFts = GdbUtils.GetFeaturesWithCoincidentVertices(cableGeometry.ToPoint, ftClass, false, buffer);
                for (int toIdx = 0; toIdx < toFts.Count; toIdx++)
                {
                    result.Add(new ConnectableDeviceWrapper(toFts[toIdx], false, displayIdx));
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a list of connections between the cable and the device, at the cable's given end, to the device's given port type
        /// </summary>
        /// <param name="cable">Cable to check</param>
        /// <param name="device">Device to check</param>
        /// <param name="isFromEnd">Digitized end of cable</param>
        /// <param name="portType">Input or output</param>
        /// <returns>List of Connection</returns>
        public List<Connection> GetConnections(FiberCableWrapper cable, DeviceWrapper device, bool isFromEnd, PortType portType)
        {
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            List<Connection> result = new List<Connection>();
            List<int> ports = new List<int>();
            List<int> strands = new List<int>();
            
            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = device.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
                if (null == deviceHasPorts)
                {
                    throw new Exception("Unable to find port relationship class.");
                }

                ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                if (null == portTable)
                {
                    throw new Exception("Invalid destination on port relationship class.");
                }

                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                releaser.ManageLifetime(filter);
                filter.WhereClause = string.Format("{0}='{1}' AND {2}='{3}' AND {4}='{5}' AND {6}='{7}' AND {8} IS NOT NULL AND {9} IS NOT NULL", 
                    deviceHasPorts.OriginForeignKey,
                    device.Feature.get_Value(deviceFtClass.FindField(deviceHasPorts.OriginPrimaryKey)),
                    ConfigUtil.ConnectedCableFieldName,
                    cable.IPID, 
                    ConfigUtil.PortTypeFieldName,
                    (PortType.Input == portType ? "1" : "2"), 
                    ConfigUtil.ConnectedEndFieldName,
                    (isFromEnd ? "T" : "F"),
                    ConfigUtil.ConnectedFiberFieldName,
                    ConfigUtil.PortIdFieldName);


                // ORDER BY does not work outside of SDE. 
                // Removing for now, should not be important.
                string orderFormat = "ORDER BY {0}";
                if (PortType.Input == portType)
                {
//                    ((ESRI.ArcGIS.Geodatabase.IQueryFilterDefinition2)filter).PostfixClause = string.Format(orderFormat, ConfigUtil.ConnectedFiberFieldName);
                }
                else
                {
//                    ((ESRI.ArcGIS.Geodatabase.IQueryFilterDefinition2)filter).PostfixClause = string.Format(orderFormat, ConfigUtil.PortIdFieldName);
                }

                ESRI.ArcGIS.Geodatabase.ICursor portCursor = portTable.Search(filter, true);
                ESRI.ArcGIS.Geodatabase.IRow portRow = portCursor.NextRow();

                int portIdIdx = portTable.FindField(ConfigUtil.PortIdFieldName);
                int fiberNumberIdx = portTable.FindField(ConfigUtil.ConnectedFiberFieldName);

                while (null != portRow)
                {
                    ports.Add((int)portRow.get_Value(portIdIdx));
                    strands.Add((int)portRow.get_Value(fiberNumberIdx));

                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portRow);
                    portRow = portCursor.NextRow();
                }

                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portCursor);
            }


            List<Range> portRanges = SpliceAndConnectionUtils.MergeRanges(ports);
            List<Range> strandRanges = SpliceAndConnectionUtils.MergeRanges(strands);

            if (PortType.Input == portType)
            {
                result = SpliceAndConnectionUtils.MatchUp(strandRanges, portRanges);
            }
            else
            {
                result = SpliceAndConnectionUtils.MatchUp(portRanges, strandRanges);
            }

            return result;
        }

        /// <summary>
        /// Creates given connections between cable and device
        /// </summary>
        /// <param name="cable">Cable</param>
        /// <param name="device">Device</param>
        /// <param name="units">Units to connect</param>
        /// <param name="isFromEnd">Is it the cable's from end?</param>
        /// <param name="portType">Input or Output?</param>
        /// <param name="isExistingOperation">Flag to control whether we need to wrap this in a new edit operation</param>
        /// <returns>Success</returns>
        public bool MakeConnections(FiberCableWrapper cable, DeviceWrapper device, Dictionary<int, int> units, bool isFromEnd, PortType portType, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            if (null == units)
            {
                throw new ArgumentNullException("units");
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
                ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = device.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
                if (null == deviceHasPorts)
                {
                    throw new Exception("Unable to get port relationship class.");
                }

                ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                if (null == portTable)
                {
                    throw new Exception("Invalid destination on port relationship class.");
                }

                int portIdIdx = portTable.FindField(ConfigUtil.PortIdFieldName);
                int fiberNumberIdx = portTable.FindField(ConfigUtil.ConnectedFiberFieldName);
                int cableIdIdx = portTable.FindField(ConfigUtil.ConnectedCableFieldName);
                int isFromEndIdx = portTable.FindField(ConfigUtil.ConnectedEndFieldName);
                string isFromEndValue = isFromEnd ? "T" : "F";

                Dictionary<int, int> portsAsKeys = units;
                if (PortType.Input == portType)
                {
                    portsAsKeys = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, int> pair in units)
                    {
                        portsAsKeys[pair.Value] = pair.Key;
                    }
                }

                using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                {
                    ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                    releaser.ManageLifetime(filter);

                    string format = "{0}='{1}' AND {2}='{3}'";
                    filter.WhereClause = string.Format(format, 
                        deviceHasPorts.OriginForeignKey,
                        device.Feature.get_Value(deviceFtClass.FindField(deviceHasPorts.OriginPrimaryKey)), 
                        ConfigUtil.PortTypeFieldName,
                        (PortType.Input == portType ? "1" : "2"));

                    // Non recylcing cursor since we are doing updates.
                    ESRI.ArcGIS.Geodatabase.ICursor portCursor = portTable.Update(filter, false);
                    releaser.ManageLifetime(portCursor);

                    ESRI.ArcGIS.Geodatabase.IRow portRow = portCursor.NextRow();
                    while (null != portRow)
                    {
                        object portIdObj = portRow.get_Value(portIdIdx);
                        if (DBNull.Value != portIdObj)
                        {
                            int portId = System.Convert.ToInt32(portIdObj);
                            if (portsAsKeys.ContainsKey(portId))
                            {
                                portRow.set_Value(cableIdIdx, cable.IPID);
                                portRow.set_Value(isFromEndIdx, isFromEndValue);
                                portRow.set_Value(fiberNumberIdx, portsAsKeys[portId]);
                                portRow.Store();
                            }
                        }

                        ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portRow);
                        portRow = portCursor.NextRow();
                    }

                    if (isOperationOpen)
                    {
                        _editor.StopOperation("Create Connections");
                        isOperationOpen = false;
                    }
                    
                    success = true;
                }
            }
            catch(Exception ex)
            {
                if (isOperationOpen)
                {
                    _editor.AbortOperation();
                }

                success = false;

                throw new Exception("Save operation failed.");
            }

            return success;
        }

        /// <summary>
        /// Deletes given connections between cable to device
        /// </summary>
        /// <param name="cable">Cable</param>
        /// <param name="device">Device</param>
        /// <param name="units">Units to connect</param>
        /// <param name="portType">Input or Output?</param>
        /// <param name="isExistingOperation">Flag to control whether we need to wrap this in an edit operation</param>
        /// <returns>Success</returns>
        public bool BreakConnections(FiberCableWrapper cable, DeviceWrapper device, Dictionary<int, int> units, PortType portType, bool isExistingOperation)
        {
            bool success = false;
            bool isOperationOpen = false;

            #region Validation
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            if (null == units)
            {
                throw new ArgumentNullException("units");
            }
            
            if (ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing == _editor.EditState)
            {
                throw new InvalidOperationException("You must be editing to perform this operation");
            }
            #endregion

            if (0 < units.Count)
            {
                if (!isExistingOperation)
                {
                    _editor.StartOperation();
                    isOperationOpen = true;
                }

                try
                {
                    ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = device.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                    ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
                    if (null == deviceFtClass)
                    {
                        throw new Exception("Unable to find port relationship class.");
                    }

                    ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                    if (null == portTable)
                    {
                        throw new Exception("Invalid destination on port relationship class.");
                    }

                    using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                    {
                        ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                        releaser.ManageLifetime(filter);

                        StringBuilder inList = new StringBuilder(1024);
                        foreach (KeyValuePair<int, int> pair in units)
                        {
                            string appendFormat = "{0},";
                            if (PortType.Input == portType)
                            {
                                inList.AppendFormat(appendFormat, pair.Key);
                            }
                            else
                            {
                                inList.AppendFormat(appendFormat, pair.Value);
                            }
                        }
                        inList.Remove(inList.Length - 1, 1);

                        string format = "{0}='{1}' AND {2}='{3}' AND {4}='{5}' AND {6} IN ({7})";
                        filter.WhereClause = string.Format(format, 
                            deviceHasPorts.OriginForeignKey,
                            device.Feature.get_Value(deviceFtClass.FindField(deviceHasPorts.OriginPrimaryKey)),
                            ConfigUtil.ConnectedCableFieldName,
                            cable.IPID, 
                            ConfigUtil.PortTypeFieldName,
                            (PortType.Input == portType ? "1" : "2"), 
                            ConfigUtil.ConnectedFiberFieldName,
                            inList.ToString());

                        filter.SubFields = string.Format("{0},{1},{2}", ConfigUtil.ConnectedEndFieldName, ConfigUtil.ConnectedFiberFieldName, ConfigUtil.ConnectedCableFieldName);

                        ESRI.ArcGIS.Geodatabase.IRowBuffer buffer = portTable.CreateRowBuffer();
                        releaser.ManageLifetime(buffer);
                        // We want to set them to null, so we can just send the empty buffer
                        portTable.UpdateSearchedRows(filter, buffer);

                        if (isOperationOpen)
                        {
                            _editor.StopOperation("Break Connections");
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
            }

            return success;
        }
        


        /// <summary>
        /// Break all connections for a given cable
        /// </summary>
        /// <param name="cable">Cable to break connections</param>
        /// <param name="isExistingOperation">Should we start/stop the edit operation, or are we already in one?</param>
        /// <returns>Success</returns>
        public bool BreakAllConnections(FiberCableWrapper cable, bool isExistingOperation)
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
                using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                {
                    ESRI.ArcGIS.Geodatabase.IFeatureClass cableFtClass = cable.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                    ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                    releaser.ManageLifetime(filter);

                    filter.WhereClause = string.Format("{0}='{1}'", ConfigUtil.ConnectedCableFieldName, cable.IPID);
                    filter.SubFields = string.Format("{0},{1},{2}", ConfigUtil.ConnectedCableFieldName, ConfigUtil.ConnectedFiberFieldName, ConfigUtil.ConnectedEndFieldName);

                    string[] deviceClassNames = ConfigUtil.DeviceFeatureClassNames;
                    for (int i = 0; i < deviceClassNames.Length; i++)
                    {
                        string deviceClassName = deviceClassNames[i];


                        ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = _wkspHelper.FindFeatureClass(deviceClassName);
//                        ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = GdbUtils.GetFeatureClass(cableFtClass, deviceClassName);
                        ESRI.ArcGIS.Geodatabase.ITable portTable = ConfigUtil.GetPortTable(deviceFtClass);
                        ESRI.ArcGIS.Geodatabase.IRowBuffer buffer = portTable.CreateRowBuffer();
                        // We want to set them to null, so we can just send the empty buffer
                        portTable.UpdateSearchedRows(filter, buffer);

                        ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(buffer);
                    }

                    if (isOperationOpen)
                    {
                        _editor.StopOperation("Break Connections");
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

    }
}
