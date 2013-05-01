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

namespace Esri_Telecom_Tools.Core.Wrappers
{
    /// <summary>
    /// Wraps a cable feature in related to another cable
    /// </summary>
    public class SpliceableCableWrapper : ConnectableCableWrapper
    {
        private bool _isOtherFromEnd = false;

        /// <summary>
        /// Constructs a new SpliceableCableWrapper
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        /// <param name="isThisFromEnd">Flag for which end of this cable would be spliced</param>
        /// <param name="isOtherFromEnd">Flag for which end of the other cable would be spliced</param>
        public SpliceableCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature, bool isThisFromEnd, bool isOtherFromEnd)
            : base(fiberCableFeature, isThisFromEnd)
        {
            _isOtherFromEnd = isOtherFromEnd;
        }

        /// <summary>
        /// Constructs a new SpliceableCableWrapper where the display index is already known
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        /// <param name="isThisFromEnd">Flag for which end of this cable would be spliced</param>
        /// <param name="isOtherFromEnd">Flag for which end of the other cable would be spliced</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public SpliceableCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature, bool isThisFromEnd, bool isOtherFromEnd, int displayFieldIndex)
            : base(fiberCableFeature, isThisFromEnd, displayFieldIndex)
        {
            _isOtherFromEnd = isOtherFromEnd;
        }

        /// <summary>
        /// Constructs a new SpliceableCableWrapper, determining which ends to use by comparing to the other cable
        /// </summary>
        /// <param name="thisFeature">IFeature to wrap</param>
        /// <param name="otherFeature">IFeature with which to compare endpoints</param>
        public SpliceableCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature thisFeature, ESRI.ArcGIS.Geodatabase.IFeature otherFeature)
            : base(thisFeature, true)
        {

            #region Validation
            // We already know thisFeature is not null from the base validation
            if (null == otherFeature)
            {
                throw new ArgumentNullException("otherFeature");
            }
            else if (null == otherFeature.Class)
            {
                throw new ArgumentException("otherFeature.Class cannot be null.");
            }

            ESRI.ArcGIS.Geometry.IPolyline thisPolyline = thisFeature.Shape as ESRI.ArcGIS.Geometry.IPolyline;
            if (null == thisPolyline)
            {
                throw new ArgumentException("thisFeature.Shape must be IPolyline.");
            }

            ESRI.ArcGIS.Geometry.IPolyline otherPolyline = otherFeature.Shape as ESRI.ArcGIS.Geometry.IPolyline;
            if (null == otherPolyline)
            {
                throw new ArgumentException("otherPolyline.Shape must be IPolyline.");
            }
            #endregion

            ESRI.ArcGIS.Geometry.IRelationalOperator thisFrom = thisPolyline.FromPoint as ESRI.ArcGIS.Geometry.IRelationalOperator;
            ESRI.ArcGIS.Geometry.IRelationalOperator thisTo = thisPolyline.ToPoint as ESRI.ArcGIS.Geometry.IRelationalOperator;
            ESRI.ArcGIS.Geometry.IPoint otherFrom = otherPolyline.FromPoint;
            ESRI.ArcGIS.Geometry.IPoint otherTo = otherPolyline.ToPoint;

            // Assume it will be the from end of thisFeature and the to end of otherFeature
            base._isThisFromEnd = true;
            _isOtherFromEnd = false;

            if (thisTo.Equals(otherFrom))
            {
                base._isThisFromEnd = false;
                _isOtherFromEnd = true;
            }
            else if (thisFrom.Equals(otherFrom))
            {
                _isOtherFromEnd = true;
            }
            else if (thisTo.Equals(otherTo))
            {
                base._isThisFromEnd = false;
            }
        }

        /// <summary>
        /// Returns the flag of which end of the other cable would be spliced
        /// </summary>
        public bool IsOtherFromEnd
        {
            get
            {
                return _isOtherFromEnd;
            }
        }
    }
}