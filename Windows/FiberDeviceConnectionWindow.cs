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
using Esri_Telecom_Tools.Core;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Core.Utils;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class FiberDeviceConnectionWindow : UserControl
    {
        private FiberDeviceConnectionHelper _connectionHelper = null;
        private HookHelperExt _hookHelper = null;
        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private bool _isEditing = false;

        // preserve a record of unit to unit connections that were loaded into a grid for the given features
        private Dictionary<int, int> _original = new Dictionary<int, int>();
        // preserve a record of unit to unit connections that were deleted from the grid
        private Dictionary<int, int> _deleted = new Dictionary<int, int>();

        // These are used for the unsaved edits prompt, to let us revert to previous selections since the combobox doesn't
        // have a cancelable Changing event.
        private bool _isReverting = false;
        private int _lastSelectedFromIdx = -1;
        private int _lastSelectedToIdx = -1;


        public FiberDeviceConnectionWindow(object hook)
        {
            InitializeComponent();
            this.Hook = hook;

            _hookHelper = HookHelperExt.Instance(hook);

            // --------------------------------------------
            // Listen to workspace changes. Wire up events 
            // only when a valid workspace is selected.
            // --------------------------------------------
            _wkspHelper.ValidWorkspaceSelected += new EventHandler(_wkspHelper_ValidWorkspaceSelected);
            _wkspHelper.WorkspaceClosed += new EventHandler(_wkspHelper_WorkspaceClosed);
        }

        void _wkspHelper_WorkspaceClosed(object sender, EventArgs e)
        {
            // Stop listening for selection changes
            _hookHelper.ActiveViewSelectionChanged -= new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(helper_ActiveViewSelectionChanged);
        }

        void _wkspHelper_ValidWorkspaceSelected(object sender, EventArgs e)
        {
            // Listen for selection changes
            _hookHelper.ActiveViewSelectionChanged += new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(helper_ActiveViewSelectionChanged);
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
            private FiberDeviceConnectionWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal FiberDeviceConnectionWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new FiberDeviceConnectionWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }

        }

        /// <summary>
        /// To prepare and display the form to the user
        /// </summary>
        /// <param name="connectionHelper">Class providing helper methods for connections</param>
        public void DisplayConnections(FiberDeviceConnectionHelper connectionHelper, HookHelperExt hookHelper)
        {
            _connectionHelper = connectionHelper;
            _hookHelper = hookHelper;

            // Changes the GUI appropriately
            SetEditState(_isEditing);

            // Populate drop downs with splice closure and device info.
            PopulateDropDowns(connectionHelper);
        }

        /// <summary>
        /// Flag for whether to provide the UI elements for editing; when false, the grid is read-only
        /// </summary>
        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                _isEditing = value;
                SetEditState(value);
            }
        }

        /// <summary>
        /// Reset form controls to allow or disallow editing
        /// </summary>
        /// <param name="isEditing"></param>
        private void SetEditState(bool isEditing)
        {
            if (isEditing)
            {
                this.Text = "Connections Editor";

                grdConnections.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

                // We store true/false in the tag as to whether edits were made to the current grid rows. 
                if (null != btnSave.Tag)
                {
                    btnSave.Enabled = (bool)btnSave.Tag;
                }
            }
            else
            {
                this.Text = "Connections Viewer";

                grdConnections.EditMode = DataGridViewEditMode.EditProgrammatically;

                // Cannot save when not editing
                btnSave.Enabled = false;
            }

            grdConnections.AllowUserToAddRows = isEditing;
            grdConnections.AllowUserToDeleteRows = isEditing;
        }

        /// <summary>
        /// Map selection changed, update the cable choices
        /// </summary>
        void helper_ActiveViewSelectionChanged()
        {
            // Window not fully initialized or visible.
            if (!this.Visible || _connectionHelper == null) return;

            try
            {
                // Save changes option if the selection change with unsaved edits.
                if (btnSave.Enabled)
                {
                    FeatureWrapper from = cboFrom.SelectedItem as FeatureWrapper;
                    FeatureWrapper to = cboTo.SelectedItem as FeatureWrapper;

                    IsUserSure(from, to);
                }

                // Populate with new information
                ClearGrid();
                PopulateDropDowns(_connectionHelper);
                List<Connection> list = getConnections();
                LoadExistingRecords(list);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error gathering connection information: \n" + e.ToString());
            }
        }

        /// <summary>
        /// Update the dropdowns of features on the UI
        /// </summary>
        /// <param name="helper">Helper class</param>
        private void PopulateDropDowns(FiberDeviceConnectionHelper helper)
        {
            // Clear the existing choices
            cboFrom.Items.Clear();
            cboTo.Items.Clear();
            lblAvailableFrom.Text = "";
            lblAvailableTo.Text = "";

            // Get the features and the display index for use when creating the wrapper
            ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName);
            int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

            // Get the selected features
            List<ESRI.ArcGIS.Geodatabase.IFeature> selectedFts = _hookHelper.GetSelectedFeatures(ftLayer);

            // Add each of the fiber features to the drop down
            for (int ftIdx = 0; ftIdx < selectedFts.Count; ftIdx++)
            {
                FiberCableWrapper w = new FiberCableWrapper(selectedFts[ftIdx], displayIdx);
                cboFrom.Items.Add(w);
            }

            // Now do that same thing for each of the device feature classes
            string[] deviceClassNames = ConfigUtil.DeviceFeatureClassNames;
            for (int deviceClassIdx = 0; deviceClassIdx < deviceClassNames.Length; deviceClassIdx++)
            {
                string ftClassName = deviceClassNames[deviceClassIdx];

                // Get the features and the display index for use when creating the wrapper
                ftLayer = _hookHelper.FindFeatureLayer(ftClassName);
                if (ftLayer != null) // what if layer removed from map
                {
                    selectedFts = _hookHelper.GetSelectedFeatures(ftLayer);

                    displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

                    for (int ftIdx = 0; ftIdx < selectedFts.Count; ftIdx++)
                    {
                        DeviceWrapper w = new DeviceWrapper(selectedFts[ftIdx], displayIdx);
                        cboFrom.Items.Add(w);
                    }
                }
            }

            // Preselect the first choice
            if (0 < cboFrom.Items.Count)
            {
                cboFrom.SelectedItem = cboFrom.Items[0];
            }
        }

        /// <summary>
        /// Resets the grid and caches of what was on it
        /// </summary>
        private void ClearGrid()
        {
            grdConnections.Rows.Clear();

            _original.Clear();
            _deleted.Clear();

            btnSave.Enabled = false; // Obviously if there is no data, it cannot be saved
            btnSave.Tag = false; // No edits have been made
        }

        /// <summary>
        /// Converts a list of ranges into a string for display on the form
        /// </summary>
        /// <param name="availableRanges">List of available ranges</param>
        /// <returns>string</returns>
        private string GetAvailableRangesString(List<Range> availableRanges)
        {
            if (null == availableRanges)
            {
                throw new ArgumentNullException("availableRanges");
            }

            string result = "No available ranges!";

            if (0 < availableRanges.Count)
            {
                StringBuilder availableLabel = new StringBuilder(64);
                for (int i = 0; i < availableRanges.Count; i++)
                {
                    availableLabel.AppendFormat("{0},", availableRanges[i].ToString());
                }
                availableLabel.Remove(availableLabel.Length - 1, 1);
                result = string.Format("Available: {0}", availableLabel.ToString());
            }

            return result;
        }

        /// <summary>
        /// Reloads the grid with a given list of connections
        /// </summary>
        /// <param name="connections">Connections to present on the grid</param>
        private void LoadExistingRecords(List<Connection> connections)
        {
            ClearGrid();

            if (null != connections)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    Connection connection = connections[i];
                    int rowIdx = grdConnections.Rows.Add(connection.ARange, connection.BRange);
                    grdConnections.Rows[rowIdx].ReadOnly = true; // Allow this to be deleted, but not edited.

                    Range a = connection.ARange;
                    Range b = connection.BRange;
                    int numUnits = a.High - a.Low + 1;
                    for (int offset = 0; offset < numUnits; offset++)
                    {
                        _original[a.Low + offset] = b.Low + offset;
                    }
                }
            }

            btnSave.Enabled = false;
            btnSave.Tag = false; // No edits have been made
        }

        /// <summary>
        /// Saves any changes to the grid 
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                FeatureWrapper from = cboFrom.SelectedItem as FeatureWrapper;
                FeatureWrapper to = cboTo.SelectedItem as FeatureWrapper;

                if (from != null && to != null)
                {
                    SaveChanges(from, to);
                    UpdateAvailableRanges();
                }
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "General error.", ex.ToString());
                MessageBox.Show("Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// Unhook listeners
        /// </summary>
        private void ConnectionEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _hookHelper.ActiveViewSelectionChanged -= new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(helper_ActiveViewSelectionChanged);
        }

        /// <summary>
        /// A change has been made; enable the option to save them
        /// </summary>
        private void grdConnections_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            btnSave.Enabled = true;
            btnSave.Tag = true; // Edits have been made
        }

        /// <summary>
        /// The flash link has been clicked; flash the feature
        /// </summary>
        private void lblFlashFrom_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboFrom.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _hookHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// The flash link has been clicked; flash the feature
        /// </summary>
        private void lblFlashTo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboTo.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _hookHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// The form is closing; check for unsaved edits to the grid
        /// </summary>
        private void ConnectionEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnSave.Enabled)
            {
                FeatureWrapper from = cboFrom.SelectedItem as FeatureWrapper;
                FeatureWrapper to = cboTo.SelectedItem as FeatureWrapper;

                e.Cancel = !IsUserSure(from, to);
            }
        }

        /// <summary>
        /// A "from" item has been selected; load "to" choices
        /// </summary>
        private void cboFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isReverting)
            {
                bool isChangeAccepted = !btnSave.Enabled; // If the save button is disabled, there is no reason to cancel the change

                if (!isChangeAccepted)
                {
                    // Get the current to feature and the from feature that was selected before the index changed, so we can
                    // prompt for unsaved edits
                    FeatureWrapper to = cboTo.SelectedItem as FeatureWrapper;
                    FeatureWrapper preChangeFrom = null;
                    if (-1 < _lastSelectedFromIdx
                        && _lastSelectedFromIdx < cboFrom.Items.Count)
                    {
                        preChangeFrom = cboFrom.Items[_lastSelectedFromIdx] as FeatureWrapper;
                    }

                    isChangeAccepted = IsUserSure(preChangeFrom, to);
                }

                if (isChangeAccepted)
                {
                    // The current index is committed
                    _lastSelectedFromIdx = cboFrom.SelectedIndex;

                    // Unload anything that is dependent on the selection of the From Feature
                    ClearGrid();
                    cboTo.Items.Clear();
                    lblAvailableFrom.Text = "";
                    lblAvailableTo.Text = "";

                    if (null != cboFrom.SelectedItem)
                    {
                        FiberCableWrapper cableWrapper = cboFrom.SelectedItem as FiberCableWrapper;
                        DeviceWrapper deviceWrapper = cboFrom.SelectedItem as DeviceWrapper;

                        if (null != cableWrapper)
                        {
                            List<ConnectableDeviceWrapper> devices = _connectionHelper.GetCoincidentDevices(cableWrapper);

                            for (int i = 0; i < devices.Count; i++)
                            {
                                cboTo.Items.Add(devices[i]);
                            }
                        }
                        else if (null != deviceWrapper)
                        {
                            List<ConnectableCableWrapper> cables = _connectionHelper.GetCoincidentCables(deviceWrapper);

                            for (int i = 0; i < cables.Count; i++)
                            {
                                cboTo.Items.Add(cables[i]);
                            }
                        }

                        // Preselect the first item
                        if (0 < cboTo.Items.Count)
                        {
                            cboTo.SelectedItem = cboTo.Items[0];
                        }
                    }
                }
                else
                {
                    // Cancel the change by re-selecting the previous from feature
                    _isReverting = true;
                    cboFrom.SelectedIndex = _lastSelectedFromIdx;
                    _isReverting = false;
                }
            }
        }

        /// <summary>
        /// A "to" item has been selected; load the grid
        /// </summary>
        private void cboTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isReverting)
            {
                bool isChangeAccepted = !btnSave.Enabled; // If the save button is disabled, there is no reason to cancel the change

                if (!isChangeAccepted)
                {
                    // Get the current from feature and the to feature that was selected before the index changed, so we can
                    // prompt for unsaved edits
                    FeatureWrapper from = cboFrom.SelectedItem as FeatureWrapper;
                    FeatureWrapper preChangeTo = null;
                    if (-1 < _lastSelectedToIdx
                        && _lastSelectedToIdx < cboTo.Items.Count)
                    {
                        preChangeTo = cboTo.Items[_lastSelectedToIdx] as FeatureWrapper;
                    }

                    isChangeAccepted = IsUserSure(from, preChangeTo);
                }

                if (isChangeAccepted)
                {
                    // The change is committed
                    _lastSelectedToIdx = cboTo.SelectedIndex;

                    if (null != cboFrom.SelectedItem
                        && null != cboTo.SelectedItem)
                    {
                        UpdateAvailableRanges();

                        // Get the ranges and connections based on whether we are port-strand or strand-port
                        List<Connection> connections = getConnections();

                        LoadExistingRecords(connections);
                    }
                }
                else
                {
                    // Reselect the previous choice
                    _isReverting = true;
                    cboTo.SelectedIndex = _lastSelectedToIdx;
                    _isReverting = false;
                }
            }
        }

        /// <summary>
        /// Gets the connections for the current combo drop down status
        /// </summary>
        private List<Connection> getConnections()
        {
            List<Connection> connections = null;

            if (cboFrom.SelectedItem is FiberCableWrapper
                && cboTo.SelectedItem is ConnectableDeviceWrapper)
            {
                FiberCableWrapper cable = (FiberCableWrapper)cboFrom.SelectedItem;
                ConnectableDeviceWrapper device = (ConnectableDeviceWrapper)cboTo.SelectedItem;
                connections = _connectionHelper.GetConnections(cable, device, device.IsCableFromEnd, PortType.Input);
            }
            else if (cboFrom.SelectedItem is DeviceWrapper
                && cboTo.SelectedItem is ConnectableCableWrapper)
            {
                DeviceWrapper device = (DeviceWrapper)cboFrom.SelectedItem;
                ConnectableCableWrapper cable = (ConnectableCableWrapper)cboTo.SelectedItem;
                connections = _connectionHelper.GetConnections(cable, device, cable.IsThisFromEnd, PortType.Output);
            }

            return connections;
        }

        /// <summary>
        /// The user has deleted a row
        /// </summary>
        private void grdConnections_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (e.Row.ReadOnly)
            {
                // It was one of the originals
                int deletedRowIdx = grdConnections.CurrentRow.Index;

                List<Range> aRanges = SpliceAndConnectionUtils.ParseRanges(grdConnections[colFromRange.Index, deletedRowIdx].Value.ToString());
                List<Range> bRanges = SpliceAndConnectionUtils.ParseRanges(grdConnections[colToRange.Index, deletedRowIdx].Value.ToString());

                List<Connection> connections = SpliceAndConnectionUtils.MatchUp(aRanges, bRanges);

                foreach (Connection connection in connections)
                {
                    Range aRange = connection.ARange;
                    Range bRange = connection.BRange;

                    int numUnits = aRange.High - aRange.Low + 1;
                    for (int offset = 0; offset < numUnits; offset++)
                    {
                        _deleted[aRange.Low + offset] = bRange.Low + offset;
                    }

                    if (lblAvailableFrom.Text.StartsWith("N"))
                    {
                        lblAvailableFrom.Text = string.Format("Available: {0}", aRange);
                    }
                    else
                    {
                        lblAvailableFrom.Text += string.Format(",{0}", aRange.ToString());
                    }

                    if (lblAvailableTo.Text.StartsWith("N"))
                    {
                        lblAvailableTo.Text = string.Format("Available: {0}", bRange);
                    }
                    else
                    {
                        lblAvailableTo.Text += string.Format(",{0}", bRange.ToString());
                    }
                }
            }

            btnSave.Enabled = true;
            btnSave.Tag = true; // Edits have been made
        }

        /// <summary>
        /// Prompts the user about saving pending edits
        /// </summary>
        /// <param name="fromFtWrapper"></param>
        /// <param name="toFtWrapper"></param>
        /// <returns>False if the user chooses to cancel what they were doing; True if they choose Yes or No 
        /// (which means they are OK with what they are doing)</returns>
        private bool IsUserSure(FeatureWrapper fromFtWrapper, FeatureWrapper toFtWrapper)
        {
            bool result = true;

            // Assume they do not want to save and do want to continue
            DialogResult dialogResult = DialogResult.No;

            dialogResult = MessageBox.Show("You have unsaved edits. Would you like to save them before closing?", "Connection Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (DialogResult.Cancel == dialogResult)
            {
                // The user isn't sure about what they are doing and wants to cancel it
                result = false;
            }
            else if (DialogResult.Yes == dialogResult)
            {
                // The user wants to save -- give it a shot
                bool isSaveOk = SaveChanges(fromFtWrapper, toFtWrapper);
                if (!isSaveOk)
                {
                    // They wanted to save but it didn't work. They probably got a message telling them what to fix. Cancel
                    // whatever they were doing so that they have a chance to do so
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Check changes to the grid and save them to the database
        /// </summary>
        /// <param name="from">From feature</param>
        /// <param name="to">To feature</param>
        /// <returns>Success</returns>
        private bool SaveChanges(FeatureWrapper from, FeatureWrapper to)
        {
            bool result = false;
            string isNotOkString = string.Empty;

            Dictionary<int, int> currentGrid = new Dictionary<int, int>();
            FiberCableWrapper cable = null;
            DeviceWrapper device = null;
            bool isFromEnd = false;
            PortType portType = PortType.Input;

            #region Detect Direction
            try
            {
                if (from is FiberCableWrapper && to is ConnectableDeviceWrapper)
                {
                    cable = cboFrom.SelectedItem as FiberCableWrapper;
                    device = cboTo.SelectedItem as DeviceWrapper;
                    isFromEnd = ((ConnectableDeviceWrapper)device).IsCableFromEnd;
                    portType = PortType.Input;
                }
                else if (from is DeviceWrapper && to is ConnectableCableWrapper)
                {
                    device = cboFrom.SelectedItem as DeviceWrapper;
                    cable = cboTo.SelectedItem as FiberCableWrapper;
                    isFromEnd = ((ConnectableCableWrapper)cable).IsThisFromEnd;
                    portType = PortType.Output;
                }
                else
                {
                    isNotOkString = "Must connect a cable to a device, or device to a cable.";
                }
            }
            catch (Exception ex)
            {
                isNotOkString = ex.Message;
            }
            #endregion

            try
            {
                if (null != cable && null != device)
                {
                    // Only continue if we have a valid setup
                    try
                    {
                        int aIdx = colFromRange.Index;
                        int bIdx = colToRange.Index;

                        // Less than count-1 lets us avoid the insert row
                        for (int i = 0; i < grdConnections.Rows.Count - 1; i++)
                        {
                            object aRanges = (grdConnections[aIdx, i].Value != null ? grdConnections[aIdx, i].Value : "");
                            object bRanges = (grdConnections[bIdx, i].Value != null ? grdConnections[bIdx, i].Value : "");
                            List<Range> fromRanges = SpliceAndConnectionUtils.ParseRanges(aRanges.ToString());
                            List<Range> toRanges = SpliceAndConnectionUtils.ParseRanges(bRanges.ToString());

                            // Check that counts match up
                            if (!SpliceAndConnectionUtils.AreCountsEqual(fromRanges, toRanges))
                            {
                                isNotOkString = "Number of units from A to B must match on each row.";
                            }

                            // Check the ranges are within the feature's units
                            if (PortType.Input == portType)
                            {
                                if (!SpliceAndConnectionUtils.AreRangesWithinFiberCount(fromRanges, cable))
                                {
                                    isNotOkString = "Selected units exceed fiber count for cable.";
                                }
                                else if (!SpliceAndConnectionUtils.AreRangesWithinPortCount(toRanges, device, portType))
                                {
                                    isNotOkString = "Selected units exceed input port count for device.";
                                }
                            }
                            else
                            {
                                if (!SpliceAndConnectionUtils.AreRangesWithinFiberCount(toRanges, cable))
                                {
                                    isNotOkString = "Selected units exceed fiber count for cable.";
                                }
                                else if (!SpliceAndConnectionUtils.AreRangesWithinPortCount(fromRanges, device, portType))
                                {
                                    isNotOkString = "Selected units exceed output port count for device.";
                                }
                            }

                            if (0 < isNotOkString.Length)
                            {
                                // No need to check the rest if this one was not OK
                                break;
                            }

                            List<Connection> matchedUp = SpliceAndConnectionUtils.MatchUp(fromRanges, toRanges);
                            foreach (Connection connection in matchedUp)
                            {
                                Range a = connection.ARange;
                                Range b = connection.BRange;
                                int numUnits = a.High - a.Low + 1;
                                for (int offset = 0; offset < numUnits; offset++)
                                {
                                    int aUnit = a.Low + offset;
                                    int bUnit = b.Low + offset;

                                    if (currentGrid.ContainsKey(aUnit)
                                        || currentGrid.ContainsValue(bUnit))
                                    {
                                        isNotOkString = string.Format("Duplicate connection found from {0} to {1}.", aUnit, bUnit);
                                        // No need to check the rest if this one was not OK
                                        break;
                                    }
                                    else
                                    {
                                        currentGrid[aUnit] = bUnit;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isNotOkString = ex.Message;
                        MessageBox.Show(ex.Message);
                    }

                    // Check the ranges are within the feature's units
                    List<int> checkToUnits = new List<int>();
                    List<int> checkFromUnits = new List<int>();

                    // Anything that is in the current grid, we will see if it is available. But if it was deleted, we can ignore
                    // checking its availabilty, because we are about to free it up. Also if it was original, we can ignore it,
                    // since we are reprocessing it. Duplicates ON the grid have already been checked for.
                    // NOTE: We can simplify this to just check original, since any deleted ones were in the original.
                    foreach (int toUnit in currentGrid.Values)
                    {
                        if (!_original.ContainsValue(toUnit))
                        {
                            checkToUnits.Add(toUnit);
                        }
                    }

                    foreach (int fromUnit in currentGrid.Keys)
                    {
                        if (!_original.ContainsKey(fromUnit))
                        {
                            checkFromUnits.Add(fromUnit);
                        }
                    }

                    if (PortType.Input == portType)
                    {
                        if (!SpliceAndConnectionUtils.AreRangesAvailable(checkToUnits, device, portType))
                        {
                            isNotOkString = "Some To units are not in the available ranges for the device.";
                        }
                        else if (!SpliceAndConnectionUtils.AreRangesAvailable(checkFromUnits, cable, isFromEnd))
                        {
                            isNotOkString = "Some From units are not in the available ranges for the cable.";
                        }
                    }
                    else
                    {
                        if (!SpliceAndConnectionUtils.AreRangesAvailable(checkFromUnits, device, portType))
                        {
                            isNotOkString = "Some From units are not in the available ranges for the device.";
                        }
                        else if (!SpliceAndConnectionUtils.AreRangesAvailable(checkToUnits, cable, isFromEnd))
                        {
                            isNotOkString = "Some To units are not in the available ranges for the cable.";
                        }
                    }

                    if (0 == isNotOkString.Length)
                    {
                        // For the deleted ones, if they were added back, don't delete them...
                        List<int> keys = new List<int>();
                        keys.AddRange(_deleted.Keys);
                        foreach (int key in keys)
                        {
                            if (currentGrid.ContainsKey(key)
                                && currentGrid[key] == _deleted[key])
                            {
                                // It is added back, so don't delete it
                                _deleted.Remove(key);
                            }
                        }

                        _connectionHelper.BreakConnections(cable, device, _deleted, portType, false);

                        // For the added ones, if they already exist or are not available, don't add them
                        // Since we already know they are in the fiber count range, the only problem would be if they were already
                        // spliced. This would be the case if (1) it was part of the original, (2) has already appeared higher
                        // on the currentGrid, (3) is spliced to something else. (2) is handled when building currentGrid, by checking 
                        // if the aUnit or bUnit was already used and (3) is checked in the AreRangesAvailable checks. So now we will
                        // confirm (1)...
                        keys.Clear();
                        keys.AddRange(currentGrid.Keys);
                        foreach (int key in keys)
                        {
                            if (_original.ContainsKey(key)
                                && _original[key] == currentGrid[key])
                            {
                                currentGrid.Remove(key);
                            }
                        }

                        _connectionHelper.MakeConnections(cable, device, currentGrid, isFromEnd, portType, false);

                        // These are no longer part of the originals
                        foreach (int deletedKey in _deleted.Keys)
                        {
                            _original.Remove(deletedKey);
                        }

                        // These are now part of the originals
                        foreach (KeyValuePair<int, int> addedPair in currentGrid)
                        {
                            _original[addedPair.Key] = addedPair.Value;
                        }

                        _deleted.Clear(); // The grid is fresh

                        // Set the existing rows as committed data. Less than count-1 lets us avoid the insert row
                        for (int i = 0; i < grdConnections.Rows.Count - 1; i++)
                        {
                            grdConnections.Rows[i].ReadOnly = true;
                        }

                        btnSave.Enabled = false;
                        btnSave.Tag = false; // No edits have been made
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.ToString());
            }

            if (0 < isNotOkString.Length)
            {
                string message = string.Format("{0}\nPlease correct this and try again.", isNotOkString);
                MessageBox.Show(message, "Connection Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
        }

        /// <summary>
        /// Resets the available range labels
        /// </summary>
        private void UpdateAvailableRanges()
        {
            List<Range> availableFrom = null;
            List<Range> availableTo = null;

            // Get the ranges and connections based on whether we are port-strand or strand-port
            #region Determine Direction
            if (cboFrom.SelectedItem is FiberCableWrapper
                && cboTo.SelectedItem is ConnectableDeviceWrapper)
            {
                FiberCableWrapper cable = (FiberCableWrapper)cboFrom.SelectedItem;
                ConnectableDeviceWrapper device = (ConnectableDeviceWrapper)cboTo.SelectedItem;

                availableFrom = SpliceAndConnectionUtils.GetAvailableRanges(cable, device.IsCableFromEnd);
                availableTo = SpliceAndConnectionUtils.GetAvailableRanges(device, PortType.Input);
            }
            else if (cboFrom.SelectedItem is DeviceWrapper
                && cboTo.SelectedItem is ConnectableCableWrapper)
            {
                DeviceWrapper device = (DeviceWrapper)cboFrom.SelectedItem;
                ConnectableCableWrapper cable = (ConnectableCableWrapper)cboTo.SelectedItem;

                availableFrom = SpliceAndConnectionUtils.GetAvailableRanges(device, PortType.Output);
                availableTo = SpliceAndConnectionUtils.GetAvailableRanges(cable, cable.IsThisFromEnd);
            }
            #endregion

            lblAvailableFrom.Text = GetAvailableRangesString(availableFrom);
            lblAvailableTo.Text = GetAvailableRangesString(availableTo);
        }



    }
}
