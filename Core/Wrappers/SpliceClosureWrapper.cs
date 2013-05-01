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
    /// Wraps a Splice Closure feature
    /// </summary>
    public class SpliceClosureWrapper : FeatureWrapper
    {
        private int _ipidIdx = -1;

        /// <summary>
        /// Constructs a new SpliceClosureWrapper
        /// </summary>
        /// <param name="spClosureFeature">IFeature to wrap</param>
        public SpliceClosureWrapper(ESRI.ArcGIS.Geodatabase.IFeature spClosureFeature) : base (spClosureFeature)
        {
            CacheFields();
        }

        /// <summary>
        /// Constructs a new SpliceClosureWrapper where the display index is already known
        /// </summary>
        /// <param name="spClosureFeature">IFeature to wrap</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public SpliceClosureWrapper(ESRI.ArcGIS.Geodatabase.IFeature spClosureFeature, int displayFieldIndex)
            : base(spClosureFeature, displayFieldIndex)
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
        /// Caches the fields used for wrapped properties
        /// </summary>
        private void CacheFields()
        {
            ESRI.ArcGIS.Geodatabase.IObjectClass ftClass = _feature.Class;
            _ipidIdx = ftClass.Fields.FindField(ConfigUtil.IpidFieldName);
        }
    }
}
