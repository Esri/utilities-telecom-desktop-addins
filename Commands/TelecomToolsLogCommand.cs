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
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using Esri_Telecom_Tools.Windows;
using ESRI.ArcGIS.Desktop.AddIns;
using Esri_Telecom_Tools.Helpers;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Commands
{
    [ComVisible(false)]
    public class TelecomToolsLogCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private LogHelper _logHelper = LogHelper.Instance();

        public TelecomToolsLogCommand()
        {
            try
            {
                // -----------------------------------
                // Always hide log window on 
                // any initialization
                // -----------------------------------
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_TelecomToolsLogWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(false);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "TelecomToolsLogCommand()", ex.Message);
            }
        }

        protected override void OnClick()
        {
            try
            {
                // -------------------------------------
                // Initialize the window with the helper
                // -------------------------------------
                TelecomToolsLogWindow.AddinImpl winImpl =
                    AddIn.FromID<TelecomToolsLogWindow.AddinImpl>(
                    ThisAddIn.IDs.Esri_Telecom_Tools_Windows_TelecomToolsLogWindow);
                TelecomToolsLogWindow logWindow = winImpl.UI;
                logWindow.InitLog(LogHelper.Instance());

                //Get dockable window.
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_TelecomToolsLogWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(true);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "TelecomToolsLogCommand::OnClick()", ex.Message);
            }
        }

        protected override void OnUpdate()
        {
        }
    }
}
