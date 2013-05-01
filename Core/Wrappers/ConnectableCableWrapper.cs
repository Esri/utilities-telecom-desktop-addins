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
    /// Wraps a fiber cable feature relative to which end would be connected to a device
    /// </summary>
    public class ConnectableCableWrapper: FiberCableWrapper
    {
        protected bool _isThisFromEnd = false;

        /// <summary>
        /// Constructs a new ConnectableCableWrapper
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        /// <param name="isThisFromEnd">Flag of which end of the cable would be connected</param>
        public ConnectableCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature, bool isThisFromEnd)
            : base(fiberCableFeature)
        {
            _isThisFromEnd = isThisFromEnd;
        }

        /// <summary>
        /// Constructs a new ConnectableCableWrapper where the display index is already known
        /// </summary>
        /// <param name="fiberCableFeature">IFeature to wrap</param>
        /// <param name="isThisFromEnd">Flag of which end of the cable would be connected</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public ConnectableCableWrapper(ESRI.ArcGIS.Geodatabase.IFeature fiberCableFeature, bool isThisFromEnd, int displayFieldIndex)
            : base(fiberCableFeature, displayFieldIndex)
        {
            _isThisFromEnd = isThisFromEnd;
        }

        /// <summary>
        /// Returns the flag of which end of the cable would be used for splices or connections
        /// </summary>
        public bool IsThisFromEnd
        {
            get
            {
                return _isThisFromEnd;
            }
        }
    }
}
