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
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.CatalogUI;
using System.Windows.Forms;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.esriSystem;

namespace Esri_Telecom_Tools.Helpers
{
    // -----------------------------------------------------
    // Responsible for maintaining a hook to the current 
    // chosen telecom workspace, getting a list of possible 
    // workspaces to choose from, and testing if a given 
    // workspace is a valid telecom workspace.
    //
    // Overrides and provides definition of abstract 
    // WorkspaceIsValid method. This method defines what 
    // needs to be done for any given derived class.
    // 
    // To help with future maintainability all code should 
    // use the current workspace and not go looking for data 
    // from layers in the map. Layers can come from multiple 
    // workspaces so we might be mixing and matching using 
    // this method. Also layers dont have to be in the map 
    // for the tools to work allowing for potentially 
    // thinned down maps for a better end user experience.
    // -----------------------------------------------------
    public class TelecomWorkspaceHelper : BaseWorkspaceHelper
    {
        private static TelecomWorkspaceHelper _instance = null;
        private string m_defaultsTableName = "DYNAMICVALUE";

        // ------------------------------------
        // All access is through singleton
        // ------------------------------------
        private TelecomWorkspaceHelper()
        {
        }

        public String dynamicValuesTableName()
        {
            return m_defaultsTableName;
        }

        // ------------------------------------
        // Use this static helper to get hold 
        // of a Telecom Workspace Helper.
        // ------------------------------------
        public static TelecomWorkspaceHelper Instance()
        {
            if(_instance == null)
            {
                _instance = new TelecomWorkspaceHelper();
            }
            return _instance;
        }

        protected override bool WorkspaceIsValid(IFeatureWorkspace fworkspace)
        {
            bool result = true;

            try
            {
                IWorkspace workspace = fworkspace as IWorkspace;
                ISQLSyntax sqlSyntax = (ISQLSyntax)workspace;

                // ----------------------------------------------
                // Need to get the db name & onwer. This is very 
                // important so we can deal with different types 
                // of table name qualification when dealing with 
                // enterprise and file geodatabases.Names are 
                // only fully qualified with enterprise GDBs.
                // ----------------------------------------------
                IWorkspace wksp = fworkspace as IWorkspace;
                IEnumDatasetName enumDatasetName = wksp.get_DatasetNames(esriDatasetType.esriDTAny);
                IDatasetName datasetName = enumDatasetName.Next();
                if (datasetName != null)
                {
                    datasetName = enumDatasetName.Next();
                    // Parse path name out into db, owner, table.
                    string db = string.Empty;
                    string owner = string.Empty;
                    string tbl = string.Empty;
                    sqlSyntax.ParseTableName(datasetName.Name, out db, out owner, out tbl);
                    _ownerName = owner;
                    _dbName = db;
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Database owner...", _ownerName);
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Database name...", _dbName);
                }
                else
                {
                    // Not a valid workspace if nothing found in it
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Invalid workspace selected.");
                    return false;
                }

                // ----------------------------------------------
                // Workspace is valid and is a feature workspace
                // ----------------------------------------------
                if (fworkspace == null || workspace == null)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Invalid workspace selected.");
                    return false;
                }
                else
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Reading Workspace...",
                        workspace.PathName + "," + workspace.Type.ToString());
                }

                // -----------------------------------
                // Are we dealing with a geodatabase 
                // ie not shapefiles etc
                // -----------------------------------
                if (workspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace &&
                    workspace.Type != esriWorkspaceType.esriLocalDatabaseWorkspace)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Telecom workspace is not a geodatabase.");
                    return false;
                }

                // --------------------------------
                // Get the workspace properties
                // --------------------------------
                IDatabaseConnectionInfo2 dbInfo = workspace as IDatabaseConnectionInfo2;

                IPropertySet wkPropSet = workspace.ConnectionProperties;
                object names;
                object values;
                wkPropSet.GetAllProperties(out names, out values);

                if (dbInfo != null)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Database Connection Info...");
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Connected DB: ", dbInfo.ConnectedDatabase);
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Connected User: ", dbInfo.ConnectedUser);
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "DB Type: ", dbInfo.ConnectionDBMS.ToString());
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Connection Server: ", dbInfo.ConnectionServer);
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "GDB Server Class: ", dbInfo.GeodatabaseServerClass.ToString());
                }                

                // ---------------------------------------
                // Does it have a dynamic values dataset?
                // ---------------------------------------
                IWorkspace2 wksp2 = fworkspace as IWorkspace2;
                if (!InWorkspace(wksp2, esriDatasetType.esriDTTable, m_defaultsTableName)) return false;

                // -------------------------------------------------
                // Do checks for other FCs and tables in app.config
                // -------------------------------------------------
                if (!InWorkspace(wksp2, esriDatasetType.esriDTFeatureClass, ConfigUtil.FiberCableFtClassName)) return false;
                if (!InWorkspace(wksp2, esriDatasetType.esriDTTable, ConfigUtil.FiberSpliceTableName)) return false;
                if (!InWorkspace(wksp2, esriDatasetType.esriDTTable, ConfigUtil.FiberTableName)) return false;
                if (!InWorkspace(wksp2, esriDatasetType.esriDTTable, ConfigUtil.BufferTubeClassName)) return false;
                // ----------
                // Devices
                // ----------
                string[] devices = ConfigUtil.DeviceFeatureClassNames;
                foreach (string name in devices)
                {
                    if (!InWorkspace(wksp2, esriDatasetType.esriDTFeatureClass, name)) return false;
                }

                // --------------------------------------------
                // Do a check for # Fibers and Strands domains
                // If these exist, DB will need upgrading first
                // --------------------------------------------
                IFeatureClass cableFc = FindFeatureClass(ConfigUtil.FiberCableFtClassName);
                if (cableFc == null) { return false; }
                int buffersIdx = cableFc.FindField(ConfigUtil.NumberOfBuffersFieldName);
                if (buffersIdx == -1) { return false; }
                IField field = cableFc.Fields.Field[buffersIdx];
                if (field.Domain != null)
                {
                    //MessageBox.Show("An invalid schema was found. Please read the " +
                    //    "documentation on how to upgrade old databases to work with the " +
                    //    "newer tools. Also view the log for more details. The log is " +
                    //    "accessible from telecom toolbar.", "Invalid Telecom Schema");

                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "DB Telecom Schema Version OK?", "False. Invalid Domain found on FiberCable.");


                    // Assume no until they say yes
                    DialogResult dialogResult = DialogResult.No;

                    dialogResult = MessageBox.Show("Do you wish to try to upgrade this database? \n\nPLEASE ENSURE YOU HAVE EXCLUSIVE ACCESS TO THIS DATABASE AND THAT NO SERVICES ARE RUNNING AGAINST IT \n\nPLEASE ALWAYS ENSURE YOU HAVE A BACKUP BEFORE CONSIDERING THIS!", "Upgrade Workspace", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (DialogResult.No == dialogResult)
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Database upgrade NO.", "");
                        return false;
                    }
                    else if (DialogResult.Yes == dialogResult)
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Database Upgrade YES.", "");
                        result = RemoveFiberCableConfigDomains(workspace);
                    }
                }
                else
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "DB Telecom Schema Version OK?", "True");
                }

            }
            catch (Exception e)
            {
                result = false;
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "WorkspaceIsValid", e.Message);
            }

            return result;
        }

        private bool RemoveFiberCableConfigDomains(IWorkspace wksp)
        {
            bool result = true;

            if(wksp == null || ((wksp as IWorkspaceDomains3) == null)) return false;

            try
            {
                // get Handles to everything we need first or bail.
                IFeatureClass cableFc = FindFeatureClass(ConfigUtil.FiberCableFtClassName);
                if (cableFc == null) { return false; }
                ISubtypes subs = cableFc as ISubtypes;
                if (subs == null) { return false; }
                IClassSchemaEdit4 schEdit = cableFc as IClassSchemaEdit4;
                if (schEdit == null) { return false; }

                // ----------------------------------------------------
                // First we have to unassign the domains from the FC fields
                // ----------------------------------------------------
                schEdit.AlterDomain(ConfigUtil.NumberOfFibersFieldName, null);
                schEdit.AlterDomain(ConfigUtil.NumberOfBuffersFieldName, null);
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Unassigned FiberCable domains", "NumberOfFibers + NumberOfBuffers");

                // ----------------------------------------------------
                // Also have to remove domain from the subtypes (arghh)
                // 1 & 2 are overhead and underground.
                // ----------------------------------------------------
                IEnumSubtype types = subs.Subtypes;
                subs.set_Domain(1, ConfigUtil.NumberOfFibersFieldName, null);
                subs.set_Domain(2, ConfigUtil.NumberOfFibersFieldName, null);
                subs.set_Domain(1, ConfigUtil.NumberOfBuffersFieldName, null);
                subs.set_Domain(2, ConfigUtil.NumberOfBuffersFieldName, null);
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Removed FiberCable subtype domains", "NumberOfFibers + NumberOfBuffers");

                // ----------------------------------------------------
                // Now we can remove the domains 
                // ----------------------------------------------------
                IWorkspaceDomains3 wkspDomains = wksp as IWorkspaceDomains3;
                if (wkspDomains.get_CanDeleteDomain("NumberOfFibers") &&
                    wkspDomains.get_CanDeleteDomain("NumberOfBuffers"))
                {
                    wkspDomains.DeleteDomain("NumberOfFibers");
                    wkspDomains.DeleteDomain("NumberOfBuffers");
                    MessageBox.Show("Domains successfully deleted");
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Domains successfully deleted", "NumberOfFibers + NumberOfBuffers");
                }
                else
                {
                    MessageBox.Show("Upgrade failed. \nCould not get exclusive access to this database. \nPLEASE RESTORE FROM YOUR BACKUP");
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Cannot delete domains.","Non-Exclusive access?");
                    return false;
                }

                // ----------------------------------------------------
                // Recalculate the fields based on the count of actual 
                // related objects found. We'll use this code later 
                // for integrity checking.
                // ----------------------------------------------------
                IRelationshipClass bufferRelationship = GdbUtils.GetRelationshipClass(cableFc, ConfigUtil.FiberCableToBufferRelClassName);
                IRelationshipClass strandRelationship = GdbUtils.GetRelationshipClass(cableFc, ConfigUtil.FiberCableToFiberRelClassName);
                IFeature ft;
                int bufferIdx = cableFc.Fields.FindField(ConfigUtil.NumberOfBuffersFieldName);
                int strandIdx = cableFc.Fields.FindField(ConfigUtil.NumberOfFibersFieldName);
                if (bufferIdx == -1 || strandIdx == -1)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Cannot find buffer or strand fields.", 
                        ConfigUtil.NumberOfBuffersFieldName + " " + ConfigUtil.NumberOfFibersFieldName);
                    MessageBox.Show("Upgrade failed. \nCould not find appropriate fields based on current config settings. \nPLEASE RESTORE FROM YOUR BACKUP.");
                    return false;
                }
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Recalculating buffer and strand counts...");
                // Start edit session do the updates....
                ArcMap.Editor.StartEditing(wksp);
                IFeatureCursor cables = cableFc.Update(null, false);
                int count = cableFc.FeatureCount(null);

                //ProgressBar
                ESRI.ArcGIS.Framework.IProgressDialogFactory progressDialogFactory = new ESRI.ArcGIS.Framework.ProgressDialogFactoryClass();

                ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel = new ESRI.ArcGIS.Display.CancelTrackerClass();

                // Set the properties of the Step Progressor
                ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = progressDialogFactory.Create(trackCancel, ArcMap.Application.hWnd);
                stepProgressor.MinRange = 1;
                stepProgressor.MaxRange = count;
                stepProgressor.StepValue = 1;
                stepProgressor.Message = "Updating cable config for " + count + " cables";

                // Create the ProgressDialog. This automatically displays the dialog
                ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog = (ESRI.ArcGIS.Framework.IProgressDialog2)stepProgressor; // Explict Cast

                // Set the properties of the ProgressDialog
                progressDialog.CancelEnabled = false;
                progressDialog.Description = "";
                progressDialog.Title = "Workspace Upgrade";
                progressDialog.Animation = ESRI.ArcGIS.Framework.esriProgressAnimationTypes.esriProgressGlobe;
                progressDialog.ShowDialog();

                int i = 1;
                while ((ft = cables.NextFeature()) != null)
                {
                    progressDialog.Description = string.Format("Updating cable {0} of {1}", i, count);
                    stepProgressor.Step();

                    ISet buffers = bufferRelationship.GetObjectsRelatedToObject(ft);
                    ft.set_Value(bufferIdx,buffers.Count);
                    ISet strands = strandRelationship.GetObjectsRelatedToObject(ft);
                    ft.set_Value(strandIdx, strands.Count);
                    ft.Store();
                    i++;
                }
                progressDialog.HideDialog();
                ArcMap.Editor.StopEditing(true);
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Upgrade Completed Successfully.");
                MessageBox.Show("Upgrade Completed Successfully.");
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "RemoveFiberCableConfigDomains", e.Message);
                result = false;
            }
            return result;
        }
    }
}
