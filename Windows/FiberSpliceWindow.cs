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
using ESRI.ArcGIS.Carto;
using System.Runtime.InteropServices;


namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class FiberSpliceWindow : UserControl
    {
        private FiberSpliceHelper _spliceHelper = null;
        private HookHelperExt _hookHelper = null;

        private LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        private bool _isEditing = false;

        // preserve a record of unit to unit connections that were loaded into a grid for the given features
        private Dictionary<int, FiberSplice> _original = new Dictionary<int, FiberSplice>();
        // preserve a record of unit to unit connections that were deleted from the grid
        private Dictionary<int, FiberSplice> _deleted = new Dictionary<int, FiberSplice>();

        // These are used for the unsaved edits prompt, to let us revert to previous selections since the combobox doesn't
        // have a cancelable Changing event.
        private bool _isReverting = false;
        private int _lastSelectedSpliceIdx = -1;
        private int _lastSelectedAIdx = -1;
        private int _lastSelectedBIdx = -1;


        public FiberSpliceWindow(object hook)
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
            private FiberSpliceWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal FiberSpliceWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new FiberSpliceWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }
        }

        #region ISpliceEditorForm Implementation

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
        /// To prepare and display the form to the user
        /// </summary>
        /// <param name="spliceHelper">Class providing helper methods for splicing</param>
        public void DisplaySplices(FiberSpliceHelper spliceHelper)
        {
            _spliceHelper = spliceHelper;

            // Get splice type domain information 
            LoadTypeDropdown(_spliceHelper);

            // Changes the GUI appropriately
            SetEditState(_isEditing);

            // Load the dropdowns with the selected splice closure info.
            PopulateSpliceClosures(_spliceHelper);
        }

        #endregion

        /// <summary>
        /// Reset form controls to allow or disallow editing
        /// </summary>
        /// <param name="isEditing"></param>
        private void SetEditState(bool isEditing)
        {
            if (isEditing)
            {
                this.Text = "Splice Editor";

                grdSplices.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

                // We store true/false in the tag as to 
                // whether edits were made to the current grid rows. 
                if (null != btnSave.Tag)
                {
                    btnSave.Enabled = (bool)btnSave.Tag;
                }
            }
            else
            {
                this.Text = "Splice Viewer";

                grdSplices.EditMode = DataGridViewEditMode.EditProgrammatically;

                // Cannot save when not editing
                btnSave.Enabled = false;
            }

            grdSplices.AllowUserToAddRows = isEditing;
            grdSplices.AllowUserToDeleteRows = isEditing;
        }

        /// <summary>
        /// Map selection changed, update the choices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void helper_ActiveViewSelectionChanged()
        {
            // Window not fully initialized or visible.
            if (!this.Visible || _spliceHelper == null) return;

            try
            {
                PopulateSpliceClosures(_spliceHelper);
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Error gathering connection information.", e.ToString());
                MessageBox.Show("Error gathering connection information: \n" + e.ToString());
            }
        }

        /// <summary>
        /// Load the drop down of selected splice closures
        /// </summary>
        /// <param name="helper">SpliceEditorHelper</param>
        private void PopulateSpliceClosures(FiberSpliceHelper helper)
        {
            try
            {
                // Clear anything that is dependent on what we are about to load
                ClearGrid();
                cboCableA.Items.Clear();
                cboCableB.Items.Clear();
                cboSpliceClosure.Items.Clear();
                lblAvailableA.Text = "";
                lblAvailableB.Text = "";

                // Find the layer
                ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ConfigUtil.SpliceClosureFtClassName);
                if (ftLayer == null)
                {
                    ArcMap.Application.StatusBar.set_Message(0, "Telecom Tools error occurred. Check log for details.");
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Could not find Feature Layer:.", ConfigUtil.SpliceClosureFtClassName);
                    return;
                }
                int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

                // Get the selection on this layer
                List<ESRI.ArcGIS.Geodatabase.IFeature> selectedSplices = _hookHelper.GetSelectedFeatures(ftLayer);

                for (int i = 0; i < selectedSplices.Count; i++)
                {
                    SpliceClosureWrapper w = new SpliceClosureWrapper(selectedSplices[i], displayIdx);
                    cboSpliceClosure.Items.Add(w);
                }

                if (0 < cboSpliceClosure.Items.Count)
                {
                    cboSpliceClosure.SelectedItem = cboSpliceClosure.Items[0];
                }
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Splice Connection Window (PopulateSpliceClosures): ", e.Message);
            }
        }

        /// <summary>
        /// Load the A dropdown with selected cables
        /// </summary>
        /// <param name="helper">SpliceEditorHelper</param>
        private void PopulateACables(FiberSpliceHelper helper, SpliceClosureWrapper spliceWrapper)
        {
            try
            {
                // Clear anything that is dependent on what we are about to load
                ClearGrid();
                cboCableA.Items.Clear();
                cboCableB.Items.Clear();
                lblAvailableA.Text = "";
                lblAvailableB.Text = "";

                List<ESRI.ArcGIS.Geodatabase.IFeature> selectedCables = helper.GetConnectedCables(spliceWrapper);
                ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = _hookHelper.FindFeatureLayer(ConfigUtil.FiberCableFtClassName);
                int displayIdx = ftLayer.FeatureClass.FindField(ftLayer.DisplayField);

                for (int i = 0; i < selectedCables.Count; i++)
                {
                    FiberCableWrapper w = new FiberCableWrapper(selectedCables[i], displayIdx);
                    cboCableA.Items.Add(w);
                }

                if (0 < cboCableA.Items.Count)
                {
                    cboCableA.SelectedItem = cboCableA.Items[0];
                }
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Splice Connection Window (PopulateACables): ", e.Message);
            }
        }

        /// <summary>
        /// Load the B dropdown with spliceable cables
        /// </summary>
        /// <param name="helper">SpliceEditorHelper</param>
        /// <param name="cableA">A Cable</param>
        private void PopulateBCables(FiberSpliceHelper helper, FiberCableWrapper cableA)
        {
            try
            {
                // Clear anything that is dependent on what we are about to load
                ClearGrid();
                cboCableB.Items.Clear();
                lblAvailableA.Text = "";
                lblAvailableB.Text = "";

                SpliceClosureWrapper splice = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;
                if (null != splice)
                {
                    List<SpliceableCableWrapper> spliceableCables = _spliceHelper.GetSpliceableCables(cableA, splice);
                    for (int i = 0; i < spliceableCables.Count; i++)
                    {
                        cboCableB.Items.Add(spliceableCables[i]);
                    }

                    if (0 < cboCableB.Items.Count)
                    {
                        cboCableB.SelectedItem = cboCableB.Items[0];
                    }
                }
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Splice Connection Window (PopulateBCables): ", e.Message);
            }

        }

        /// <summary>
        /// Creates a label string for the available ranges of a cable at a certain end
        /// </summary>
        /// <param name="cable">Cable to check</param>
        /// <param name="isFromEnd">End to check</param>
        /// <returns>string</returns>
        private string GetAvailableRangeString(FiberCableWrapper cable, bool isFromEnd)
        {
            List<Range> availableRanges = SpliceAndConnectionUtils.GetAvailableRanges(cable, isFromEnd);
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
        /// Loads the grid with existing splice ranges between the given features
        /// </summary>
        /// <param name="spliceWrapper">Selected Splice Closure</param>
        /// <param name="cableA">Selected Cable A</param>
        /// <param name="cableB">Selected Cable B</param>
        private void LoadExistingRecords(SpliceClosureWrapper spliceWrapper, FiberCableWrapper cableA, SpliceableCableWrapper cableB)
        {
            ClearGrid();

            if (null != spliceWrapper && null != cableA && null != cableB)
            {
                List<FiberSplice> rangesAtoB = SpliceAndConnectionUtils.GetSplicedRanges(cableA, cableB, spliceWrapper);
                for (int i = 0; i < rangesAtoB.Count; i++)
                {
                    FiberSplice fiberSplice = rangesAtoB[i];
                    int rowIdx = grdSplices.Rows.Add(fiberSplice.ARange, fiberSplice.BRange, fiberSplice.Loss, fiberSplice.Type);
                    grdSplices.Rows[rowIdx].ReadOnly = true; // Allow this to be deleted, but not edited.

                    Range a = fiberSplice.ARange;
                    Range b = fiberSplice.BRange;
                    int numUnits = a.High - a.Low + 1;
                    for (int offset = 0; offset < numUnits; offset++)
                    {
                        int aUnit = a.Low + offset;
                        int bUnit = b.Low + offset;

                        FiberSplice originalSplice = new FiberSplice(new Range(aUnit, aUnit), new Range(bUnit, bUnit), fiberSplice.Loss, fiberSplice.Type);
                        _original[aUnit] = originalSplice;
                    }
                }

                // These are valid for display because it is these two cables at this splice, the A and B side assignment is
                // arbitrary. We do need to reverse them in the display though. For example is Cable X (1-12) is spliced to
                // Cable Y (36-47), and they have selected X/Y in the form -- the grid should show X's units on the left and
                // Y's on the right. Here we are requesting them as Y/X though.
                List<FiberSplice> rangesBtoA = SpliceAndConnectionUtils.GetSplicedRanges((FiberCableWrapper)cboCableB.SelectedItem, (FiberCableWrapper)cboCableA.SelectedItem, spliceWrapper);
                for (int i = 0; i < rangesBtoA.Count; i++)
                {
                    FiberSplice fiberSplice = rangesBtoA[i];
                    int rowIdx = grdSplices.Rows.Add(fiberSplice.BRange, fiberSplice.ARange, fiberSplice.Loss, fiberSplice.Type);
                    grdSplices.Rows[rowIdx].ReadOnly = true; // Allow this to be deleted, but not edited.

                    Range a = fiberSplice.ARange;
                    Range b = fiberSplice.BRange;
                    int numUnits = a.High - a.Low + 1;
                    for (int offset = 0; offset < numUnits; offset++)
                    {
                        int aUnit = a.Low + offset;
                        int bUnit = b.Low + offset;

                        FiberSplice originalSplice = new FiberSplice(new Range(bUnit, bUnit), new Range(aUnit, aUnit), fiberSplice.Loss, fiberSplice.Type);
                        _original[bUnit] = originalSplice;
                    }
                }
            }

            btnSave.Enabled = false;
            btnSave.Tag = false; // No edits made yet
        }

        /// <summary>
        /// Resets the grid and caches of what was on it
        /// </summary>
        private void ClearGrid()
        {
            grdSplices.Rows.Clear();

            _original.Clear();
            _deleted.Clear();

            btnSave.Enabled = false;
            btnSave.Tag = false; // No edits made yet
        }

        /// <summary>
        /// The user has clicked the save button
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SpliceClosureWrapper spliceWrapper = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;
                FiberCableWrapper cableA = cboCableA.SelectedItem as FiberCableWrapper;
                SpliceableCableWrapper cableB = cboCableB.SelectedItem as SpliceableCableWrapper;

                SaveChanges(spliceWrapper, cableA, cableB);

                lblAvailableA.Text = GetAvailableRangeString(cableA, cableB.IsOtherFromEnd);
                lblAvailableB.Text = GetAvailableRangeString(cableB, cableB.IsThisFromEnd);
            }
            catch (Exception ex)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Error saving connections.", ex.ToString());

                MessageBox.Show("Error saving connections: \n" + ex.ToString());
            }
        }

        /// <summary>
        /// Unhook listeners
        /// </summary>
        private void SpliceEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _hookHelper.ActiveViewSelectionChanged -= new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(helper_ActiveViewSelectionChanged);
        }

        /// <summary>
        /// The user made a change
        /// </summary>
        private void grdSplices_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            btnSave.Enabled = true;
            btnSave.Tag = true; // Edits made
        }

        /// <summary>
        /// The flash link was clicked
        /// </summary>
        private void lblFlashA_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboCableA.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _hookHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// The flash link was clicked
        /// </summary>
        private void lblFlashB_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboCableB.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _hookHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// The flash link was clicked
        /// </summary>
        private void lblFlashSplice_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FeatureWrapper wrapper = cboSpliceClosure.SelectedItem as FeatureWrapper;
            if (null != wrapper)
            {
                _hookHelper.FlashFeature(wrapper.Feature);
            }
        }

        /// <summary>
        /// The form is closing; check for unsaved edits to the grid
        /// </summary>
        private void SpliceEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnSave.Enabled)
            {
                SpliceClosureWrapper spliceWrapper = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;
                FiberCableWrapper cableA = cboCableA.SelectedItem as FiberCableWrapper;
                SpliceableCableWrapper cableB = cboCableB.SelectedItem as SpliceableCableWrapper;

                e.Cancel = !IsUserSure(spliceWrapper, cableA, cableB);
            }
        }

        /// <summary>
        /// The user changed a different splice closure
        /// </summary>
        private void cboSpliceClosure_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isReverting)
            {
                bool isChangeAccepted = !btnSave.Enabled; // If the save button is disabled, there aren't edits to prompt about

                if (!isChangeAccepted)
                {
                    // Get the current cables and the splice that was selected before the index changed, so we can
                    // prompt for unsaved edits
                    FiberCableWrapper cableA = cboCableA.SelectedItem as FiberCableWrapper;
                    SpliceableCableWrapper cableB = cboCableB.SelectedItem as SpliceableCableWrapper;

                    SpliceClosureWrapper preChangeSplice = null;
                    if (-1 < _lastSelectedSpliceIdx
                        && _lastSelectedSpliceIdx < cboSpliceClosure.Items.Count)
                    {
                        preChangeSplice = cboSpliceClosure.Items[_lastSelectedSpliceIdx] as SpliceClosureWrapper;
                    }

                    isChangeAccepted = IsUserSure(preChangeSplice, cableA, cableB);
                }

                if (isChangeAccepted)
                {
                    _lastSelectedSpliceIdx = cboSpliceClosure.SelectedIndex;

                    SpliceClosureWrapper spliceWrapper = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;
                    if (null != spliceWrapper)
                    {
                        PopulateACables(_spliceHelper, spliceWrapper);
                    }
                }
                else
                {
                    // Cancel the change by re-selecting the previous from feature
                    _isReverting = true;
                    cboSpliceClosure.SelectedIndex = _lastSelectedSpliceIdx;
                    _isReverting = false;
                }
            }
        }

        /// <summary>
        /// The user changed a different cable for the A cable
        /// </summary>
        private void cboCableA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isReverting)
            {
                bool isChangeAccepted = !btnSave.Enabled; // If the save button is disabled, there aren't edits to prompt about

                if (!isChangeAccepted)
                {
                    // Get the current B cable and splice, and the A Cable that was selected before the index changed, 
                    // so we can prompt for unsaved edits
                    SpliceClosureWrapper splice = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;
                    SpliceableCableWrapper cableB = cboCableB.SelectedItem as SpliceableCableWrapper;

                    FiberCableWrapper preChangeA = null;
                    if (-1 < _lastSelectedAIdx
                        && _lastSelectedAIdx < cboCableA.Items.Count)
                    {
                        preChangeA = cboCableA.Items[_lastSelectedAIdx] as FiberCableWrapper;
                    }

                    isChangeAccepted = IsUserSure(splice, preChangeA, cableB);
                }

                if (isChangeAccepted)
                {
                    _lastSelectedAIdx = cboCableA.SelectedIndex;

                    FiberCableWrapper cableA = cboCableA.SelectedItem as FiberCableWrapper;
                    if (null != cableA)
                    {
                        PopulateBCables(_spliceHelper, cableA);
                    }
                }
                else
                {
                    // Cancel the change by re-selecting the previous from feature
                    _isReverting = true;
                    cboCableA.SelectedIndex = _lastSelectedAIdx;
                    _isReverting = false;
                }
            }
        }

        /// <summary>
        /// The user changed a different cable for the B cable
        /// </summary>
        private void cboCableB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isReverting)
            {
                bool isChangeAccepted = !btnSave.Enabled; // If the save button is disabled, there aren't edits to prompt about

                // Get the current A cable and splice
                FiberCableWrapper cableA = cboCableA.SelectedItem as FiberCableWrapper;
                SpliceClosureWrapper splice = cboSpliceClosure.SelectedItem as SpliceClosureWrapper;

                if (!isChangeAccepted)
                {
                    // Get the B Cable that was selected before the index changed, so we can prompt for unsaved edits
                    SpliceableCableWrapper preChangeB = null;
                    if (-1 < _lastSelectedBIdx
                        && _lastSelectedBIdx < cboCableB.Items.Count)
                    {
                        preChangeB = cboCableB.Items[_lastSelectedBIdx] as SpliceableCableWrapper;
                    }

                    isChangeAccepted = IsUserSure(splice, cableA, preChangeB);
                }

                if (isChangeAccepted)
                {
                    _lastSelectedBIdx = cboCableB.SelectedIndex;
                    SpliceableCableWrapper cableB = cboCableB.SelectedItem as SpliceableCableWrapper;

                    if (null != cableA
                        && null != cableB
                        && null != splice)
                    {
                        lblAvailableA.Text = GetAvailableRangeString(cableA, cableB.IsOtherFromEnd);
                        lblAvailableB.Text = GetAvailableRangeString(cableB, cableB.IsThisFromEnd);

                        LoadExistingRecords(splice, cableA, cableB);
                    }
                }
                else
                {
                    // Cancel the change by re-selecting the previous from feature
                    _isReverting = true;
                    cboCableB.SelectedIndex = _lastSelectedBIdx;
                    _isReverting = false;
                }
            }
        }

        /// <summary>
        /// The user has deleted a row
        /// </summary>
        private void grdSplices_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (e.Row.ReadOnly)
            {
                // It was one of the originals
                int deletedRowIdx = e.Row.Index;

                double? loss = null;
                if (null != grdSplices[colLoss.Index, deletedRowIdx].Value)
                {
                    // We know that since the row is original and non-null, the value must have come straight out of the
                    // database and be parsable
                    loss = double.Parse(grdSplices[colLoss.Index, deletedRowIdx].Value.ToString());
                }

                object type = grdSplices[colLoss.Index + 1, deletedRowIdx].Value;

                List<Range> aRanges = SpliceAndConnectionUtils.ParseRanges(grdSplices[colRangeA.Index, deletedRowIdx].Value.ToString());
                List<Range> bRanges = SpliceAndConnectionUtils.ParseRanges(grdSplices[colRangeB.Index, deletedRowIdx].Value.ToString());

                List<Connection> connections = SpliceAndConnectionUtils.MatchUp(aRanges, bRanges);
                foreach (Connection connection in connections)
                {
                    Range aRange = connection.ARange;
                    Range bRange = connection.BRange;

                    int numUnits = aRange.High - aRange.Low + 1;
                    for (int offset = 0; offset < numUnits; offset++)
                    {
                        int aUnit = aRange.Low + offset;
                        int bUnit = bRange.Low + offset;

                        FiberSplice deletedSplice = new FiberSplice(new Range(aUnit, aUnit), new Range(bUnit, bUnit), loss, type);
                        _deleted[aUnit] = deletedSplice;
                    }

                    if (lblAvailableA.Text.StartsWith("N"))
                    {
                        lblAvailableA.Text = string.Format("Available: {0}", aRange);
                    }
                    else
                    {
                        lblAvailableA.Text += string.Format(",{0}", aRange.ToString());
                    }

                    if (lblAvailableB.Text.StartsWith("N"))
                    {
                        lblAvailableB.Text = string.Format("Available: {0}", bRange);
                    }
                    else
                    {
                        lblAvailableB.Text += string.Format(",{0}", bRange.ToString());
                    }
                }
            }

            btnSave.Enabled = true;
            btnSave.Tag = true; // Edits made
        }

        /// <summary>
        /// Prompts the user about saving pending edits
        /// </summary>
        /// <param name="spliceWrapper"></param>
        /// <param name="cableAWrapper"></param>
        /// <param name="cableBWrapper"></param>
        /// <returns>False if the user chooses to cancel what they were doing; True if they choose Yes or No 
        /// (which means they are OK with what they are doing)</returns>
        private bool IsUserSure(SpliceClosureWrapper spliceWrapper, FiberCableWrapper cableAWrapper, SpliceableCableWrapper cableBWrapper)
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
                bool isSaveOk = SaveChanges(spliceWrapper, cableAWrapper, cableBWrapper);
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
        /// <param name="splice">The associated splice closure</param>
        /// <param name="cableA">A cable</param>
        /// <param name="cableB">The other cable</param>
        /// <returns>Success</returns>
        private bool SaveChanges(SpliceClosureWrapper splice, FiberCableWrapper cableA, SpliceableCableWrapper cableB)
        {
            bool result = false;
            string isNotOkString = string.Empty;

            Dictionary<int, FiberSplice> currentGrid = new Dictionary<int, FiberSplice>();
            List<int> currentBStrands = new List<int>();

            try
            {
                int aIdx = colRangeA.Index;
                int bIdx = colRangeB.Index;
                int lossIdx = colLoss.Index;
                int typeIdx = grdSplices.Columns[colType.Name].Index; // If we had to use colTypeText, it will be using the same name

                // Less than count-1 lets us avoid the insert row
                for (int i = 0; i < grdSplices.Rows.Count - 1; i++)
                {
                    if (grdSplices[aIdx, i].Value == null || grdSplices[bIdx, i].Value == null)
                    {
                        isNotOkString = "A or B unit range missing.";
                    }
                    if (0 < isNotOkString.Length)
                    {
                        // No need to check the rest if this one was not OK
                        break;
                    }

                    List<Range> aRanges = SpliceAndConnectionUtils.ParseRanges(grdSplices[aIdx, i].Value.ToString());
                    List<Range> bRanges = SpliceAndConnectionUtils.ParseRanges(grdSplices[bIdx, i].Value.ToString());

                    if (!SpliceAndConnectionUtils.AreCountsEqual(aRanges, bRanges))
                    {
                        isNotOkString = "Number of units from A to B must match on each row.";
                    }
                    else if (!SpliceAndConnectionUtils.AreRangesWithinFiberCount(aRanges, cableA))
                    {
                        isNotOkString = "Selected units exceed fiber count for cable A.";
                    }
                    else if (!SpliceAndConnectionUtils.AreRangesWithinFiberCount(bRanges, cableB))
                    {
                        isNotOkString = "Selected units exceed fiber count for cable B.";
                    }

                    if (0 < isNotOkString.Length)
                    {
                        // No need to check the rest if this one was not OK
                        break;
                    }

                    List<Connection> matchedUp = SpliceAndConnectionUtils.MatchUp(aRanges, bRanges);
                    foreach (Connection range in matchedUp)
                    {
                        Range a = range.ARange;
                        Range b = range.BRange;
                        int numUnits = a.High - a.Low + 1;
                        for (int offset = 0; offset < numUnits; offset++)
                        {
                            int aUnit = a.Low + offset;
                            int bUnit = b.Low + offset;

                            if (currentGrid.ContainsKey(aUnit))
                            {
                                isNotOkString = string.Format("Duplicate splicing found for A Strand {0}", aUnit);
                                // No need to check the rest if this one was not OK
                                break;
                            }
                            else if (currentBStrands.Contains(bUnit))
                            {
                                isNotOkString = string.Format("Duplicate splicing found for B Strand {0}", bUnit);
                                // No need to check the rest if this one was not OK
                                break;
                            }
                            else
                            {
                                object lossObj = grdSplices[lossIdx, i].Value;
                                object typeObj = grdSplices[typeIdx, i].Value;

                                double? loss = null;
                                if (null != lossObj)
                                {
                                    double dblLoss = -1;
                                    if (double.TryParse(lossObj.ToString(), out dblLoss))
                                    {
                                        loss = dblLoss;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Loss value on row {0} could not be parsed. Using null.", "Splice Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }

                                FiberSplice fiberSplice = new FiberSplice(new Range(aUnit, aUnit), new Range(bUnit, bUnit), loss, typeObj);
                                currentGrid[aUnit] = fiberSplice;
                                currentBStrands.Add(bUnit);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isNotOkString = ex.Message;
            }


            // Check the ranges are within the feature's units
            List<int> checkToUnits = new List<int>();
            List<int> checkFromUnits = new List<int>();

            // Anything that is in the current grid, we will see if it is available. But if it was deleted, we can ignore
            // checking its availabilty, because we are about to free it up. Also if it was original, we can ignore it,
            // since we are reprocessing it. Duplicates ON the grid have already been checked for.
            // NOTE: We can simplify this to just check original, since any deleted ones were in the original.
            foreach (FiberSplice checkSplice in currentGrid.Values)
            {
                int unit = checkSplice.BRange.Low;
                checkToUnits.Add(unit);
            }

            foreach (FiberSplice checkSplice in _original.Values)
            {
                int unit = checkSplice.BRange.Low;
                if (checkToUnits.Contains(unit))
                {
                    checkToUnits.Remove(unit);
                }
            }

            foreach (int fromUnit in currentGrid.Keys)
            {
                if (!_original.ContainsKey(fromUnit))
                {
                    checkFromUnits.Add(fromUnit);
                }
            }

            if (!SpliceAndConnectionUtils.AreRangesAvailable(checkFromUnits, cableA, cableB.IsOtherFromEnd))
            {
                isNotOkString = "Some A units are not in the available ranges for the A Cable.";
            }
            else if (!SpliceAndConnectionUtils.AreRangesAvailable(checkToUnits, cableB, cableB.IsThisFromEnd))
            {
                isNotOkString = "Some B units are not in the available ranges for the B Cable.";
            }

            if (0 < isNotOkString.Length)
            {
                string message = string.Format("{0}\nPlease correct this and try again.", isNotOkString);
                MessageBox.Show(message, "Splice Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (null != cableA && null != cableB)
                {
                    // For the deleted ones, if they were added back, don't delete them...
                    List<int> keys = new List<int>();
                    keys.AddRange(_deleted.Keys);
                    foreach (int key in keys)
                    {
                        if (currentGrid.ContainsKey(key))
                        {
                            FiberSplice fiberSplice = currentGrid[key];
                            if (fiberSplice.BRange.Low == _deleted[key].BRange.Low
                                && fiberSplice.Loss == _deleted[key].Loss
                                && fiberSplice.Type == _deleted[key].Type)
                            {
                                // It is added back, so don't delete it
                                _deleted.Remove(key);
                            }
                        }
                    }

                    if (0 < _deleted.Count)
                    {
                        _spliceHelper.BreakSplices(cableA, cableB, splice, _deleted, false);
                    }

                    // For the added ones, if they already exist or are not available, don't add them
                    // Since we already know they are in the fiber count range, the only problem would be if they were already
                    // spliced. This would be the case if (1) it was part of the original or (2) has already appeared higher
                    // on the currentGrid. (2) is handled when building currentGrid, by checking if the aUnit or bUnit was already used.
                    keys.Clear();
                    keys.AddRange(currentGrid.Keys);
                    foreach (int key in keys)
                    {
                        if (_original.ContainsKey(key))
                        {
                            FiberSplice fiberSplice = currentGrid[key];
                            if (fiberSplice.BRange.Low == _original[key].BRange.Low
                                && fiberSplice.Loss == _original[key].Loss
                                && fiberSplice.Type == _original[key].Type)
                            {
                                // It was on the original, so we don't need to create it
                                currentGrid.Remove(key);
                            }
                        }
                    }

                    if (0 < currentGrid.Count)
                    {
                        _spliceHelper.CreateSplices(cableA, cableB, splice, currentGrid, false);
                    }

                    // These are no longer part of the originals
                    foreach (KeyValuePair<int, FiberSplice> deletedPair in _deleted)
                    {
                        _original.Remove(deletedPair.Key);
                    }

                    // These are now part of the originals
                    foreach (KeyValuePair<int, FiberSplice> addedPair in currentGrid)
                    {
                        _original[addedPair.Key] = addedPair.Value;
                    }

                    _deleted.Clear(); // The grid is fresh

                    // Set the existing rows as committed data. Less than count-1 lets us avoid the insert row
                    for (int i = 0; i < grdSplices.Rows.Count - 1; i++)
                    {
                        grdSplices.Rows[i].ReadOnly = true;
                    }

                    btnSave.Enabled = false;
                    btnSave.Tag = false; // No edits made yet
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks the database to see if the splice Type field has a domain; if it does load the choices, otherwise
        /// enable free text editing on the column
        /// </summary>
        /// <param name="helper">Helper class</param>
        private void LoadTypeDropdown(FiberSpliceHelper helper)
        {
            try
            {
                ESRI.ArcGIS.Geodatabase.IFeatureClass ftClass =   _wkspHelper.FindFeatureClass(ConfigUtil.FiberCableFtClassName);
//                ESRI.ArcGIS.Geodatabase.IFeatureClass ftClass = helper.FindFeatureClass(ConfigUtil.FiberCableFtClassName);
                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = _wkspHelper.FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = GdbUtils.GetTable(ftClass, ConfigUtil.FiberSpliceTableName);
                ESRI.ArcGIS.Geodatabase.IField typeField = fiberSpliceTable.Fields.get_Field(fiberSpliceTable.FindField(ConfigUtil.TypeFieldName));

                ESRI.ArcGIS.Geodatabase.ICodedValueDomain domain = typeField.Domain as ESRI.ArcGIS.Geodatabase.ICodedValueDomain;
                if (null != domain)
                {
                    colType.Items.Clear();
                    colType.Items.Add(string.Empty); // For DBNull

                    for (int codeIdx = 0; codeIdx < domain.CodeCount; codeIdx++)
                    {
                        colType.Items.Add(domain.get_Name(codeIdx));
                    }
                }
                else
                {
                    // Change to a text column
                    System.Windows.Forms.DataGridViewTextBoxColumn colTypeText = new DataGridViewTextBoxColumn();
                    colTypeText.HeaderText = colType.HeaderText;
                    colTypeText.Name = colType.Name;
                    grdSplices.Columns.Remove(colType);
                    grdSplices.Columns.Add(colTypeText);
                }
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Splice Connection Window (LoadTypeDropdown): ", e.Message);
            }
        }

    }
}
