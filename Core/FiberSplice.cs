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
    /// A Connection between two fiber cables; the ranges are the fiber strands. Also records Loss and Type values
    /// </summary>
    /// <remarks>Loss and Type are nullable. Use "null" here, which is stored as DBNull.Value in the database</remarks>
    public class FiberSplice : Connection
    {
        private double? _loss = null;
        private object _spliceType = null;

        /// <summary>
        /// Contructs a new FiberSplice object 
        /// </summary>
        /// <param name="aRange">Cable A strand range</param>
        /// <param name="bRange">Cable B strand range</param>
        /// <param name="loss">Loss value</param>
        /// <param name="spliceType">Type value</param>
        public FiberSplice(Range aRange, Range bRange, double? loss, object spliceType)
            : base(aRange, bRange)
        {
            _loss = loss;
            _spliceType = spliceType;
        }

        /// <summary>
        /// The loss resulting from the splice
        /// </summary>
        public double? Loss
        {
            get
            {
                return _loss;
            }
        }

        /// <summary>
        /// Type of splice (Mechanical, Fusion, etc.)
        /// </summary>
        public object Type
        {
            get
            {
                return _spliceType;
            }
        }
    }
}
