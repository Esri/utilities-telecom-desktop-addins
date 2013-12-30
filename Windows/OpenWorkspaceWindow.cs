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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Esri_Telecom_Tools.Helpers;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.esriSystem;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class OpenWorkspaceWindow : UserControl
    {
        private LogHelper _logger = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        public OpenWorkspaceWindow(object hook)
        {
            InitializeComponent();
            this.Hook = hook;

            // Add hook to listen for workspace changes
            _wkspHelper.ActiveViewChanged += new EventHandler(_wkspHelper_ActiveViewChanged);
            _wkspHelper.ItemAdded += new EventHandler(_wkspHelper_ItemAdded);
            _wkspHelper.ItemDeleted += new EventHandler(_wkspHelper_ItemDeleted);
            _wkspHelper.ValidWorkspaceSelected += new EventHandler(_wkspHelper_WorkspaceSelected);
            _wkspHelper.WorkspaceClosed += new EventHandler(_wkspHelper_WorkspaceClosed);

            // Do initial population of info
            populateWorkspaces();
        }

        void _wkspHelper_WorkspaceClosed(object sender, EventArgs e)
        {
            this.currentWorkspaceLabel.Text = "<Not Set>";
        }

        void _wkspHelper_WorkspaceSelected(object sender, EventArgs e)
        {
            this.currentWorkspaceLabel.Text = (_wkspHelper.CurrentWorkspace as IWorkspace).PathName;
        }

        private void populateWorkspaces()
        {
            // Populate the dialog with valid fiber cable configurations
            IList<IFeatureWorkspace> workspaces = _wkspHelper.Workspaces;
            listView1.SuspendLayout();
            listView1.Items.Clear();
            foreach (IWorkspace wksp in workspaces)
            {
                ListViewItem item = new ListViewItem();
                item.Tag = wksp;
                item.Text = wksp.PathName;
                item.SubItems.Add(wksp.Type.ToString());
                item.SubItems.Add(wksp.Exists().ToString());
                listView1.Items.Add(item);
            }
            listView1.ResumeLayout();
            //if (listView1.Items[0] != null) { listView1.Items[0].Selected = true; }
            //listView1.HideSelection = false;
        }

        void _wkspHelper_ItemAdded(object sender, EventArgs e)
        {
            populateWorkspaces();
        }

        void _wkspHelper_ItemDeleted(object sender, EventArgs e)
        {
            populateWorkspaces();
        }

        void _wkspHelper_ActiveViewChanged(object sender, EventArgs e)
        {
            populateWorkspaces();
        }

        /// <summary>
        /// Host object of the dockable window
        /// </summary>
        private object Hook
        {
            get;
            set;
        }

        /// <summary>
        /// Implementation class of the dockable window add-in. It is responsible for 
        /// creating and disposing the user interface class of the dockable window.
        /// </summary>
        [ComVisible(false)]
        public class AddinImpl : ESRI.ArcGIS.Desktop.AddIns.DockableWindow
        {
            private OpenWorkspaceWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal OpenWorkspaceWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new OpenWorkspaceWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listView1.SelectedItems;
            if (items.Count == 1)
            {
                SelectWorkspaceButton.Enabled = true;
            }
            else
            {
                SelectWorkspaceButton.Enabled = false;
            }
        }

        private void SelectWorkspaceButton_Click(object sender, EventArgs e)
        {
            // There should only be one selected.
            ListView.SelectedListViewItemCollection items = listView1.SelectedItems;
            IFeatureWorkspace wksp = items[0].Tag as IFeatureWorkspace;
            bool result = _wkspHelper.OpenWorkspace(wksp);
            if (result == false)
            {
                MessageBox.Show("Invalid telecom workspace detected. \nSee log for details. \nPlease select another workspace", "Open Workspace", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                // Stupid that i have to do this. 
                // Why cant i easily hide this dialog from within this class
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_OpenWorkspaceWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(false);
            }
        }

    }
}
