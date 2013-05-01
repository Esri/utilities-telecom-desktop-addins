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
using Esri_Telecom_Tools.Events;

namespace Esri_Telecom_Tools.Helpers
{
        public class LogHelper
    {
        private static LogHelper _instance = null;

        // ------------------------------------
        // All access is through singleton
        // ------------------------------------
        private LogHelper()
        {
        }

        // initialize with filestream?

        // Signal updates
        public event EventHandler LogUpdated;

        // ------------------------------------
        // Use this static helper to get hold 
        // of a Telecom Log Helper.
        // ------------------------------------
        public static LogHelper Instance()
        {
            if(_instance == null)
            {
                _instance = new LogHelper();
            }
            return _instance;
        }

        public void addLogEntry(string timestamp, string type, string desciption,string details = "")
        {
            // Raise an update event
            if (LogUpdated != null)
            {
                LogUpdated(this, new TelecomLogEvent(timestamp, type, desciption,details));
            }
        }
    }
}
