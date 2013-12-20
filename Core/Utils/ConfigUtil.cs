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
using System.IO;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Text;
using Esri_Telecom_Tools.Helpers;

namespace Esri_Telecom_Tools.Core.Utils
{
    /// <summary>
    /// Provides access to configurable elements of the Telecom Template, like feature class names, field names, enabled flags, etc.
    /// </summary>
    public static class ConfigUtil
    {
        // Generic app defaults - FC and field names etc
        private static Dictionary<string, string> _settingsCache = new Dictionary<string, string>();

        // Standard report fields for each type (ClassName,[list of fields])
        private static Dictionary<string, List<string>> _reportFieldsCache = new Dictionary<string, List<string>>();      

        private static List<string> DEVICE_CLASSNAMES = new List<string>(new String[] {});
        private static List<string> PORT_TABLENAMES = new List<string>(new String[] {});

        #region General
        public static string IpidFieldName
        {
            get
            {
                return GetConfigValue("IPID", "IPID");
            }
        }
        #endregion

        #region Device / Port Helpers

        /// <summary>
        /// A list of all the feature class names for device features.
        /// </summary>
        public static string[] DeviceFeatureClassNames
        {
            get
            {
                if (DEVICE_CLASSNAMES.Count == 0)
                {
                    DEVICE_CLASSNAMES.AddRange(GetConfigValue("DEVICE_CLASSNAMES", "").Split(','));
                    return DEVICE_CLASSNAMES.ToArray();
                }
                else
                    return DEVICE_CLASSNAMES.ToArray();
            }
        }

        /// <summary>
        /// A list of all the port table names.
        /// </summary>
        public static string[] PortTableNames
        {
            get
            {
                if (PORT_TABLENAMES.Count == 0)
                {
                    PORT_TABLENAMES.AddRange(GetConfigValue("PORT_TABLENAMES", "").Split(','));
                    return PORT_TABLENAMES.ToArray();
                }
                else
                    return PORT_TABLENAMES.ToArray();
            }
        }

        /// <summary>
        /// Determines if the class name is in the list of device classes
        /// </summary>
        /// <param name="className">Name to check</param>
        /// <returns>bool</returns>
        public static bool IsDeviceClassName(string className)
        {
            if (DEVICE_CLASSNAMES.Count == 0)
            {
                DEVICE_CLASSNAMES.AddRange(GetConfigValue("DEVICE_CLASSNAMES", "").Split(','));
                return DEVICE_CLASSNAMES.Contains(className.ToUpper());
            }
            else
                return DEVICE_CLASSNAMES.Contains(className.ToUpper());
        }

        /// <summary>
        /// Determines if the class name is in the list of port classes
        /// </summary>
        /// <param name="className">Name to check</param>
        /// <returns>bool</returns>
        public static bool IsPortClassName(string className)
        {
            if (PORT_TABLENAMES.Count == 0)
            {
                PORT_TABLENAMES.AddRange(GetConfigValue("PORT_TABLENAMES", "").Split(','));
                return PORT_TABLENAMES.Contains(className.ToUpper());
            }
            else
                return PORT_TABLENAMES.Contains(className.ToUpper());
        }

        /// <summary>
        /// Returns the port table related to a given device feature class
        /// </summary>
        /// <param name="deviceClassName">Device class</param>
        /// <returns>ITable</returns>
        public static ESRI.ArcGIS.Geodatabase.ITable GetPortTable(ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass)
        {
            if (null == deviceFtClass)
            {
                throw new ArgumentNullException("deviceFtClass");
            }

            ESRI.ArcGIS.Geodatabase.IRelationshipClass relationshipClass = GetPortRelationship(deviceFtClass);
            if (null == relationshipClass)
            {
                throw new Exception("Could not find port relationship class.");
            }

            ESRI.ArcGIS.Geodatabase.ITable result = relationshipClass.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
            if (null == result)
            {
                throw new Exception("DestinationClass from port relationship null or does not implement ITable.");
            }

            return result;
        }

        /// <summary>
        /// Returns the device class related to a given port table
        /// </summary>
        /// <param name="deviceClassName">Port Table</param>
        /// <returns>IFeatureClass</returns>
        public static ESRI.ArcGIS.Geodatabase.IFeatureClass GetDeviceClass(ESRI.ArcGIS.Geodatabase.ITable portTable)
        {
            if (null == portTable)
            {
                throw new ArgumentNullException("portTable");
            }

            ESRI.ArcGIS.Geodatabase.IRelationshipClass relationshipClass = GetDeviceRelationship(portTable);
            if (null == relationshipClass)
            {
                throw new Exception("Could not find port relationship class.");
            }

            ESRI.ArcGIS.Geodatabase.IFeatureClass result = relationshipClass.OriginClass as ESRI.ArcGIS.Geodatabase.IFeatureClass;
            if (null == result)
            {
                throw new Exception("OriginClass from port relationship null or does not implement ITable.");
            }

            return result;
        }

        /// <summary>
        /// Finds the Device to Port relationship class for a given device feature class
        /// </summary>
        /// <param name="deviceClass">device feature class</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.IRelationshipClass</returns>
        public static ESRI.ArcGIS.Geodatabase.IRelationshipClass GetPortRelationship(ESRI.ArcGIS.Geodatabase.IFeatureClass deviceClass)
        {
            // TODO: Currently returns the first relationship class found whose name ends with "Ports". Should probably make the relationship classes part of the configuration

            ESRI.ArcGIS.Geodatabase.IRelationshipClass result = null;

            if (null == deviceClass)
            {
                throw new ArgumentNullException("deviceClass");
            }

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IEnumRelationshipClass relClasses = deviceClass.get_RelationshipClasses(ESRI.ArcGIS.Geodatabase.esriRelRole.esriRelRoleOrigin);
                releaser.ManageLifetime(relClasses);

                relClasses.Reset();
                result = relClasses.Next();

                while (null != result)
                {
                    ESRI.ArcGIS.Geodatabase.IDataset relationshipDataset = result as ESRI.ArcGIS.Geodatabase.IDataset;

                    if (relationshipDataset.Name.EndsWith("Ports", StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    result = relClasses.Next();
                }
            }

            if (null == result)
            {
                throw new Exception("Relationship class from device to port was not found.");
            }

            return result;
        }
        
        /// <summary>
        /// Finds the Device to Port relationship class for a given port table
        /// </summary>
        /// <param name="deviceClass">port table</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.IRelationshipClass</returns>
        public static ESRI.ArcGIS.Geodatabase.IRelationshipClass GetDeviceRelationship(ESRI.ArcGIS.Geodatabase.ITable portTable)
        {
            // TODO: Currently returns the first relationship class found whose name ends with "Ports". Should probably make the relationship classes part of the configuration

            ESRI.ArcGIS.Geodatabase.IRelationshipClass result = null;

            if (null == portTable)
            {
                throw new ArgumentNullException("portTable");
            }

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IEnumRelationshipClass relClasses = ((ESRI.ArcGIS.Geodatabase.IObjectClass)portTable).get_RelationshipClasses(ESRI.ArcGIS.Geodatabase.esriRelRole.esriRelRoleDestination);
                releaser.ManageLifetime(relClasses);

                relClasses.Reset();
                result = relClasses.Next();

                while (null != result)
                {
                    ESRI.ArcGIS.Geodatabase.IDataset relationshipDataset = result as ESRI.ArcGIS.Geodatabase.IDataset;

                    if (relationshipDataset.Name.EndsWith("Ports", StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }

                    result = relClasses.Next();
                }
            }

            if (null == result)
            {
                throw new Exception("Relationship class from device to port was not found.");
            }

            return result;
        }

        #endregion

        #region Device Fields

        public static string InputPortsFieldName
        {
            get
            {
                return GetConfigValue("Devices_InputPorts", "INPUTPORTS");
            }
        }

        public static string OutputPortsFieldName
        {
            get
            {
                return GetConfigValue("Devices_OutputPorts", "OUTPUTPORTS");
            }
        }

        #endregion

        #region Port Fields

        public static string PortIdFieldName
        {
            get
            {
                return GetConfigValue("Ports_PortID", "PORTID");
            }
        }

        public static string PortTypeFieldName
        {
            get
            {
                return GetConfigValue("Ports_PortType", "PORTTYPE");
            }
        }

        public static string ConnectedCableFieldName
        {
            get
            {
                return GetConfigValue("Ports_ConnectedCableID", "CABLEIDFKEY");
            }
        }

        public static string ConnectedEndFieldName
        {
            get
            {
                return GetConfigValue("Ports_ConnectedFromEnd", "ISFROMEND");
            }
        }

        public static string ConnectedFiberFieldName
        {
            get
            {
                return GetConfigValue("Ports_ConnectedFiberNumber", "FIBERNUMBER");
            }
        }

        #endregion

        #region Fiber

        public static string FiberTableName
        {
            get
            {
                return GetConfigValue("Fiber_TableName", "Fiber");
            }
        }

        #endregion
        
        #region FiberCable

        public static string FiberCableFtClassName
        {
            get
            {
                return GetConfigValue("FiberCable_FeatureClassName", "FiberCable");
            }
        }

        public static string NumberOfBuffersFieldName
        {
            get
            {
                return GetConfigValue("FiberCable_NumberOfBuffers", "NumberOfBuffers");
            }
        }

        public static string NumberOfFibersFieldName
        {
            get
            {
                return GetConfigValue("FiberCable_NumberOfFibers", "NumberOfFibers");
            }
        }

        #endregion

        #region CopperCable

        public static string CopperCableFtClassName
        {
            get
            {
                return GetConfigValue("CopperCable_FeatureClassName", "CopperCable");
            }
        }

        #endregion

        #region SpliceClosure

        public static string SpliceClosureFtClassName
        {
            get
            {
                return GetConfigValue("SpliceClosure_FeatureClassName", "SpliceClosure");
            }
        }

        #endregion

        #region Fiber

        public static string Fiber_NumberFieldName
        {
            get
            {
                return GetConfigValue("Fiber_FiberNumber", "FiberNumber");
            }
        }

        public static string Fiber_ColorFieldName
        {
            get
            {
                return GetConfigValue("Fiber_FiberColor", "FiberColor");
            }
        }

        public static string Fiber_ContainingCableFieldName
        {
            get
            {
                return GetConfigValue("Fiber_ContainingCable", "CABLEIDFKEY");
            }
        }

        #endregion

        #region BufferTube

        public static string BufferTubeClassName
        {
            get
            {
                return GetConfigValue("BufferTube_TableName", "BufferTube");
            }
        }

        #endregion

        #region Conduit

        public static string ConduitFtClassName
        {
            get
            {
                return GetConfigValue("Conduit_FeatureClassName", "Conduit");
            }
        }

        #endregion

        #region Pole

        public static string PoleFtClassName
        {
            get
            {
                return GetConfigValue("Pole_FeatureClassName", "Pole");
            }
        }

        #endregion

        #region Terminal

        public static string TerminalFtClassName
        {
            get
            {
                return GetConfigValue("Terminal_FeatureClassName", "Terminal");
            }
        }

        #endregion

        #region Relationship Class names

        public static string SpliceClosureToSpliceRelClassName
        {
            get
            {
                return GetConfigValue("SpliceClosureToSpliceRelationshipClass", "SpliceClosureHasSplices");
            }
        }

        public static string FiberCableToMaintLoopClassName
        {
            get
            {
                return GetConfigValue("CableToMaintLoopRelationshipClass", "FiberCableHasMaintLoop");
            }
        }

        public static string FiberCableToBufferRelClassName
        {
            get
            {
                return GetConfigValue("CableToBufferRelationshipClass", "FiberCableHasBufferTube");
            }
        }

        public static string FiberCableToFiberRelClassName
        {
            get
            {
                return GetConfigValue("CableToFiberRelationshipClass", "FiberCableHasFiber");
            }
        }

        public static string BufferToFiberRelClassName
        {
            get
            {
                return GetConfigValue("BufferToFiberRelationshipClass", "BufferTubeHasFiber");
            }
        }

        public static string ConduitToDuctRelClassName
        {
            get
            {
                return GetConfigValue("ConduitToDuctRelationshipClass", "ConduitHasDuct");
            }
        }

        public static string DuctToInnerductRelClassName
        {
            get
            {
                return GetConfigValue("DuctToInnerductRelationshipClass", "DuctHasInnerduct");
            }
        }

        public static string PoleToAttachmentRelClassName
        {
            get
            {
                return GetConfigValue("PoleToAttachmentRelationshipClass", "PoleHasAttachment");
            }
        }

        public static string TerminalToLocationRelClassName
        {
            get
            {
                return GetConfigValue("TerminalHasLocationRelationshipClass", "TerminalHasLocation");
            }
        }

        public static string BufferTubeToFiberRelClassName
        {
            get
            {
                return GetConfigValue("BufferTubeToFiberRelClassName", "BufferTubeHasFiber");
            }
        }



        #endregion

        #region FiberSplice

        public static string FiberSpliceTableName
        {
            get
            {
                return GetConfigValue("FiberSplice_TableName", "FiberSplice");
            }
        }

        public static string ACableIdFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_ACableID", "ASEGMENTIDFKEY");
            }
        }

        public static string BCableIdFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_BCableID", "BSEGMENTIDFKEY");
            }
        }

        public static string IsAFromEndFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_AFromEnd", "ISAFROMEND");
            }
        }

        public static string IsBFromEndFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_BFromEnd", "ISBFROMEND");
            }
        }

        public static string AFiberNumberFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_AFiberNumber", "AFIBERNUMBER");
            }
        }

        public static string BFiberNumberFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_BFiberNumber", "BFIBERNUMBER");
            }
        }

        public static string SpliceClosureIpidFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_SpliceClosureIPID", "SPLICECLOSUREIPID");
            }
        }

        public static string LossFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_Loss", "LOSS");
            }
        }

        public static string TypeFieldName
        {
            get
            {
                return GetConfigValue("FiberSplice_Type", "TYPE");
            }
        }

        #endregion

        #region Configuration Access

        /// <summary>
        /// Gets a config value as int
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>int</returns>
        public static int GetConfigValue(string keyName, int defaultValue) 
        {
            int result = -1;

            string value = GetKeyValue(keyName, defaultValue);
            if (!int.TryParse(value, out result))
            {
                result = defaultValue;
            }    

            return result;
        }

        /// <summary>
        /// Gets a config value as double
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>double</returns>
        public static double GetConfigValue(string keyName, double defaultValue)
        {
            double result = defaultValue;

            string value = GetKeyValue(keyName, defaultValue);
            if (!double.TryParse(value, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Gets a config value as bool
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>bool</returns>
        public static bool GetConfigValue(string keyName, bool defaultValue)
        {
            bool result = defaultValue;

            string value = GetKeyValue(keyName, defaultValue);
            if (!bool.TryParse(value, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Gets a config value as string
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>string</returns>
        public static string GetConfigValue(string keyName, string defaultValue)
        {
            return GetKeyValue(keyName, defaultValue);
        }

        /// <summary>
        /// Checks each config file for the given key. Returns the value, or string.Empty if not found
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <returns>string</returns>
        private static string GetKeyValue(string keyName)
        {
            return GetKeyValue(keyName, null);
        }

        /// <summary>
        /// Checks the settings cache, or each config file, for the given key. Returns the value, or a default if not found
        /// </summary>
        /// <param name="keyName">Key to check for</param>
        /// <param name="defaultValue">Value to return if the key is not found</param>
        /// <returns>string</returns>
        private static string GetKeyValue(string keyName, object defaultValue)
        {
            string result = string.Empty; 

            if (_settingsCache.ContainsKey(keyName))
            {
                result = _settingsCache[keyName];
            }
            else
            {
                result = GetKeyValueFromFiles(keyName, defaultValue);
                _settingsCache[keyName] = result;
            }

            return result;
        }

        /// <summary>
        /// Reads the report fields for each type. 
        /// </summary>
        /// <returns>Dictionary</returns>
        public static Dictionary<string,List<string>> FiberTraceReportFields()
        {
            // If nothing in fiber config try reading config
            if (_reportFieldsCache.Count == 0)
            {
                System.Reflection.AssemblyName executingAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                string AppPath = Path.GetDirectoryName(executingAssemblyName.CodeBase);
                if (AppPath.IndexOf("file:\\") >= 0)
                {
                    AppPath = AppPath.Replace("file:\\", "");
                }

                string fiberConfigPath = Path.Combine(AppPath, "FiberTraceReportFields.xml");
                XmlDocument doc = new XmlDocument();
                doc.Load(fiberConfigPath);
                XmlNodeList fiberConfigs = doc.SelectNodes("//ReportItem");
                foreach (XmlNode config in fiberConfigs)
                {
                    string name = config.Attributes["Name"].Value;
                    string fields = config.Attributes["Fields"].Value;
                    if(0 == fields.CompareTo("*"))
                    {
                        _reportFieldsCache.Add(name,new List<string>(new String[] {"*"}));
                    }
                    else
                    {
                        _reportFieldsCache.Add(name,new List<string>(fields.Split(',')));
                    }                    
                }
            }
            return _reportFieldsCache;
        }

        private static string GetKeyValueFromFiles(string keyName, object defaultValue)
        {
            string result = (null == defaultValue) ? string.Empty : defaultValue.ToString();

            string[] pConfigFiles = GetConfigFiles();
            foreach (string configFile in pConfigFiles)
            {
                if (File.Exists(configFile))
                {
                    XmlDocument configDocument = new XmlDocument();
                    configDocument.Load(configFile);

                    System.Xml.XmlElement element = configDocument.SelectSingleNode(string.Format("//appSettings/add[@key='{0}']", keyName)) as System.Xml.XmlElement;
                    if (null != element)
                    {
                        result = element.GetAttribute("value");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a string array of potential config files
        /// </summary>
        /// <returns>string[]</returns>
        private static string[] GetConfigFiles()
        {
            System.Reflection.AssemblyName executingAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            string AppPath = Path.GetDirectoryName(executingAssemblyName.CodeBase);
            if (AppPath.IndexOf("file:\\") >= 0)
            {
                AppPath = AppPath.Replace("file:\\", "");
            }

            string[] pConfigFiles = new string[2];
            pConfigFiles[0] = Path.Combine(AppPath, executingAssemblyName.Name + ".dll.config");
            pConfigFiles[1] = Path.Combine(AppPath, "App.config");

            return pConfigFiles;
        }


        #endregion

        public static string SplitCableSpliceTypeName
        {
            get
            {
                return GetConfigValue("SplitFiberCable_SpliceType", "PassThru");
            }
        }

    }
}

