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
using Esri_Telecom_Tools.Helpers;
using ESRI.ArcGIS.Geodatabase;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.ADF;
using System.Diagnostics;


namespace Esri_Telecom_Tools.Commands
{
    public class IntegrityCheckCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        // For logging messages
        protected static LogHelper _logHelper = LogHelper.Instance();
        private TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        public IntegrityCheckCommand()
        {
        }

        protected override void OnClick()
        {
            try
            {
                if (_wkspHelper.CurrentWorkspace == null)
                {
                    MessageBox.Show("You must select and open a telecom workspace before running this tool");
                    return;
                }

                DialogResult res = MessageBox.Show(null, "This test may run for a considerable time (15+ minutes) and may make modifications to the database to resolve issues. \n\nConsider taking a backup before doing this. \n\nDo you wish to proceed?", "DB Integrity Check", MessageBoxButtons.OKCancel);
                if (res != DialogResult.OK)
                    return;

                IFeatureClass cableFc = _wkspHelper.FindFeatureClass(ConfigUtil.FiberCableFtClassName);
                if (cableFc == null) { return; }

                IFeatureWorkspace fworkspace =_wkspHelper.CurrentWorkspace;
                if (fworkspace == null) { return; }

                // --------------------------------------------
                // Check the integrity of the cable feature class
                // --------------------------------------------
                IRelationshipClass fiberCableToFiberRc = fworkspace.OpenRelationshipClass(ConfigUtil.FiberCableToFiberRelClassName);
                IRelationshipClass fiberCableToBufferRc = fworkspace.OpenRelationshipClass(ConfigUtil.FiberCableToBufferRelClassName);
                IFeature feature;
                bool badCable = false;
                bool badBuffers = false;
                bool badFibers = false;
                bool conversionRequired = false;
                IWorkspaceEdit2 edit = fworkspace as IWorkspaceEdit2;
                ITransactions transaction = edit as ITransactions;

                edit.StartEditing(true);
                edit.StartEditOperation();

                IQueryFilter qf = new QueryFilter();
                qf.AddField(ConfigUtil.NumberOfBuffersFieldName);
                qf.AddField(ConfigUtil.NumberOfFibersFieldName);
                qf.AddField(ConfigUtil.IpidFieldName);

                using (ComReleaser comReleaser = new ComReleaser())
                {
                    IFeatureCursor fCursor = (IFeatureCursor)cableFc.Update(qf, false);
                    ICursor pCursor = fCursor as ICursor;
                    comReleaser.ManageLifetime(fCursor);

                    int buffersFieldIdx = cableFc.FindField(ConfigUtil.NumberOfBuffersFieldName);
                    int fibersFieldIdx = cableFc.FindField(ConfigUtil.NumberOfFibersFieldName);
                    int ipidFieldIdx = cableFc.FindField(ConfigUtil.IpidFieldName);
                    
                    int count = 0;
                    while ((feature = fCursor.NextFeature()) != null)
                    {
                        // Cables should have non null field values
                        if (feature.get_Value(ipidFieldIdx) == DBNull.Value)
                        {
                            badCable = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with NULL IPID value");
                            continue;
                        }
                        if (feature.get_Value(buffersFieldIdx) == DBNull.Value)
                        {
                            badBuffers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with NULL buffer field value", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }
                        if (feature.get_Value(fibersFieldIdx) == DBNull.Value)
                        {
                            badFibers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with NULL fiber field value", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }
                        
                        int bufferCount = (int)feature.get_Value(buffersFieldIdx);
                        int fiberCount = (int)feature.get_Value(fibersFieldIdx);

                        // Cables should have non zero values
                        if (bufferCount == 0)
                        {
                            badBuffers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with 0 buffer field value", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }
                        if (fiberCount == 0)
                        {
                            badFibers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with 0 strand field value", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }

                        // Cables should have relationships
                        int rcBufferCount = fiberCableToBufferRc.GetObjectsRelatedToObject(feature).Count;
                        if (rcBufferCount == 0)
                        {
                            badBuffers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with 0 related buffers", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }
                        int rcFiberCount = fiberCableToFiberRc.GetObjectsRelatedToObject(feature).Count;
                        if (rcFiberCount == 0)
                        {
                            badFibers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with 0 related fibers", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }

                        // Buffer field count & relationships to buffers not matching
                        if (bufferCount != rcBufferCount)
                        {
                            badBuffers = true;
                            String output = "Expected: " + bufferCount + " Found: " + rcBufferCount + " Cable ID: " + feature.get_Value(ipidFieldIdx).ToString();
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Found Fiber Cable with buffer count->relationship mismatch", output);
                            continue;
                        }

                        // other checks
                        if (rcFiberCount % rcBufferCount != 0)
                        {
                            badFibers = true;
                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Fiber Cable with invalid strand count - (relationships % buffercount) is non zero", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            continue;
                        }

                        // we must be dealing with a total count (convert to per buffer tube value)
                        if (fiberCount == rcFiberCount && bufferCount > 1)
                        {
                            count++;
                            Debug.Write(count + " Strand Total to Strands Per Buffer conversion", " Cable ID: " + feature.get_Value(ipidFieldIdx).ToString() + "\n");
                            conversionRequired = true;
                            //                            _logHelper.addLogEntry(DateTime.Now.ToString(), "INTEGRITY", "Strand Total to Strands Per Buffer conversion", "Cable ID: " + feature.get_Value(ipidFieldIdx).ToString());
                            feature.set_Value(fibersFieldIdx, rcFiberCount / rcBufferCount);
                            feature.Store();
                        }
                    }
                }
                edit.StopEditOperation();
                edit.StopEditing(true);

                if (badCable)
                    MessageBox.Show("Database integrity issues were detected. Found invalid Fiber Cable. Please see the log file for more details");
                if (badBuffers)
                    MessageBox.Show("Database integrity issues were detected. Found Fiber Cable with buffer count issues. Please see the log file for more details");
                if (badFibers)
                    MessageBox.Show("Database integrity issues were detected. Found Fiber Cable with strands count issues. Please see the log file for more details");
                if (conversionRequired)
                    MessageBox.Show("Database integrity issues were detected. Strand Total to Strands Per Buffer conversion was done.");
            }
            catch (Exception e)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "Integrity Check Error", e.Message);
            }

            MessageBox.Show("The results of the DB checks are listed in the tools Log window");
        }

        protected override void OnUpdate()
        {
        }
    }
}
