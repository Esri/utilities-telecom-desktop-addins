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

using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Desktop.AddIns;
using Esri_Telecom_Tools.Helpers;
using Esri_Telecom_Tools.Core.Utils;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Core;
using System.Runtime.InteropServices;


namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class FiberTraceWindow : UserControl
    {
        private FiberTraceHelper _fiberTraceHelper = null;
        private HookHelperExt _hookHelper = null;

        private LogHelper _logHelper = LogHelper.Instance();

        private bool _isTraceInProgress = false;

        private static System.Text.RegularExpressions.Regex _isNumber = new System.Text.RegularExpressions.Regex(@"^\d+$");

        public FiberTraceWindow(object hook)
        {
            InitializeComponent();
            this.Hook = hook;

            _hookHelper = HookHelperExt.Instance(hook);
        }

        void _wkspHelper_WorkspaceClosed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void _wkspHelper_ValidWorkspaceSelected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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
            private FiberTraceWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal FiberTraceWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new FiberTraceWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }
        }

        public void InitFiberTrace(FiberTraceHelper helper)
        {
            _fiberTraceHelper = helper;

            // ----------------------------
            // Populate dropdown with any 
            // currently selection 
            // ----------------------------
            PopulateFeatures();

            // ------------------------------
            // Listen for selection events 
            // & enable selection tool
            // ------------------------------
            _hookHelper.ExecuteSelectionTool();
            _fiberTraceHelper.SelectionChanged -= new EventHandler(_fiberTraceHelper_SelectionChanged);
            _fiberTraceHelper.SelectionChanged += new EventHandler(_fiberTraceHelper_SelectionChanged);

            // ------------------------------
            // Listen for end of trace events
            // ------------------------------
            _fiberTraceHelper.TraceCompleted -= new EventHandler(_fiberTraceHelper_TraceCompleted);
            _fiberTraceHelper.TraceCompleted += new EventHandler(_fiberTraceHelper_TraceCompleted);

            cboPortType.SelectedIndex = 0;
        }

        void _fiberTraceHelper_SelectionChanged(object sender, EventArgs e)
        {
            if (!_isTraceInProgress)
            {
                PopulateFeatures();

                txtUnit.Text = "";
                txtUnit.Enabled = (null != cboExisting.SelectedItem);
            }
        }

        void _fiberTraceHelper_TraceCompleted(object sender, EventArgs e)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Trace Complete");

            List<ESRI.ArcGIS.Geodatabase.IRow> results = _fiberTraceHelper.TraceResults;

            if (showReportCheckBox.Checked == true)
            {
                UID dockWinID = new UIDClass();
                dockWinID.Value = @"esriTelcoTools_FiberTraceReportWindow";
                IDockableWindow dockWindow = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
                dockWindow.Show(true);

                // Show a report
                FiberTraceReportWindow.AddinImpl winImpl = 
                    AddIn.FromID<FiberTraceReportWindow.AddinImpl>(
                    ThisAddIn.IDs.Esri_Telecom_Tools_Windows_FiberTraceReportWindow);
                FiberTraceReportWindow traceReportWindow = winImpl.UI;
                traceReportWindow.InitReport(_fiberTraceHelper);  // Change this to a report helper
                traceReportWindow.PopulateReport(results);
            }

            // Select the traced features
            _fiberTraceHelper.SelectTracedDevices();
            _fiberTraceHelper.SelectTracedSpliceClosures();
            _fiberTraceHelper.SelectTracedFiberCables();
        }

        /// <summary>
        /// Loads the drop down of selected features for the user to trace from
        /// </summary>
        private void PopulateFeatures()
        {
            if (!_isTraceInProgress)
            {
                cboExisting.Items.Clear();

                // Populate combo box with cables...
                List<FiberCableWrapper> cables = _fiberTraceHelper.SelectedCables;
                for (int i = 0; i < cables.Count; i++)
                {
                    cboExisting.Items.Add(cables[i]);
                }

                // Populate combo box with devices...
                List<DeviceWrapper> devices = _fiberTraceHelper.SelectedDevices;
                for (int i = 0; i < devices.Count; i++)
                {
                    cboExisting.Items.Add(devices[i]);
                }

                if (0 < cboExisting.Items.Count)
                {
                    cboExisting.SelectedIndex = 0;
                }

                btnTrace.Enabled = IsValid;
            }
        }

        /// <summary>
        /// The link to flash the feature has been clicked
        /// </summary>
        private void lblFlashFrom_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboExisting.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _fiberTraceHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// Checks if a string is an integer against a regular expression 
        /// </summary>
        /// <param name="theValue">string to check</param>
        /// <returns>bool</returns>
        private bool IsInteger(string theValue)
        {
            bool result = false;

            if (null != theValue && 0 < theValue.Length)
            {
                System.Text.RegularExpressions.Match m = _isNumber.Match(theValue);
                result = m.Success;
            }

            return result;
        }

        /// <summary>
        /// Property to determine if all required inputs are specified
        /// </summary>
        private bool IsValid
        {
            get
            {
                return ((null != cboExisting.SelectedItem)
                    && IsInteger(txtUnit.Text));
            }
        }

        /// <summary>
        /// The user input has changed, check if it is valid
        /// </summary>
        private void txtFiberNumber_TextChanged(object sender, EventArgs e)
        {
            btnTrace.Enabled = IsValid;
        }

        /// <summary>
        /// The user has selected a different feature to trace from
        /// </summary>
        private void cboExisting_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnTrace.Enabled = IsValid;

            if (cboExisting.SelectedItem is FiberCableWrapper)
            {
                lblPortType.Enabled = false;
                cboPortType.Enabled = false;
            }
            else if (cboExisting.SelectedItem is DeviceWrapper)
            {
                lblPortType.Enabled = true;
                cboPortType.Enabled = true;
            }
        }

        /// <summary>
        /// The user has clicked the trace button
        /// </summary>
        private void btnTrace_Click(object sender, EventArgs e)
        {
            if (null != cboExisting.SelectedItem)
            {
                if (IsInteger(txtUnit.Text))
                {
                    int unit = System.Convert.ToInt32(txtUnit.Text);

                    try
                    {
                        if (cboExisting.SelectedItem is FiberCableWrapper)
                        {
                            _isTraceInProgress = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Trace Initiated");
                            _fiberTraceHelper.TraceTriggered((FiberCableWrapper)cboExisting.SelectedItem, unit);
                            _isTraceInProgress = false;
                        }
                        else if (cboExisting.SelectedItem is DeviceWrapper)
                        {
                            _isTraceInProgress = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Trace Initiated");
                            _fiberTraceHelper.TraceTriggered((DeviceWrapper)cboExisting.SelectedItem, unit, cboPortType.SelectedIndex == 0 ? PortType.Input : PortType.Output);
                            _isTraceInProgress = false;
                        }
                    }
                    catch
                    {
                        // Logging?
                    }
                    finally
                    {
                        _isTraceInProgress = false;
                    }
                }
                else
                {
                    MessageBox.Show("Specify a valid unit.", "Telecom Trace", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Select a feature.", "Telecom Trace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
