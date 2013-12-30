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
using System.Windows.Forms;

using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.Framework;
using Esri_Telecom_Tools.Helpers;
using Esri_Telecom_Tools.Core.Utils;
using Esri_Telecom_Tools.Windows;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Commands;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.CatalogUI;
using System.Runtime.InteropServices;
using Esri_Telecom_Tools.Core;



namespace Esri_Telecom_Tools.Extension
{
    /// <summary>
    /// FiberEditorExtension class implementing custom ESRI Editor Extension functionalities.
    /// </summary>
    /// 
    [ComVisible(false)]
    public class FiberEditorExtension : ESRI.ArcGIS.Desktop.AddIns.Extension
    {
        private HookHelperExt _hookHelper;

        // ------------------------
        // Creation helper objects
        //-------------------------
        private FiberCableConfigHelper _fiberCableHelper = null;
        private FiberDeviceConfigHelper _fiberDeviceHelper = null;

        // ------------------------
        // Splice helper objects
        //-------------------------
        private FiberSpliceHelper _spliceHelper = null;
        private FiberDeviceConnectionHelper _connectionHelper = null;

        // ------------------------
        // General helper objects
        //-------------------------
        private LogHelper _logHelper = null;
        private TelecomWorkspaceHelper _wkspHelper = null;

        private string[] m_nonFeatureTables = new string[] { };

        // Monitored non-feature tables
        private System.Collections.Generic.List<IObjectClassEvents_Event> _nonFeatureObjClassEvents = null;

        private bool _isShuttingDown = false;

        #region Dynamic values variables

        private ITable m_dynDefaults = null;

        // Declare configuration variables 
        private ESRI.ArcGIS.esriSystem.IPropertySet2 lastValueProperties;

        //The schema of the dynamic values table is fixed.  Please copy table provided.  
        //If it is modiifed these field positions may be incorrect.
        private int dynTargetField = 2;
        private int dynMethodField = 3;
        private int dynDataField = 4;
        private int dynCreateField = 5;
        private int dynChangeField = 6;

        private IFeatureWorkspace _currentWorkspace = null;

        private string lastEditorName;

        #endregion


        public FiberEditorExtension()
        {
            try
            {
                // --------------------------------------
                // Initialize log window with log helper
                // --------------------------------------
                _logHelper = LogHelper.Instance();
                TelecomToolsLogWindow.AddinImpl winImpl =
                    AddIn.FromID<TelecomToolsLogWindow.AddinImpl>(
                    ThisAddIn.IDs.Esri_Telecom_Tools_Windows_TelecomToolsLogWindow);
                TelecomToolsLogWindow logWindow = winImpl.UI;
                logWindow.InitLog(_logHelper);

                // --------------------
                // Build a hook helper
                // --------------------
                _hookHelper = HookHelperExt.Instance(this.Hook);

                // -------------------------------------------
                // Initialize telecom workspace helper.
                //
                // Listen to ActiveViewChanged event.
                // 
                // If this happens the focus map more than 
                // likely changed. Since the tools go after 
                // layers in the TOC we probably need to close 
                // the current telecom workspace since 
                // editing etc could not longer be done. 
                // Should add code to ask for saving changes.
                // -------------------------------------------
                _wkspHelper = TelecomWorkspaceHelper.Instance();
                _wkspHelper.ActiveViewChanged += new EventHandler(_wkspHelper_ActiveViewChanged);

                // -------------------------------------------
                // Build helpers that actually do all object
                // creation work for special feature types
                // -------------------------------------------
                _fiberCableHelper = new FiberCableConfigHelper(_hookHelper, ArcMap.Editor as IEditor3);
                _fiberDeviceHelper = new FiberDeviceConfigHelper(_hookHelper, ArcMap.Editor as IEditor3);

                // --------------------------------------------
                // Splice and Connection helpers
                // --------------------------------------------
                _spliceHelper = new FiberSpliceHelper(_hookHelper, ArcMap.Editor as IEditor3);
                _connectionHelper = new FiberDeviceConnectionHelper(_hookHelper, ArcMap.Editor as IEditor3);

                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Telecom Extension Constructed.");
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "General error.", ex.ToString());
            }
        }

        /// <summary>
        /// The active view changed probably through a switch to a different focus map.
        /// Since Telecom tools use layers in the map in some cases we need to close 
        /// the current telecom workspace and force user to revalidate current workspace.
        /// </summary>        
        void _wkspHelper_ActiveViewChanged(object sender, EventArgs e)
        {
            if (_wkspHelper.CurrentWorkspaceIsValid)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Active view changed","Closing current telecom workspace");

                // ------------------------------------
                // Close workspace event handler will 
                // deal with any active edit sessions.
                // ------------------------------------
                _wkspHelper.CloseCurrentWorkspace();
            }
        }

        #region Helpers to the various editor event interfaces

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

        #region OnStartup/OnShutdown events

        /// <summary>
        /// The extension is starting up
        /// </summary>        
        protected override void OnStartup()
        {
            try
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Extension Startup...");

                AddWorkspaceEvents();
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "General error.", ex.ToString());
            }
        }

        /// <summary>
        /// The extension is shutting down
        /// </summary>        
        protected override void OnShutdown()
        {
            _isShuttingDown = true;

            try
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Extension Shutdown...");

                // Close the current workspace
                _wkspHelper.CloseCurrentWorkspace();

                RemoveWorkspaceEvents();
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "General error.", ex.ToString());
            }
        }

        /// <summary>
        /// Adds events to track changes to the current telecom workspace
        /// </summary>        
        private void AddWorkspaceEvents()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Adding workspace event handlers");

            // Add event handlers
            _wkspHelper.ValidWorkspaceSelected += new EventHandler(_wkspHelper_WorkspaceSelected);
            _wkspHelper.WorkspaceClosed += new EventHandler(_wkspHelper_WorkspaceClosed);
        }

        /// <summary>
        /// Removes events to track changes to the current telecom workspace
        /// </summary>        
        private void RemoveWorkspaceEvents()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Removing workspace event handlers");

            // Remove event handlers
            _wkspHelper.ValidWorkspaceSelected -= new EventHandler(_wkspHelper_WorkspaceSelected);
            _wkspHelper.WorkspaceClosed -= new EventHandler(_wkspHelper_WorkspaceClosed);
        }

        /// <summary>
        /// Called when a valid telecom workspace is selected
        /// </summary>        
        private void _wkspHelper_WorkspaceSelected(object sender, EventArgs e)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Telecom workspace selected");

            // So we dont have telecom editing stuff firing during general 
            // non-telecom editing sessions we use the workspace open/close 
            // events to enable and disbale the editing event tracking. If 
            // user is already in an edit session then opens a telecom 
            // workspace events will not be rigged up in the correct order. 
            //
            // Workpace validity needs to be checked in the StartEditing 
            // event but this will have already fired. To get around this we 
            // must close any current editing sessions and force the user 
            // to start editing again.
            // 
            // Edits have been made
            if (ArcMap.Editor != null &&
                ArcMap.Editor.EditState == esriEditState.esriStateEditing &&
                ArcMap.Editor.HasEdits())
            {
                // Assume they do not want to save and do want to continue
                DialogResult dialogResult = DialogResult.No;

                dialogResult = MessageBox.Show("Current edit session must be closed first. You have unsaved edits. Would you like to save them before closing?", "Workspace Closed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (DialogResult.No == dialogResult)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "No edits saved");
                    ArcMap.Editor.StopEditing(false);
                }
                else if (DialogResult.Yes == dialogResult)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "Edits saved");
                    ArcMap.Editor.StopEditing(true);
                }
            }
            else
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "No edits have been made.");
                ArcMap.Editor.StopEditing(false);
            }

            AddEditorEvents();  // Start/Stop Editing
        }

        /// <summary>
        /// Called when a valid telecom workspace is closed
        /// </summary>        
        private void _wkspHelper_WorkspaceClosed(object sender, EventArgs e)
        {
            try
            {

                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Telecom workspace closed");

                // Edits have been made
                if (ArcMap.Editor != null &&
                    ArcMap.Editor.EditState == esriEditState.esriStateEditing &&
                    ArcMap.Editor.HasEdits())
                {
                    // Assume they do not want to save and do want to continue
                    DialogResult dialogResult = DialogResult.No;

                    dialogResult = MessageBox.Show("You have unsaved edits. Would you like to save them before closing?", "Workspace Closed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (DialogResult.No == dialogResult)
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "No edits saved");
                        ArcMap.Editor.StopEditing(false);
                    }
                    else if (DialogResult.Yes == dialogResult)
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "Edits saved");
                        ArcMap.Editor.StopEditing(true);
                    }
                }
                else
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping current editing session.", "No edits have been made.");
                    ArcMap.Editor.StopEditing(false);
                }

                // Remove any editor events
                // Dont attempt to remove events if shutting down as interface already gone!
                if(!_isShuttingDown)
                    RemoveEditorEvents();
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "General error.", ex.ToString());
            }
        }

        /// <summary>
        /// Adds events for starting and stopping of editing sessions
        /// </summary>        
        private void AddEditorEvents()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Adding editor event handlers");

            // --------------------------------
            // Listen to Editor events
            // --------------------------------
            try
            {
                Events.OnStartEditing += new IEditEvents_OnStartEditingEventHandler(
                    m_editEvents_OnStartEditing);
                Events.OnStopEditing += new IEditEvents_OnStopEditingEventHandler(
                    m_editEvents_OnStopEditing);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "Error", "Unable to get Edit Events Interface", ex.ToString());
            }
        }

        /// <summary>
        /// Removes events for starting and stopping of editing sessions
        /// </summary>        
        private void RemoveEditorEvents()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Removing editor event handlers");

            // --------------------------------
            // Listen to Editor events
            // --------------------------------
            try
            {
                Events.OnStartEditing -= new IEditEvents_OnStartEditingEventHandler(
                    m_editEvents_OnStartEditing);
                Events.OnStopEditing -= new IEditEvents_OnStopEditingEventHandler(
                    m_editEvents_OnStopEditing);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "Info", "Unable to get Edit Events Interface (Shutting down??)", ex.ToString());
            }
        }

        #endregion

        #region Start/Stop Editing

        /// <summary>
        /// On starting an edit session the user may have the 
        /// choice of selecting one or more workspaces in the 
        /// editor selection dialog.
        /// 
        /// This handler will validate that the user selected 
        /// the telecom workspace for editing, if not we do 
        /// nothing we dont care about those edits. If they did 
        /// choose the telecom workspace then we signal the 
        /// helpers that they need to start work.
        /// 
        /// We also need to deal with dynamic values population.
        /// </summary>        
        private void m_editEvents_OnStartEditing()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "On start editing");

            try
            {
                // -----------------------------------
                // Check to see if we are editing the 
                // telecom workspace, and that 
                // workspace is valid, if not ignore.
                // -----------------------------------
                ESRI.ArcGIS.Geodatabase.IFeatureWorkspace workspace = (ESRI.ArcGIS.Geodatabase.IFeatureWorkspace)ArcMap.Editor.EditWorkspace;
                IFeatureWorkspace fwksp = TelecomWorkspaceHelper.Instance().CurrentWorkspace;
                bool wkspIsValid = TelecomWorkspaceHelper.Instance().CurrentWorkspaceIsValid;
                if (workspace == null || !wkspIsValid)
                {
                    return;
                }

                // Valid telecom workspace being edited...
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Started editing telecom workspace...");

                // ------------------------------------
                // Start the helpers to deal with 
                // telecom workspace editing.
                // ------------------------------------
                _fiberCableHelper.onStartEditing();
                _fiberDeviceHelper.onStartEditing();
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Initialized telecom helpers.");

                //---------------------------------------
                // Open dynamic defaults table
                // Doing this now for performance reasons
                // Feature creation and population will 
                // work much faster. Even better if we 
                // brought contents into memory.
                //---------------------------------------
                if (workspace == null) return;
                ITable tab = workspace.OpenTable(TelecomWorkspaceHelper.Instance().dynamicValuesTableName());
                if (tab == null)
                {
                    ArcMap.Application.StatusBar.set_Message(0, "No Dynamic Defaults");
                    return;
                }
                else
                {
                    m_dynDefaults = tab;
                }

                //---------------------------------------
                // Listen for feature template changes
                // so we can show appopriate dialogs 
                // when needed.
                //---------------------------------------
                Events5.OnCurrentTemplateChanged += new IEditEvents5_OnCurrentTemplateChangedEventHandler(Events5_OnCurrentTemplateChanged);




                // Everything below hear needs to move to helpers and dynamic values helper


                // ------------------------------------------------
                // We need to deal with deletions differently since 
                // there may be connectivity invovled. 
                //
                // All creations are managed by helpers (embedded 
                // in dialogs).
                //
                // All deletions are manager extension Editor.
                // -------------------------------------------------
                Events.OnDeleteFeature += new IEditEvents_OnDeleteFeatureEventHandler(Events_OnDeleteFeature);

                // -----------------------------------
                //  Now for Dynamic Values changes
                // -----------------------------------
                Events.OnChangeFeature += new IEditEvents_OnChangeFeatureEventHandler(m_editEvents_OnChangeFeature);
                Events.OnCreateFeature += new IEditEvents_OnCreateFeatureEventHandler(m_editEvents_OnCreateFeature);

                if (lastValueProperties == null)
                {
                    constructFieldArray();
                }

                // ----------------------------------------------------------------------
                // Assuming that none of the non feature class tables will be in the 
                // document by default, add in object class level events for adds and 
                // deletes. NOTE: these events will only fire if cursor type adds and 
                // updates are NOT used. These cursors are designed to do fast updates 
                // by eliminating event firing.
                // ----------------------------------------------------------------------
                //HookHelperExt helper = new HookHelperExt(m_editor.Parent, m_editor);
                _nonFeatureObjClassEvents = new System.Collections.Generic.List<IObjectClassEvents_Event>();
                for (int i = 0; i < m_nonFeatureTables.Length; i++)
                {
                    if (workspace != null)
                    {
                        // ESRI.ArcGIS.Geodatabase.IObjectClassEvents_Event nonFeatureOCE = getTable(m_nonFeatureTables[i]) as ESRI.ArcGIS.Geodatabase.IObjectClassEvents_Event;
                        ESRI.ArcGIS.Geodatabase.IObjectClassEvents_Event nonFeatureOCE = workspace.OpenTable(m_nonFeatureTables[i]) as ESRI.ArcGIS.Geodatabase.IObjectClassEvents_Event;
                        if (null != nonFeatureOCE)
                        {
                            _nonFeatureObjClassEvents.Add(nonFeatureOCE);
                            nonFeatureOCE.OnChange += new IObjectClassEvents_OnChangeEventHandler(
                                m_editEvents_OnChangeFeature);
                            nonFeatureOCE.OnCreate += new IObjectClassEvents_OnCreateEventHandler(
                                m_editEvents_OnCreateFeature);
                        }
                    }
                }
           
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Editor error occurred.", ex.ToString());

                MessageBox.Show("Error: \n" + ex.ToString());
            }
        }

        /// <summary>
        /// Signals the telecom workspace helpers objects to stop listening for edits etc.
        /// </summary>
        /// <param name="save">Should any edits be saved</param>
        private void m_editEvents_OnStopEditing(Boolean save)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "On stop editing");

            try
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopped editing telecom workspace.");

                // -------------------------------------
                // Hide the cable configuration window
                // -------------------------------------
                //UID dockWinID = new UIDClass();
                //dockWinID.Value = @"esriTelcoTools_FiberCableConfigWindow";
                //IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                //dockWindow.Show(false);

                // ------------------------------------
                // Stop the helpers to deal with 
                // telecom workspace editing.
                // ------------------------------------
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Stopping telecom helpers.");
                _fiberCableHelper.onStopEditing();
                _fiberDeviceHelper.onStopEditing();

                // If we are dealing with non-shapefile, non-coverage data no need to remove 
                // event handlers since no GDB relationships etc can exist. 
                if (ArcMap.Editor.EditWorkspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace ||
                    ArcMap.Editor.EditWorkspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace)
                {
                    // --------------------------------------------
                    // Stop listening for feature template changes
                    // --------------------------------------------
                    Events5.OnCurrentTemplateChanged -= new IEditEvents5_OnCurrentTemplateChangedEventHandler(Events5_OnCurrentTemplateChanged);

                    // -----------------------------
                    // Stop listening for deletions
                    // -----------------------------
                    Events.OnDeleteFeature -= new IEditEvents_OnDeleteFeatureEventHandler(Events_OnDeleteFeature);

                    // -----------------------------------
                    //  Stop listening for Dynamic Values changes
                    // -----------------------------------
                    Events.OnChangeFeature += new IEditEvents_OnChangeFeatureEventHandler(m_editEvents_OnChangeFeature);
                    Events.OnCreateFeature += new IEditEvents_OnCreateFeatureEventHandler(m_editEvents_OnCreateFeature);
                }

                if (!save)
                {
                    lastValueProperties = null;
                }

                // ---------------------------------------
                // Remove non feature class event tracking
                // ---------------------------------------
                if (null != _nonFeatureObjClassEvents)
                {
                    for (int i = _nonFeatureObjClassEvents.Count - 1; i > -1; i--)
                    {
                        ESRI.ArcGIS.Geodatabase.IObjectClassEvents_Event nonFtClassOCE = _nonFeatureObjClassEvents[i];
                        nonFtClassOCE.OnCreate -= new IObjectClassEvents_OnCreateEventHandler(m_editEvents_OnCreateFeature);
                        nonFtClassOCE.OnChange -= new IObjectClassEvents_OnChangeEventHandler(m_editEvents_OnChangeFeature);
                        _nonFeatureObjClassEvents.Remove(nonFtClassOCE);
                    }
                    _nonFeatureObjClassEvents = null;
                }
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Error on stop editing.", ex.ToString());

                MessageBox.Show("Error: \n" + ex.ToString());
            }
        }

        #endregion

        #region Deletion

        /// <summary>
        /// We need to deal with trickle down deletions 
        /// of related records for some objects
        /// </summary>
        /// <param name="obj">The feature being deleted</param>
        void Events_OnDeleteFeature(IObject obj)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Deleting feature.");

            // ----------------------------------------------
            // Events will only happen for the those feature 
            // classes or tables already in the document when 
            // the edit session was started. 
            // ----------------------------------------------

            // quick sanity checks
            if (obj == null || !(obj is IFeature)) return;
            IFeature ft = obj as IFeature;

            // Check for specific telecom object types
            string className = GdbUtils.ParseTableName(obj.Class as ESRI.ArcGIS.Geodatabase.IDataset);
            if (0 == string.Compare(className, ConfigUtil.FiberCableFtClassName, true) && obj is IFeature)
            {
                CascadeFiberCableDelete(ft);
            }
            else if (0 == string.Compare(className, ConfigUtil.SpliceClosureFtClassName, true))
            {
                CascadeSpliceClosureDelete(ft);
            }
        }

        /// <summary>
        /// Cable features can have related: 
        ///         splices, 
        ///         connections, 
        ///         strands
        ///         buffer tubes, 
        ///         maintenance loop
        /// </summary>
        /// <param name="cableFeature">Deleted IFeature</param>
        private void CascadeFiberCableDelete(ESRI.ArcGIS.Geodatabase.IFeature cableFeature)
        {
            FiberCableWrapper cable = new FiberCableWrapper(cableFeature);

            // Delete splices (to other cables)
            _spliceHelper.BreakAllSplices(cable, true);

            // Delete connections (to equipment)
            _connectionHelper.BreakAllConnections(cable, true);
        }

        /// <summary>
        /// Splice closure features can have related:
        ///         splice records
        /// </summary>
        /// <param name="spliceFt">Deleted IFeature</param>
        private void CascadeSpliceClosureDelete(ESRI.ArcGIS.Geodatabase.IFeature spliceFt)
        {
            SpliceClosureWrapper splice = new SpliceClosureWrapper(spliceFt);

            // Delete fiber splices 
            _spliceHelper.BreakAllSplices(splice, true);
        }

        #endregion

        #region Template Changed


        void Events5_OnCurrentTemplateChanged(IEditTemplate editTemplate)
        {
            try
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Edit template changed.");

                if (editTemplate == null || editTemplate.Layer == null) return;
                if (editTemplate.Layer as IFeatureLayer == null) return;

                IFeatureLayer fLayer = editTemplate.Layer as IFeatureLayer;
                IFeatureClass fc;
                if ((fc = fLayer.FeatureClass) != null)
                {
                    ESRI.ArcGIS.Geodatabase.IDataset dataset = (ESRI.ArcGIS.Geodatabase.IDataset)fc;
                    string tableName = GdbUtils.ParseTableName(dataset);

                    // -------------------------------------------------
                    // Checks for different feature types of interest.
                    // Show the appropriate dialogs for any that need it
                    // -------------------------------------------------

                    // -----------------------------
                    // Fiber
                    // -----------------------------
                    if (0 == string.Compare(ConfigUtil.FiberCableFtClassName, tableName, true))
                    {
                        // Is the current template chosen properly configured with default values...
                        // Needs number of buffer tubes and no of strands of fiber
                        if (editTemplate.get_DefaultValue(ConfigUtil.NumberOfBuffersFieldName) == null ||
                            editTemplate.get_DefaultValue(ConfigUtil.NumberOfFibersFieldName) == null)
                        {
                            IEditor3 editor = ArcMap.Editor as IEditor3;
                            editor.CurrentTemplate = null;
                            MessageBox.Show("Template item has not been configured with # Buffer Tubes or # Strands per Tube. \n\nRight click this item and change these values in the properties.\n\nCreate new template items in the 'Organize Templates' window at the top of this area.");
                        }
                        else
                        {
                            int buffers = (int)editTemplate.get_DefaultValue(ConfigUtil.NumberOfBuffersFieldName);
                            int fibers = (int)editTemplate.get_DefaultValue(ConfigUtil.NumberOfFibersFieldName);

                            FiberCableConfiguration cf = new FiberCableConfiguration(
                                                            buffers,
                                                            fibers,
                                                            "",
                                                            "");

                            _fiberCableHelper.FiberCableConfig = cf;
                        }

                        // Enable the configuration helper
                        //_fiberCableHelper.onStartEditing();
                    }
                    else
                    {
                        //_fiberCableHelper.onStopEditing();
                    }

                    // -----------------------------
                    // Fiber Devices
                    // -----------------------------
                    if (ConfigUtil.IsDeviceClassName(tableName))
                    {
                        // Is the current template chosen properly configured with default values...
                        // Needs number of buffer tubes and no of strands of fiber
                        if (editTemplate.get_DefaultValue(ConfigUtil.InputPortsFieldName) == null ||
                            editTemplate.get_DefaultValue(ConfigUtil.OutputPortsFieldName) == null)
                        {
                            IEditor3 editor = ArcMap.Editor as IEditor3;
                            editor.CurrentTemplate = null;
                            MessageBox.Show("Template item has not been configured with # Input Ports or # Output Ports. \n\nRight click this item and change these values in the properties.\n\nCreate new template items in the 'Organize Templates' window at the top of this area.");
                        }
                        else
                        {
                            int inputPorts = (int)editTemplate.get_DefaultValue(ConfigUtil.InputPortsFieldName);
                            int outputPorts = (int)editTemplate.get_DefaultValue(ConfigUtil.OutputPortsFieldName);

                            _fiberDeviceHelper.InputPorts = inputPorts;
                            _fiberDeviceHelper.OutputPorts = outputPorts;
                        }

                        //_fiberDeviceHelper.onStartEditing();
                    }
                    else
                    {
                        //_fiberDeviceHelper.onStopEditing();
                    }
                }
            }
            catch(Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Error changing edit template.", ex.ToString());
            }
        }

        #endregion

        #region Dynamic Values

        private void m_editEvents_OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Changing feature.");

            sendEvent(obj as IObject, "OnChange");
        }

        private void m_editEvents_OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
//            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Creating feature.");

            sendEvent(obj as IObject, "OnCreate");
        }

        private void sendEvent(IObject inObject, string mode)
        {
            //IFeature inFeature = inObject as IFeature;
            //IInvalidArea invalid = new InvalidAreaClass();

            //if (inFeature != null)
            //{
            //    //Prepare to redraw area around feature
            //    invalid.Display = editor.Display;
            //    IEnvelope ext = inFeature.Extent;
            //    ext.Expand(1.5, 1.5, true);
            //    invalid.Add(ext);
            //}


            //Set attributes based on the dynamic defaults configuration table
            setDynamicDefaults(inObject, mode);

            ////Queue redraw
            //if (inFeature != null)
            //    invalid.Invalidate((short)esriScreenCache.esriAllScreenCaches);
        }

        //Creates lastValueProperties Property Set which stores fields and values for last value method
        private void constructFieldArray()
        {
            // Assume we are in an edit session
            if (ArcMap.Editor.EditWorkspace == null) return;

            // Open defaults table            
            IDataset dataset = m_dynDefaults as IDataset;
            IQueryFilter qFilter = new QueryFilterClass();
            qFilter.WhereClause = "ValueMethod = 'LAST_VALUE'";

            lastValueProperties = new PropertySetClass();

            if (m_dynDefaults != null)
            {
                ICursor tabCursor = m_dynDefaults.Search(qFilter, true);
                IRow row = tabCursor.NextRow();
                while (row != null)
                {
                    object objRow = row.get_Value(dynTargetField);
                    object nullObject = null;
                    lastValueProperties.SetProperty(objRow.ToString(), nullObject);
                    row = tabCursor.NextRow();
                }
            }
        }

        private void setDynamicDefaults(IObject inObject, string mode)
        {
            try
            {
                //Convert row to feature (test for feature is null before using - this could be a table update)
                IFeature inFeature = inObject as IFeature;

                // Skip Orphan Junctions (saves time)
                if (inFeature != null)
                {
                    INetworkFeature inNetFeat = inObject as INetworkFeature;
                    if ((inNetFeat != null) &&
                        (inFeature.Class.ObjectClassID == inNetFeat.GeometricNetwork.OrphanJunctionFeatureClass.ObjectClassID))
                        return;
                }

                //Get cursor to dynamic values table retriving only 
                ICursor tabCursor;
                getDefaultRows(inObject, out tabCursor);
                IRow row = null;
                if (tabCursor != null)
                    row = tabCursor.NextRow();

                //for each row in the matching rows (matched by table name or wildcard) returned from the config table
                while (row != null)
                {
                    //get fieldname
                    string fieldName = row.get_Value(dynTargetField).ToString();

                    //if this field is found in the feature/object being added or modified...
                    int fieldNum = inObject.Fields.FindField(fieldName);
                    if (fieldNum > -1)
                    {
                        // get requested method and any data parameters
                        string valMethod = row.get_Value(dynMethodField).ToString();
                        string valData = row.get_Value(dynDataField).ToString();

                        switch (mode)
                        {
                            case "OnCreate":
                                //Continue to next field in config table if create events were not requested
                                if (row.get_Value(dynCreateField).ToString() == "0")
                                {
                                    row = tabCursor.NextRow();
                                    continue;
                                }
                                break;
                            case "OnChange":
                                // Collect value for changed feature (stored for LAST VALUE method)
                                IRowChanges inChanges = inObject as IRowChanges;
                                bool changed = inChanges.get_ValueChanged(fieldNum);
                                if (changed)
                                    lastValueProperties.SetProperty(fieldName, inObject.get_Value(fieldNum));
                                //Continue to next field in config table if change events were not requested
                                if (row.get_Value(dynChangeField).ToString() == "0")
                                {
                                    row = tabCursor.NextRow();
                                    continue;
                                }
                                break;
                        }

                        // set values as specified
                        switch (valMethod)
                        {
                            case "TIMESTAMP":

                                inObject.set_Value(fieldNum, DateTime.Now);
                                break;

                            case "LAST_VALUE":

                                if (mode == "OnCreate")
                                {
                                    //if (inObject.get_Value(fieldNum) == null)
                                    //{
                                    object lastValue = lastValueProperties.GetProperty(fieldName);
                                    if (lastValue != null)
                                        inObject.set_Value(fieldNum, lastValue);
                                    //}
                                }


                                break;

                            case "FIELD":

                                // verify that field to copy exists
                                int fieldCopy = inObject.Fields.FindField(valData as string);
                                if (fieldCopy > -1)
                                {
                                    //copy value only if current field is empty
                                    string currentValue = inObject.get_Value(fieldNum).ToString();
                                    if (currentValue == "")
                                        inObject.set_Value(fieldNum, inObject.get_Value(fieldCopy));
                                }
                                break;

                            case "CURRENT_USER":

                                if (lastEditorName == null)
                                    lastEditorName = getCurrentUser();
                                inObject.set_Value(fieldNum, lastEditorName);
                                break;

                            case "GUID":

                                if (mode == "OnCreate") // SHould only set this once on create to give the object a unique value
                                {
                                    object currentValue = inObject.get_Value(fieldNum);
                                    if (DBNull.Value == currentValue) // Do not overwrite if someone else has already generated
                                    {
                                        Guid g = Guid.NewGuid();
                                        inObject.set_Value(fieldNum, g.ToString("B").ToUpper());
                                    }
                                }

                                break;

                            case "EXPRESSION":

                                if (mode == "OnCreate")
                                {
                                    if (inFeature != null & valData != null)
                                    {
                                        try
                                        {
                                            int calcField = inFeature.Fields.FindField(fieldName);
                                            //if (inFeature.get_Value(calcField) == null)
                                            //{
                                            int[] fids = { inFeature.OID };
                                            IGeoDatabaseBridge gdbBridge = new GeoDatabaseHelperClass();
                                            IFeatureCursor fCursor = (IFeatureCursor)gdbBridge.GetFeatures((IFeatureClass)inFeature.Class, ref fids, false);

                                            ICalculator calc = new CalculatorClass();
                                            calc.Expression = valData;
                                            calc.Field = fieldName;
                                            calc.Cursor = (ICursor)fCursor;
                                            calc.ShowErrorPrompt = false;

                                            ICalculatorCallback calculatorCallback = new CalculatorCallback();
                                            calc.Callback = calculatorCallback;

                                            calc.Calculate();
                                            calculatorCallback = null;
                                            //}
                                        }
                                        catch { }
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                    }
                    row = tabCursor.NextRow();
                }

                if (null != tabCursor)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(tabCursor);
                }
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Error applying dynamic defaults.", ex.ToString());

                MessageBox.Show("Error: \n" + ex.ToString());
            }
        }

        //Returns rows from defaults table which match this object's table name or wildcard
        private void getDefaultRows(IObject inObject, out ICursor outCursor)
        {
            if (m_dynDefaults != null)
            {
                IDataset dataset = inObject.Class as IDataset;
                IQueryFilter qFilter = new QueryFilterClass();
                IQueryFilterDefinition qFilterDef = qFilter as IQueryFilterDefinition;
                //qFilterDef.PostfixClause = "ORDERBY TABLENAME";
                string[] items = dataset.Name.Split('.');
                string name = items[items.GetLength(0) - 1];
                qFilter.WhereClause = "TABLENAME = '" + name + "' or TABLENAME = '" + name.ToUpper() + "' or TABLENAME = '*'";
                outCursor = m_dynDefaults.Search(qFilter, true) as ICursor;
            }
            else
            {
                outCursor = null;
            }
            return;
        }


        //Current user is obtained edit workspace (if RDBMS) or from Windows
        private String getCurrentUser()
        {
            // Get the base class' workspace.

            IWorkspace workspace = ArcMap.Editor.EditWorkspace;
            int userFieldLength = 100;

            // If supported, use the IDatabaseConnectionInfo interface to get the username.
            IDatabaseConnectionInfo databaseConnectionInfo = workspace as IDatabaseConnectionInfo;
            if (databaseConnectionInfo != null)
            {
                String connectedUser = databaseConnectionInfo.ConnectedUser;
                if (connectedUser.ToUpper() != "DBO")
                {
                    // If the user name is longer than the user field allows, shorten it.
                    if (connectedUser.Length > userFieldLength)
                    {
                        connectedUser = connectedUser.Substring(0, userFieldLength);
                    }
                    return connectedUser;
                }
            }

            // Get the current Windows user.
            String userDomain = Environment.UserDomainName;
            String userName = Environment.UserName;
            String qualifiedUserName = String.Format(@"{0}\{1}", userDomain, userName);

            // If the user name is longer than the user field allows, shorten it.
            if (qualifiedUserName.Length > userFieldLength)
            {
                qualifiedUserName = qualifiedUserName.Substring(0, userFieldLength);
            }
            return qualifiedUserName;
        }

        #endregion

    }

}
