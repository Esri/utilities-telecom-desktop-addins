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
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using Esri_Telecom_Tools.Helpers;
using Esri_Telecom_Tools.Windows;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Commands
{
    [ComVisible(false)]
    public class FiberTraceCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private LogHelper _logHelper = LogHelper.Instance();
        private FiberTraceHelper _fiberTraceHelper = null;
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        public FiberTraceCommand()
        {
            try
            {
                // -----------------------------------
                // Build trace helper that does all 
                // the work
                // -----------------------------------
                _fiberTraceHelper = new FiberTraceHelper(HookHelperExt.Instance(this.Hook));

                // -----------------------------------
                // Always hide trace windows on 
                // any initialization
                // -----------------------------------
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_FiberTraceWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(false);

                dockWinID.Value = @"esriTelcoTools_FiberTraceReportWindow";
                dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(false);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FiberTraceCommand()", ex.Message);
            }
        }

        protected override void OnClick()
        {
            try
            {
                // -----------------------------------
                // Show the fiber trace dialog
                // -----------------------------------
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_FiberTraceWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(true);

                FiberTraceWindow.AddinImpl winImpl =
                    AddIn.FromID<FiberTraceWindow.AddinImpl>(
                    ThisAddIn.IDs.Esri_Telecom_Tools_Windows_FiberTraceWindow);
                FiberTraceWindow traceWindow = winImpl.UI;
                traceWindow.InitFiberTrace(_fiberTraceHelper);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FiberTraceCommand::OnClick", ex.Message);
            }
        }

        protected override void OnUpdate()
        {
            Enabled = _wkspHelper.CurrentWorkspaceIsValid && (ArcMap.Editor != null);
        }

    }
}
