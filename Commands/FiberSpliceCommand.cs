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
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Editor;
using Esri_Telecom_Tools.Helpers;
using Esri_Telecom_Tools.Windows;
using ESRI.ArcGIS.Geodatabase;
using System.Runtime.InteropServices;


namespace Esri_Telecom_Tools.Commands
{
    [ComVisible(false)]
    public class FiberSpliceCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private FiberSpliceHelper _spliceHelper = null;
        private HookHelperExt _hookHelper = null;

        public FiberSpliceCommand()
        {
            try
            {
                if (ArcMap.Editor == null)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Editor License not found.", "FiberSpliceConnectionCommand()");
                    return;
                }

                // -----------------------------------
                // Construct a new hook helper and a 
                // splice helper that does all the 
                // cable to cable splice work
                // -----------------------------------
                _hookHelper = HookHelperExt.Instance(this.Hook);
                _spliceHelper = new FiberSpliceHelper(_hookHelper, ArcMap.Editor as IEditor3);

                // -----------------------------------
                // Always hide splice window on 
                // any initialization
                // -----------------------------------
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_FiberSpliceWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(false);

                // -----------------------------------
                // Track the start and stop of editing
                // -----------------------------------
                Events.OnStartEditing += new IEditEvents_OnStartEditingEventHandler(Events_OnStartEditing);
                Events.OnStopEditing += new IEditEvents_OnStopEditingEventHandler(Events_OnStopEditing);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FiberSpliceCommand()", ex.Message);
            }
        }

//        protected override void Dispose(bool disposing)
//        {
//            base.Dispose(disposing);
////            Events.OnStartEditing -= new IEditEvents_OnStartEditingEventHandler(Events_OnStartEditing);
////            Events.OnStopEditing -= new IEditEvents_OnStopEditingEventHandler(Events_OnStopEditing);
//        }

        void Events_OnStopEditing(bool save)
        {
            // -----------------------------------
            // Get the splice form and set to 
            // read only mode
            // -----------------------------------
            FiberSpliceWindow.AddinImpl winImpl = 
                AddIn.FromID<FiberSpliceWindow.AddinImpl>(
                ThisAddIn.IDs.Esri_Telecom_Tools_Windows_FiberSpliceWindow);
            FiberSpliceWindow spliceWindow = winImpl.UI;
            spliceWindow.IsEditing = false;
        }

        void Events_OnStartEditing()
        {
            // -----------------------------------
            // Check to see if we are editing the 
            // telecom workspace, and that 
            // workspace is valid, if not ignore.
            // -----------------------------------
            ESRI.ArcGIS.Geodatabase.IFeatureWorkspace workspace = (ESRI.ArcGIS.Geodatabase.IFeatureWorkspace)ArcMap.Editor.EditWorkspace;
            IFeatureWorkspace fwksp = TelecomWorkspaceHelper.Instance().CurrentWorkspace;
            bool wkspIsValid = TelecomWorkspaceHelper.Instance().CurrentWorkspaceIsValid;
            if (workspace == null || !wkspIsValid || !workspace.Equals(fwksp))
            {
                return;
            }

            // -----------------------------------
            // Workspace is valid for editing.
            // Get the splice form and set to 
            // edit mode
            // -----------------------------------
            FiberSpliceWindow.AddinImpl winImpl = 
                AddIn.FromID<FiberSpliceWindow.AddinImpl>(
                ThisAddIn.IDs.Esri_Telecom_Tools_Windows_FiberSpliceWindow);
            FiberSpliceWindow spliceWindow = winImpl.UI;
            spliceWindow.IsEditing = true;
        }

        #region Shortcut properties to the various editor event interfaces
        private IEditEvents_Event Events
        {
            get { return ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditEvents_Event; }
            //get { return ArcMap.Editor as IEditEvents_Event; }
        }
        private IEditEvents2_Event Events2
        {
            get { return ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditEvents2_Event; }
            //get { return ArcMap.Editor as IEditEvents2_Event; }
        }
        private IEditEvents3_Event Events3
        {
            get { return ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditEvents3_Event; }
            //get { return ArcMap.Editor as IEditEvents3_Event; }
        }
        private IEditEvents4_Event Events4
        {
            get { return ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditEvents4_Event; }
            //get { return ArcMap.Editor as IEditEvents4_Event; }
        }
        private IEditEvents5_Event Events5
        {
            get { return ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditEvents5_Event; }
            //get { return ArcMap.Editor as IEditEvents4_Event; }
        }
        #endregion

        protected override void OnClick()
        {
            try
            {
                // ------------------------------------
                // Set the selection tool as the 
                // current tool
                // ------------------------------------
                _hookHelper.ExecuteSelectionTool();

                // -------------------------------------
                // Initialize the window with the helper
                // -------------------------------------
                FiberSpliceWindow.AddinImpl winImpl =
                    AddIn.FromID<FiberSpliceWindow.AddinImpl>(
                    ThisAddIn.IDs.Esri_Telecom_Tools_Windows_FiberSpliceWindow);
                FiberSpliceWindow spliceWindow = winImpl.UI;
                spliceWindow.DisplaySplices(_spliceHelper);

                //Get dockable window.
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_FiberSpliceWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(true);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FiberSpliceCommand::OnClick()", ex.Message);
            }
        }

        protected override void OnUpdate()
        {
            Enabled = _wkspHelper.CurrentWorkspaceIsValid && (ArcMap.Editor != null); 
        }
    }
}
