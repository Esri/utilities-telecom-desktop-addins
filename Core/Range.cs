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
    /// A continuous range of units.
    /// </summary>
    public class Range
    {
        private int _low = -1;
        private int _high = -1;
        private int _originalLow = -1;
        private int _originalHigh = -1;

        /// <summary>
        /// Contructs a new Range and tracks these as the Original values. Also swaps high/low if low is greater than high.
        /// </summary>
        /// <param name="low">Low value</param>
        /// <param name="high">High value</param>
        public Range(int low, int high)
        {
            if (low > high)
            {
                // Someone, somewhere, someday, is going to do this just to see what happens. Hopefully they will be pleased...
                int swap = low;
                low = high;
                high = swap;
            }

            _low = low;
            _high = high;

            _originalLow = low;
            _originalHigh = high;
        }

        /// <summary>
        /// The Low value for the range
        /// </summary>
        public int Low
        {
            get
            {
                return _low;
            }
            set
            {
                _low = value;
            }
        }

        /// <summary>
        /// The High value for the range
        /// </summary>
        public int High
        {
            get
            {
                return _high;
            }
            set
            {
                _high = value;
            }
        }

        /// <summary>
        /// The original Low value when the Range object was constructed
        /// </summary>
        public int OriginalLow
        {
            get
            {
                return _originalLow;
            }
        }

        /// <summary>
        /// The original High value when the Range object was constructed
        /// </summary>
        public int OriginalHigh
        {
            get
            {
                return _originalHigh;
            }
        }

        /// <summary>
        /// Returns "n" or "n - m"
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            if (_low == _high)
            {
                return _low.ToString();
            }
            else
            {
                return string.Format("{0} - {1}", _low, _high);
            }
        }
    }
}
