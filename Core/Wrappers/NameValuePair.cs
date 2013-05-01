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
using System.Linq;
using System.Text;

namespace Esri_Telecom_Tools.Core.Wrappers
{
    public class NameValuePair
    {
        private string _alias =  string.Empty;
        private string _name = string.Empty;
        private string _value = string.Empty;

        public NameValuePair(string alias, string name, string value)
        {
            _alias = alias;
            _name = name;
            _value = value;
        }

        public string Alias
        {
            get { return _alias; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
        }
    }
}
