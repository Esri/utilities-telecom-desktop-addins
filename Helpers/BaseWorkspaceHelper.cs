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
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.CatalogUI;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;

namespace Esri_Telecom_Tools.Helpers
{
    // -----------------------------------------------------
    // Responsible for maintaining a hook to the current 
    // chosen telecom workspace, getting a list of possible 
    // workspaces to choose from, and testing if a given 
    // workspace is a valid telecom workspace.
    //
    // Testing method is a abstract method, derived classes 
    // should be defined to do actual workspace checking.
    //
    // Listens to the ActiveViewChanged event and 
    // raises the ActiveViewChanged event for clients of this 
    // helper.
    //
    // Raises WorkspaceSelected whenever a workspace is set.
    // 
    // To help with future maintainability all code should 
    // use the current workspace and not go looking for data 
    // from layers in the map. Layers can come from multiple 
    // workspaces so we might be mixing and matching using 
    // this method. Also layers dont have to be in the map 
    // for the tools to work allowing for potentially 
    // thinned down maps for a better end user experience.
    //
    // -----------------------------------------------------
    public abstract class BaseWorkspaceHelper
    {
        // For logging messages
        protected static LogHelper _logHelper = null;

        // Reference to current workspace and validity flag.
        protected static IFeatureWorkspace _currentWorkspace = null;
        protected bool _workspaceIsValid = false;

        // Qualifying name parts for dealing with RDBMS systems
        protected String _dbName = String.Empty;
        protected String _ownerName = String.Empty;

        // To cache lookups on feature classes & tables
        protected Dictionary<String, IFeatureClass> _featureClasses = new Dictionary<string, IFeatureClass>();
        protected Dictionary<String, ITable> _tables = new Dictionary<string, ITable>();

        // Signal updates to client of this
        public event EventHandler ActiveViewChanged; // Fired when active view changes
        public event EventHandler ItemAdded; // Fired when data is added
        public event EventHandler ItemDeleted; // Fired when data is removed
        public event EventHandler ValidWorkspaceSelected; // Fired when a workspace is valid and selected
        public event EventHandler WorkspaceClosed; // Fired when the current workspace is closed

        protected BaseWorkspaceHelper()
        {
            _logHelper = LogHelper.Instance();

            // Grab a hook into the map
            if(ArcMap.Document.FocusMap != null)
            {
                IActiveViewEvents_Event events = ArcMap.Document.FocusMap as IActiveViewEvents_Event;
                events.ItemAdded += new IActiveViewEvents_ItemAddedEventHandler(Events_ActiveViewItemAdded);
                events.ItemDeleted += new IActiveViewEvents_ItemDeletedEventHandler(Events_ActiveViewItemDeleted);
            }
            ArcMap.Events.ActiveViewChanged += new IDocumentEvents_ActiveViewChangedEventHandler(Events_ActiveViewChanged);
        }

        private void Events_ActiveViewChanged()
        {
            if (ActiveViewChanged != null)
            {
                ActiveViewChanged(this, null);
            }
        }

        private void Events_ActiveViewItemAdded(object item)
        {
            if (ItemAdded != null)
            {
                ItemAdded(this, null);
            }
        }

        private void Events_ActiveViewItemDeleted(object item)
        {
            if (ItemDeleted != null)
            {
                ItemDeleted(this, null);
            }
        }

        public IList<IFeatureWorkspace> Workspaces
        {
            get
            {
                // Get a list of workspaces from the active view to test?
                IList<IFeatureWorkspace> workspaces = new List<IFeatureWorkspace>();
                IMxDocument doc = ArcMap.Document;
                IDocumentDatasets datasets = doc as IDocumentDatasets;
                IEnumDataset ds = datasets.Datasets;
                IDataset d;
                while ((d = ds.Next()) != null)
                {
                    IWorkspace wksp = null;
                    try
                    {
                        wksp = d.Workspace;
                    }
                    catch (Exception e)
                    {
                        _logHelper.addLogEntry(DateTime.Now.ToString(), "WARNING", "Invalid workspace found in current document",d.Name);
                    }
                    finally
                    {
                        if (wksp != null)
                        {
                            IFeatureWorkspace fWkSp = wksp as IFeatureWorkspace;
                            if (fWkSp != null)
                            {
                                if (!workspaces.Contains(fWkSp))
                                    workspaces.Add(fWkSp);
                            }
                        }
                    }
                }

                return workspaces;
            }
        }

        protected abstract bool WorkspaceIsValid(IFeatureWorkspace fworkspace);

        public IFeatureWorkspace CurrentWorkspace
        {
            get
            {
                return _currentWorkspace;
            }
        }

        /// <summary>
        /// Attempts to open a given feature workspace as a 
        /// telecom workspace. Various checks are done to see 
        /// if this location is valid for editing.
        /// 
        /// Any existing valid workspaces are closed.
        /// 
        /// Raises ValidWorkspaceSelected event if workspace is valid.
        /// </summary>
        /// <param name="wksp">The telecom workspace to be opened.</param>
        /// <returns>True if workspace was successfully opened, otherwise False</returns>
        public bool OpenWorkspace(IFeatureWorkspace wksp)
        {
            // If current workspace is valid and open the close it first.
            if (_currentWorkspace != null && _workspaceIsValid)
            {
                CloseCurrentWorkspace();
            }

            // Assume workspace is valid until we can prove invalid.
            // _currentWorkspace used in some utility functions so 
            // make sure this is set before checking validity.
            _currentWorkspace = wksp;
            _workspaceIsValid = true;

            try
            {
                if (WorkspaceIsValid(wksp))
                {
                    _workspaceIsValid = true;
                    if (this.ValidWorkspaceSelected != null)
                    {
                        ValidWorkspaceSelected(this, null);
                    }
                }
                else
                {
                    _currentWorkspace = null;
                    _workspaceIsValid = false;
                }
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Open Workspace", e.Message);
            }

            return _workspaceIsValid;
        }

        public void CloseCurrentWorkspace()
        {
            _currentWorkspace = null;
            _workspaceIsValid = false;

            // Clear any cached workspace info
            // Handles to FCs and Tables
            _featureClasses.Clear();
            _tables.Clear();

            if (WorkspaceClosed != null)
                WorkspaceClosed(this, null);
        }

        public bool CurrentWorkspaceIsValid
        {
            get
            {
                return _workspaceIsValid;
            }
        }

        protected bool InWorkspace(IWorkspace2 wksp2, esriDatasetType type, String name)
        {
            ISQLSyntax sqlSyntax = (ISQLSyntax)wksp2;
            String fqname = sqlSyntax.QualifyTableName(_dbName, _ownerName, name);

            bool result = true;
            IWorkspace workspace = wksp2 as IWorkspace;

            if (wksp2.get_NameExists(type, fqname))
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Exists check: " + fqname, "PASS");
            }
            else
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Exists check: " + fqname, "FAIL");
                return false;
            }

            return result;
        }

        public IFeatureClass FindFeatureClass(String name)
        {
            if (name == null || 1 > name.Length)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FindFeatureClass", "Name not set.");
                throw new ArgumentException("table name not specified");
            }

            if (_currentWorkspace == null)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FindFeatureClass", "Current workspace not set.");
                throw new Exception("Telecom Workspace Is Not Set.");
            }

            // Dictionary cached lookup
            IFeatureClass value;
            if (_featureClasses.ContainsKey(name))
                value = _featureClasses[name];
            else
            {
                value = FindTable(name) as IFeatureClass;

                // Store the IFeatureClass ref in the cache.
                _featureClasses.Add(name, value);
            }
            return value;
        }

        /// <summary>
        /// Attempts to open a given table
        /// </summary>
        /// <param name="tablename">The unqualified name of the table to be opened. 
        /// The table name will be qualfied within this function</param>
        /// <returns>A reference to the ITable</returns>
        public ITable FindTable(String name)
        {
            if (name == null || 1 > name.Length)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FindTable", "Name not set.");
                throw new ArgumentException("table name not specified");
            }

            if (_currentWorkspace == null)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FindTable", "Current workspace not set.");
                throw new Exception("Telecom workspace is not set.");
            }

            // Dictionary cached lookup
            ITable value = null;
            if (_tables.ContainsKey(name))
                value = _tables[name];
            else
            {
                // get qualified name from connection properties.
                ISQLSyntax sqlsyntax = _currentWorkspace as ISQLSyntax;
                string qualifiedName = sqlsyntax.QualifyTableName(_dbName, _ownerName, name);
                value = _currentWorkspace.OpenTable(qualifiedName);

                // Store the ITable ref in the cache.
                _tables.Add(name, value);
            }
            return value;
        }

    }
}
