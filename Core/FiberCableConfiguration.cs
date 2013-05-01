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

namespace Esri_Telecom_Tools.Core
{
    /// <summary>
    /// Encapsulates a combination of buffer tube and fiber count
    /// </summary>
    public class FiberCableConfiguration
    {
        private string _displayName = "";
        private string _detailDesc = "";
        private int _bufferCount = -1;
        private int _totalFiberCount = -1;
        private int _fibersPerTube = -1;

        /// <summary>
        /// Constructs a new FiberCableConfiguration
        /// </summary>
        /// <param name="bufferCount">Total number of buffer tubes</param>
        /// <param name="fiberCount">Total Number of fiber strands</param>
        public FiberCableConfiguration(int bufferCount, int totalFiberCount)
        {
            _bufferCount = bufferCount;
            _totalFiberCount = totalFiberCount;
            _fibersPerTube = totalFiberCount / bufferCount;
        }

        /// <summary>
        /// Constructs a new FiberCableConfiguration
        /// </summary>
        /// <param name="bufferCount">Total number of buffer tubes</param>
        /// <param name="strandsPerTube">Number of fiber strands per tube</param>
        /// <param name="displayName">Name that will appear in any dropdowns</param>
        /// <param name="detailDesc">Name or text that will appear in detailed descriptions</param>
        public FiberCableConfiguration(int bufferCount, int strandsPerTube, string displayName, string detailDesc)
        {
            _bufferCount = bufferCount;
            _fibersPerTube = strandsPerTube;
            _totalFiberCount = bufferCount * strandsPerTube;
            _displayName = displayName;
            _detailDesc = detailDesc;          
        }

        /// <summary>
        /// The display name
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        /// <summary>
        /// The detail name
        /// </summary>
        public string DetailDesc
        {
            get
            {
                return _detailDesc;
            }
        }

        /// <summary>
        /// The total number of buffer tubes
        /// </summary>
        public int BufferCount
        {
            get
            {
                return _bufferCount;
            }
        }

        /// <summary>
        /// The total number of fiber strands
        /// </summary>
        public int TotalFiberCount
        {
            get
            {
                return _totalFiberCount;
            }
        }

        /// <summary>
        /// The calculated (rounded if necessary) number of fiber strands
        /// </summary>
        public int FibersPerTube
        {
            get
            {
                return _fibersPerTube;
            }
        }

        /// <summary>
        /// A clear representation of the configuration.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return string.Format("{0} x {1} ({2} strands)", _bufferCount, _fibersPerTube, _totalFiberCount);
        }
    }
}
