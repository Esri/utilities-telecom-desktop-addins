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

using ESRI.ArcGIS.Geodatabase;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Helpers;
using Esri_Telecom_Tools.Core.Utils;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class FiberTraceReportWindow : UserControl
    {
        private FiberTraceHelper _fiberTraceHelper = null;
        private LogHelper _logHelper = null;

        public FiberTraceReportWindow(object hook)
        {
            InitializeComponent();
            this.Hook = hook;

            _logHelper = LogHelper.Instance();
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
            private FiberTraceReportWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal FiberTraceReportWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new FiberTraceReportWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }

        }

        public void InitReport(FiberTraceHelper hookHelper)
        {
            _fiberTraceHelper = hookHelper;
        }


        public void PopulateReport(List<ESRI.ArcGIS.Geodatabase.IRow> traceResults)
        {
            // Get the report fields that have been specificed in XML.
            Dictionary<string,List<string>> fieldsDict = ConfigUtil.FiberTraceReportFields();

            treeView1.SuspendLayout();
            treeView1.Nodes.Clear();
            TreeNode parent = null;
            int idx = 0;
            foreach(IRow traceItem in traceResults)
            {
                ESRI.ArcGIS.Geodatabase.IDataset dataset = traceItem.Table as ESRI.ArcGIS.Geodatabase.IDataset;
                string className = GdbUtils.ParseTableName(dataset);

                TreeNode node = new TreeNode(className);

                // If row is a feature add feature as tag with click event
                if (traceItem is IFeature)
                {
                    node.BackColor = Color.LightSeaGreen;
                    node.Tag = traceItem as IFeature;
                }

                // If start node highlight in yellow
                if (_fiberTraceHelper.StartedOnFiber)
                {
                    if (idx == _fiberTraceHelper.StartedFiberIndex)
                    {
                        node.BackColor = Color.Yellow;
                    }
                }

                // Do attributes only if in report xml
                if (fieldsDict.ContainsKey(className))
                {
                    List<string> fields = fieldsDict[className];
                    // Build the details for the node
                    List<NameValuePair> props = GdbUtils.PropertySet(traceItem);
                    foreach (NameValuePair prop in props)
                    {
                        // show all attributes
                        if (fields.Count == 1 && (fields[0].CompareTo("*") == 0))
                        {
                            TreeNode details = new TreeNode(prop.Alias + ": " + prop.Value);
                            node.Nodes.Add(details);
                        }
                        // show selected attributes
                        else if (fields.Contains(prop.Name))
                        {
                            TreeNode details = new TreeNode(prop.Alias + ": " + prop.Value);
                            node.Nodes.Add(details);
                        }
                    }
                }
                                
                if (parent == null)
                {
                    parent = node;
                    // This is the first node in tree
                    treeView1.Nodes.Add(node);
                }
                else
                {
                    parent.Nodes.Add(node);

                    // Indent for major items (devices and splice closures)
                    if (ConfigUtil.IsDeviceClassName(className) ||
                        (0 == string.Compare(className, ConfigUtil.SpliceClosureFtClassName, true)))
                    {
                        // This becomes new parent
                        parent = node;
                    }
                }
                idx++;
            }
            treeView1.ResumeLayout();
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button == MouseButtons.Right)
            {

                // Point where the mouse is clicked.
                Point p = new Point(e.X, e.Y);

                // Get the node that the user has clicked.
                TreeNode node = treeView1.GetNodeAt(p);
                if (node != null)
                {

                    // Select the node the user has clicked.
                    // The node appears selected until the menu is displayed on the screen.
                    treeView1.SelectedNode = node;

                    if(node.Tag != null)
                    {
                        contextMenuStrip1.Show(treeView1,p);
                    }
                }
            }
        }

        private void flashFeatureMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                TreeNode sel = treeView1.SelectedNode;
                if (sel.Tag != null)
                {
                    _fiberTraceHelper.FlashFeature(sel.Tag as IFeature);
                }
            }
        }

    }
}
