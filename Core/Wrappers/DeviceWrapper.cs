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
using Esri_Telecom_Tools.Core.Utils;

namespace Esri_Telecom_Tools.Core.Wrappers
{
    /// <summary>
    /// Wraps a device feature
    /// </summary>
    public class DeviceWrapper : FeatureWrapper
    {
        private int _ipidIdx = -1;
        private int _inputPortsIdx = -1;
        private int _outputPortsIdx = -1;

        /// <summary>
        /// Constructs a new DeviceWrapper
        /// </summary>
        /// <param name="deviceFeature">IFeature to wrap</param>
        public DeviceWrapper(ESRI.ArcGIS.Geodatabase.IFeature deviceFeature) 
            : base(deviceFeature)
        {
            CacheFields();
        }

        /// <summary>
        /// Constructs a new DeviceWrapper where the display index is already known
        /// </summary>
        /// <param name="deviceFeature">IFeature to wrap</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public DeviceWrapper(ESRI.ArcGIS.Geodatabase.IFeature deviceFeature, int displayFieldIndex)
            : base(deviceFeature, displayFieldIndex)
        {
            CacheFields();
        }

        /// <summary>
        /// Returns the IPID value, or string.Empty if null
        /// </summary>
        public string IPID
        {
            get
            {
                if (DBNull.Value != _feature.get_Value(_ipidIdx))
                {
                    return _feature.get_Value(_ipidIdx).ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// The # of input ports in the device, or -1 if null
        /// </summary>
        public int inputPorts
        {
            get
            {
                if (DBNull.Value != _feature.get_Value(_inputPortsIdx))
                {
                    return (int)_feature.get_Value(_inputPortsIdx);
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The # of output ports on the device, or -1 if null
        /// </summary>
        public int outputPorts
        {
            get
            {
                if (DBNull.Value != _feature.get_Value(_outputPortsIdx))
                {
                    return (int)_feature.get_Value(_outputPortsIdx);
                }
                else
                {
                    return -1;
                }
            }
        }


        /// <summary>
        /// Caches the fields used for wrapped properties
        /// </summary>
        private void CacheFields()
        {
            ESRI.ArcGIS.Geodatabase.IObjectClass ftClass = _feature.Class;
            _ipidIdx = ftClass.Fields.FindField(ConfigUtil.IpidFieldName);
            _inputPortsIdx = ftClass.Fields.FindField(ConfigUtil.InputPortsFieldName);
            _outputPortsIdx = ftClass.Fields.FindField(ConfigUtil.OutputPortsFieldName);
        }
    }
}
