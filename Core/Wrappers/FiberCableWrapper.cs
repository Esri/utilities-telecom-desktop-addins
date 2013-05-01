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
using Esri_Telecom_Tools.Core.Utils;

namespace Esri_Telecom_Tools.Core.Wrappers
{
    /// <summary>
    /// Wraps a fiber cable feature
    /// </summary>
    public class FiberCableWrapper : FeatureWrapper
    {
        private int _ipidIdx = -1;
        private int _bufferTubesIdx = -1;
        private int _fibersIdx = -1;

        /// <summary>
        /// Constructs a new FiberCableWrapper
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        public FiberCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature)
            : base(fiberCableFeature)
        {
            CacheFields();
        }

        /// <summary>
        /// Constructs a new FiberCableWrapper where the display index is already known
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public FiberCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature, int displayFieldIndex)
            : base(fiberCableFeature, displayFieldIndex)
        {
            CacheFields();
        }

        /// <summary>
        /// The IPID of the feature, or string.Empty if null
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
        /// The # of buffer tubes in the cable, or -1 if null
        /// </summary>
        public int bufferTubes
        {
            get
            {
                if (DBNull.Value != _feature.get_Value(_bufferTubesIdx))
                {
                    return (int)_feature.get_Value(_bufferTubesIdx);
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The # of fibers in the cable, or -1 if null
        /// </summary>
        public int fibers
        {
            get
            {
                if (DBNull.Value != _feature.get_Value(_fibersIdx))
                {
                    return (int)_feature.get_Value(_fibersIdx);
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
            ESRI.ArcGIS.Geodatabase.IObjectClass ftClass = base._feature.Class;
            _ipidIdx = ftClass.Fields.FindField(ConfigUtil.IpidFieldName);
            _bufferTubesIdx = ftClass.Fields.FindField(ConfigUtil.NumberOfBuffersFieldName);
            _fibersIdx = ftClass.Fields.FindField(ConfigUtil.NumberOfFibersFieldName);
        }
    }
}
