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
    /// Wraps a device feature relative to which end of a cable would be connected to it
    /// </summary>
    public class ConnectableDeviceWrapper : DeviceWrapper
    {
        protected bool _isCableFromEnd = false;

        /// <summary>
        /// Constructs a new ConnectableDeviceWrapper
        /// </summary>
        /// <param name="deviceFeature">IFeature to wrap</param>
        /// <param name="isCableFromEnd">Flag for connecting cable's end</param>
        public ConnectableDeviceWrapper(ESRI.ArcGIS.Geodatabase.IFeature deviceFeature, bool isCableFromEnd)
            : base(deviceFeature)
        {
            _isCableFromEnd = isCableFromEnd;
        }

        /// <summary>
        /// Constructs a new ConnectableDeviceWrapper where the display index is already known 
        /// </summary>
        /// <param name="deviceFeature">IFeature to wrap</param>
        /// <param name="isCableFromEnd">Flag for connecting cable's end</param>
        /// <param name="displayFieldIndex">Index of display field</param>
        public ConnectableDeviceWrapper(ESRI.ArcGIS.Geodatabase.IFeature deviceFeature, bool isCableFromEnd, int displayFieldIndex)
            : base(deviceFeature, displayFieldIndex)
        {
            _isCableFromEnd = isCableFromEnd;
        }

        /// <summary>
        /// Returns the flag of which end of the cable would be connected at this device
        /// </summary>
        public bool IsCableFromEnd
        {
            get
            {
                return _isCableFromEnd;
            }
        }
    }
}
