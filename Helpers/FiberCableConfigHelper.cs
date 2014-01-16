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

using Esri_Telecom_Tools.Core;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.Editor;
using System.Windows.Forms;



namespace Esri_Telecom_Tools.Helpers
{
    /// <summary>
    /// This class does all the real work so that we can 
    /// change the UI more easily if necessary
    /// </summary>
    public class FiberCableConfigHelper : EditorBase
    {
        private LogHelper _logHelper = LogHelper.Instance();

        private HookHelperExt _hookHelper = null;
       
        // The current fiber cable configuration being used.
        private FiberCableConfiguration _fiberConfig = null;

        public FiberCableConfigHelper(HookHelperExt hookHelper, ESRI.ArcGIS.Editor.IEditor3 editor)
            : base(editor)
        {
            _hookHelper = hookHelper;
        }

        public FiberCableConfiguration FiberCableConfig
        {
            set
            {
                _fiberConfig = value;
            }
        }

        protected override void editor_OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            // Check for bad inputs
            ESRI.ArcGIS.Geodatabase.IFeature feature = obj as ESRI.ArcGIS.Geodatabase.IFeature;
            if (feature == null || feature.Class == null) return;

            // Work out type of feature
            ESRI.ArcGIS.Geodatabase.IDataset dataset = (ESRI.ArcGIS.Geodatabase.IDataset)feature.Class;
            string tableName = GdbUtils.ParseTableName(dataset);

            // -----------------------------
            // Fiber
            // -----------------------------
            if (0 == string.Compare(ConfigUtil.FiberCableFtClassName, tableName, true))
            {
                try
                {
                    //FiberCableConfiguration cf =
                    //    ConfigUtil.FiberCableConfigurationFromDisplayName(listView1.SelectedItems[0].Text);
                    if (_fiberConfig != null)
                    {
                        ConfigureCable(feature, _fiberConfig, true);
                    }
                }
                catch (Exception ex)
                {
                    _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Failed to configure cable.", ex.Message);

                    string message = "Failed to configure cable:" + System.Environment.NewLine +
                        ex.Message;
                    MessageBox.Show(message, "Configure Fiber Cable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void editor_OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
//            throw new NotImplementedException();
        }

        protected override void editor_OnDeleteFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
//            throw new NotImplementedException();
        }

        protected override void editor_OnSelectionChanged()
        {
//            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the buffer tube and strand counts based on the given configuration. If IPID and/or CABLEID are null, it also
        /// takes care of them
        /// </summary>
        /// <param name="feature">The FiberCable feature to configure</param>
        /// <param name="configuration">The tube/strand counts</param>
        /// <param name="isExistingOperation">Flag to control whether this method is being called from within an existing 
        /// edit operation</param>
        /// <returns>Success</returns>
        protected bool ConfigureCable(ESRI.ArcGIS.Geodatabase.IFeature feature, FiberCableConfiguration configuration, bool isExistingOperation)
        {
            bool isComplete = false;
            bool isOurOperationOpen = false;

            // The following assignments are defaults for the case where they are not already populated on the feature
            string fiberCableIpid = Guid.NewGuid().ToString("B").ToUpper();

            // The following will be set during Validation
            ESRI.ArcGIS.Geodatabase.IObjectClass ftClass = null;
            ESRI.ArcGIS.Geodatabase.IFields fields = null;
            int ipidIdx = -1;
            int bufferCountIdx = -1;
            int strandCountIdx = -1;

            #region Validation

            if (null == feature)
            {
                throw new ArgumentNullException("feature");
            }

            if (null == configuration)
            {
                throw new ArgumentNullException("configuration");
            }

            if (_editor.EditState == ESRI.ArcGIS.Editor.esriEditState.esriStateNotEditing)
            {
                throw new InvalidOperationException("You must be editing the workspace to perform this operation.");
            }

            ftClass = feature.Class;
            fields = ftClass.Fields;

            string missingFieldFormat = "Field {0} is missing.";

            ipidIdx = fields.FindField(ConfigUtil.IpidFieldName);
            if (-1 == ipidIdx)
            {
                throw new InvalidOperationException(string.Format(missingFieldFormat, ConfigUtil.IpidFieldName));
            }

            bufferCountIdx = fields.FindField(ConfigUtil.NumberOfBuffersFieldName);
            if (-1 == bufferCountIdx)
            {
                throw new InvalidOperationException(string.Format(missingFieldFormat, ConfigUtil.NumberOfBuffersFieldName));
            }

            strandCountIdx = fields.FindField(ConfigUtil.NumberOfFibersFieldName);
            if (-1 == strandCountIdx)
            {
                throw new InvalidOperationException(string.Format(missingFieldFormat, ConfigUtil.NumberOfFibersFieldName));
            }

            #endregion


            ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel = new ESRI.ArcGIS.Display.CancelTrackerClass();
            ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog = _hookHelper.CreateProgressDialog(trackCancel, "Preparing to configure cable...", 1, configuration.TotalFiberCount, 1, "Starting edit operation...", "Fiber Configuration");
            ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = (ESRI.ArcGIS.esriSystem.IStepProgressor)progressDialog;

            progressDialog.ShowDialog();
            stepProgressor.Step();

            if (!isExistingOperation)
            {
                _editor.StartOperation();
                isOurOperationOpen = true;
            }

            try
            {
                if (DBNull.Value == feature.get_Value(ipidIdx))
                {
                    feature.set_Value(ipidIdx, fiberCableIpid);
                }
                else
                {
                    fiberCableIpid = feature.get_Value(ipidIdx).ToString();
                }
               
                feature.set_Value(bufferCountIdx, configuration.BufferCount);
                feature.set_Value(strandCountIdx, configuration.FibersPerTube);

                isComplete = GenerateUnits(feature, configuration, progressDialog, trackCancel);

                progressDialog.Description = "Completing configuration...";
                stepProgressor.Step();

                if (isOurOperationOpen)
                {
                    if (isComplete)
                    {
                        feature.Store();
                        _editor.StopOperation("Configure Fiber");
                    }
                    else
                    {
                        _editor.AbortOperation();
                    }
                }
            }
            catch(Exception e)
            {
                if (isOurOperationOpen)
                {
                    _editor.AbortOperation();
                }
            }

            progressDialog.HideDialog();
            return isComplete;
        }

        /*
        private String fiberOrBufferColorLookup(int number)
        {
            switch (number)
            {
                case 1:
                    return "Blue";
                case 2:
                    return "Orange";
                case 3:
                    return "Green";
                case 4:
                    return "Brown";
                case 5:
                    return "Slate";
                case 6:
                    return "White";
                case 7:
                    return "Red";
                case 8:
                    return "Black";
                case 9:
                    return "Yellow";
                case 10:
                    return "Violet";
                case 11:
                    return "Rose";
                case 12:
                    return "Aqua";
                default:
                    return string.Empty;
            }
        }
        */

        /// <summary>
        /// Generates a number of buffer tubes and fiber records for a fiber cable, given a configuration. 
        /// </summary>
        /// <param name="feature">IFeature to generate for</param>
        /// <param name="configuration">Specification of buffer and fiber counts</param>
        /// <param name="progressDialog">Progress dialog for user notification</param>
        /// <param name="trackCancel">TrackCancel used in the progress dialog</param>
        /// <returns>Success</returns>
        private bool GenerateUnits(ESRI.ArcGIS.Geodatabase.IFeature feature, FiberCableConfiguration configuration, ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog, ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel)
        {
            bool isComplete = false;
            bool isCancelled = false;
            Guid g;

            ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = (ESRI.ArcGIS.esriSystem.IStepProgressor)progressDialog;
            ESRI.ArcGIS.Geodatabase.IObjectClass ftClass = feature.Class;

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IRelationshipClass cableHasBuffer = GdbUtils.GetRelationshipClass(ftClass, ConfigUtil.FiberCableToBufferRelClassName);
                releaser.ManageLifetime(cableHasBuffer);
                ESRI.ArcGIS.Geodatabase.IRelationshipClass cableHasFiber = GdbUtils.GetRelationshipClass(ftClass, ConfigUtil.FiberCableToFiberRelClassName);
                releaser.ManageLifetime(cableHasFiber);
                ESRI.ArcGIS.Geodatabase.IRelationshipClass bufferHasFiber = GdbUtils.GetRelationshipClass(ftClass, ConfigUtil.BufferToFiberRelClassName);
                releaser.ManageLifetime(bufferHasFiber);

                ESRI.ArcGIS.Geodatabase.ITable bufferTable = cableHasBuffer.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                ESRI.ArcGIS.Geodatabase.ITable fiberTable = cableHasFiber.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;

                // Fields to populate on buffer
                int bufferIpidIdx = bufferTable.Fields.FindField(ConfigUtil.IpidFieldName);
                int fiberCountIdx = bufferTable.Fields.FindField(ConfigUtil.NumberOfFibersFieldName);
                int bufferToCableIdx = bufferTable.Fields.FindField(cableHasBuffer.OriginForeignKey);
                object bufferToCableValue = feature.get_Value(feature.Fields.FindField(cableHasBuffer.OriginPrimaryKey));

                // Fields to populate on fiber
                int fiberIpidIdx = fiberTable.Fields.FindField(ConfigUtil.IpidFieldName);
                int fiberNumberIdx = fiberTable.Fields.FindField(ConfigUtil.Fiber_NumberFieldName);
                int fiberColorIdx = fiberTable.Fields.FindField(ConfigUtil.Fiber_ColorFieldName);
                int fiberToCableIdx = fiberTable.Fields.FindField(cableHasFiber.OriginForeignKey);
                object fiberToCableValue = feature.get_Value(feature.Fields.FindField(cableHasFiber.OriginPrimaryKey));
                int fiberToBufferIdx = fiberTable.Fields.FindField(bufferHasFiber.OriginForeignKey);
                int fiberToBufferValueIdx = bufferTable.Fields.FindField(bufferHasFiber.OriginPrimaryKey);

                // Research using InsertCursor for speed.
                int fiberNumber = 0;
                for (int bufferIdx = 1; bufferIdx <= configuration.BufferCount; bufferIdx++)
                {
                    g = Guid.NewGuid();
                    string bufferId = g.ToString("B").ToUpper();

                    ESRI.ArcGIS.Geodatabase.IRow row = bufferTable.CreateRow();
                    releaser.ManageLifetime(row);

                    row.set_Value(bufferIpidIdx, bufferId);
                    row.set_Value(fiberCountIdx, configuration.FibersPerTube);
                    row.set_Value(bufferToCableIdx, bufferToCableValue); 
                    row.Store();

                    object fiberToBufferValue = row.get_Value(fiberToBufferValueIdx);

                    // Research using InsertCursor for speed.
                    for (int fiberIdx = 1; fiberIdx <= configuration.FibersPerTube; fiberIdx++)
                    {
                        fiberNumber++;
                        progressDialog.Description = string.Format("Creating fiber {0} of {1}", fiberNumber, configuration.TotalFiberCount);
                        stepProgressor.Step();

                        g = Guid.NewGuid();
                        ESRI.ArcGIS.Geodatabase.IRow fiberRow = fiberTable.CreateRow();
                        releaser.ManageLifetime(fiberRow);


                        fiberRow.set_Value(fiberIpidIdx, g.ToString("B").ToUpper());
                        fiberRow.set_Value(fiberNumberIdx, fiberNumber);
                        
                        // Dangerous if coded values are altered but while 
                        // domain type is int Rather than string coded, this 
                        // is quickest way to add this
                        // Dont do for fiber groupings of more than 12
                        if (configuration.FibersPerTube <= 12)
                            fiberRow.set_Value(fiberColorIdx, fiberIdx);
    
                        fiberRow.set_Value(fiberToBufferIdx, fiberToBufferValue);
                        fiberRow.set_Value(fiberToCableIdx, fiberToCableValue);

                        fiberRow.Store();

                        if (!trackCancel.Continue())
                        {
                            isCancelled = true;
                            break;
                        }
                    }

                    if (!trackCancel.Continue())
                    {
                        isCancelled = true;
                        break;
                    }
                }

                if (!isCancelled)
                {
                    isComplete = true;
                }
            }

            return isComplete;
        }

    }
}
