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
using Esri_Telecom_Tools.Core.Wrappers;
using ESRI.ArcGIS.Geodatabase;

namespace Esri_Telecom_Tools.Core.Wrappers
{
    /// <summary>
    /// Wraps an IFeature for addition to .NET controls with ToString implementations
    /// </summary>
    public class FeatureWrapper
    {
        protected ESRI.ArcGIS.Geodatabase.IFeature _feature = null;
        private int _displayFieldIdx = -1;
        private string _toString = string.Empty;

        /// <summary>
        /// Constructs a new FeatureWrapper
        /// </summary>
        /// <param name="feature">IFeature</param>
        public FeatureWrapper(ESRI.ArcGIS.Geodatabase.IFeature feature)
        {
            if (null == feature)
            {
                throw new ArgumentNullException("feature");
            }
            else if (null == feature.Class)
            {
                throw new ArgumentException("feature.Class cannot be null");
            }

            _feature = feature;
            CacheToString();
        }

        /// <summary>
        /// Constructs a new FeatureWrapper where the display field is already known
        /// </summary>
        /// <param name="feature">IFeature to wrap</param>
        /// <param name="displayFieldIndex">Index for the display field</param>
        public FeatureWrapper(ESRI.ArcGIS.Geodatabase.IFeature feature, int displayFieldIndex)
        {
            if (null == feature)
            {
                throw new ArgumentNullException("feature");
            }
            else if (null == feature.Class)
            {
                throw new ArgumentException("feature.Class cannot be null");
            }

            _feature = feature;
            _displayFieldIdx = displayFieldIndex;
            CacheToString();
        }

        /// <summary>
        /// Deconstructs the FeatureWrapper
        /// </summary>
        ~FeatureWrapper()
        {
            _feature = null;
        }

        /// <summary>
        /// The wrapped IFeature
        /// </summary>
        public ESRI.ArcGIS.Geodatabase.IFeature Feature
        {
            get { return _feature; }
        }

        /// <summary>
        /// Gets or sets the display field by name
        /// </summary>
        public string DisplayFieldName
        {
            get
            {
                string displayFieldName = _feature.Table.OIDFieldName;
                if (-1 < _displayFieldIdx)
                {
                    displayFieldName = _feature.Fields.get_Field(_displayFieldIdx).Name;
                }
                return displayFieldName;
            }
            set
            {
                _displayFieldIdx = _feature.Fields.FindField(value);
                CacheToString();
            }
        }

        /// <summary>
        /// Gets or sets the display field by index
        /// </summary>
        public int DisplayFieldIndex
        {
            get
            {
                return _displayFieldIdx;
            }
            set
            {
                _displayFieldIdx = value;
                CacheToString();
            }
        }

        /// <summary>
        /// Provides a clear string representation of the feature based on feature class, display field, and / or OID
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return _toString;
        }

        /// <summary>
        /// Caches a string representation of the feature based on feature class, display field, and / or OID
        /// </summary>
        private void CacheToString()
        {
            string result = base.ToString();

            if (null != _feature)
            {
                string displayValue = string.Format("OID {0}", _feature.OID);
                if (-1 < _displayFieldIdx)
                {
                    object objValue = _feature.get_Value(_displayFieldIdx);
                    if (DBNull.Value != objValue)
                    {
                        displayValue = objValue.ToString();
                    }
                }

                string ftClassName = "Unknown Ft. Class";

                ESRI.ArcGIS.Geodatabase.IDataset dataset = _feature.Class as ESRI.ArcGIS.Geodatabase.IDataset;
                if (null != dataset)
                {
                    ESRI.ArcGIS.Geodatabase.ISQLSyntax syntax = dataset.Workspace as ESRI.ArcGIS.Geodatabase.ISQLSyntax;
                    if (null != syntax)
                    {
                        string dbName = string.Empty;
                        string ownerName = string.Empty;
                        string tableName = string.Empty;
                        syntax.ParseTableName(dataset.Name, out dbName, out ownerName, out tableName);
                        ftClassName = tableName;
                    }
                }

                result = string.Format("{0} ({1})", displayValue, ftClassName);
            }

            _toString = result;
        }
    }
}
