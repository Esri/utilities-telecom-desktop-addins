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
    /// Event args with a feature wrapper
    /// </summary>
    public class FeatureWrapperEventArgs : EventArgs
    {
        private FeatureWrapper _ftWrapper = null;

        /// <summary>
        /// Constructs a new FeatureWrapperEventArgs
        /// </summary>
        /// <param name="ftWrapper">FeatureWrapper</param>
        public FeatureWrapperEventArgs(FeatureWrapper ftWrapper)
        {
            if (null == ftWrapper)
            {
                throw new ArgumentNullException("ftWrapper");
            }

            _ftWrapper = ftWrapper;
        }

        /// <summary>
        /// Gets the FeatureWrapper associated to the event args
        /// </summary>
        public FeatureWrapper FeatureWrapper
        {
            get
            {
                return _ftWrapper;
            }
        }
    }
}
