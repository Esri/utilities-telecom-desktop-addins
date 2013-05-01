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

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.CartoUI;
using Esri_Telecom_Tools.Core.Utils;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;


namespace Esri_Telecom_Tools.Helpers
{
//    public abstract class HookHelperExt : ESRI.ArcGIS.Controls.HookHelperClass, System.Windows.Forms.IWin32Window
    public class HookHelperExt : ESRI.ArcGIS.Controls.HookHelperClass
    {
        private static HookHelperExt _instance = null;

        // Cache feature layers for a given workspace. Clear cache when workspace changes.
        protected Dictionary<String, IFeatureLayer> _featureLayerCache = new Dictionary<string, IFeatureLayer>();

        // Helper objects
        protected LogHelper _logHelper = LogHelper.Instance();
        protected TelecomWorkspaceHelper _wkspHelper = TelecomWorkspaceHelper.Instance();

        // Events that this object fires
        public event ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler ActiveViewSelectionChanged;
//        public event ESRI.ArcGIS.Carto.IActiveViewEvents_AfterDrawEventHandler ActiveViewAfterDraw;

//        private string EDITOR_REQUIRED_EXCEPTION_MESSAGE = "This method or property can only be used when HookHelperExt is constructed with a reference to the ESRI Object Editor.";

        private ESRI.ArcGIS.Carto.IActiveViewEvents_Event _activeViewEvents = null;
        
        // ObjectClassID key to display field index value
        private Dictionary<int, int> _displayIndices = new Dictionary<int, int>();

        public static HookHelperExt Instance(object hook)
        {
            if (_instance == null)
            {
                _instance = new HookHelperExt(hook);
            }
            return _instance;
        }


        /// <summary>
        /// Constructs a new HookHelperExt with a given hook
        /// </summary>
        /// <param name="hook">Hook</param>
        private HookHelperExt(object hook)
            : base()
        {
            base.Hook = hook;

            // Track changes to the current telecom workspace
            _wkspHelper.ValidWorkspaceSelected += new EventHandler(_wkspHelper_WorkspaceSelected);

            // Sets up events to catch afterDraw, selectionChanged, focusMapChanged etc
            if (null != this.ActiveView)
            {
                HookActiveView(this.ActiveView);
            }

            this.OnHookUpdated += new ESRI.ArcGIS.Controls.IHookHelperEvents_OnHookUpdatedEventHandler(this_OnHookUpdated);
        }

        void _wkspHelper_WorkspaceSelected(object sender, EventArgs e)
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Workspace Selected", "Clearing hook helper layer cache");
            _featureLayerCache.Clear();
        }

        /// <summary>
        /// Gets the number of map units for a given number of pixels
        /// </summary>
        /// <param name="pixelUnits">number of pixels to convert</param>
        /// <returns>double</returns>
        /// <remarks>Converted from http://resources.esri.com/help/9.3/ArcGISDesktop/com/samples/Cartography/Display/dc78c617-adbb-4145-bc9a-530230905f80.htm</remarks>
        public double ConvertPixelsToMapUnits(double pixelUnits)
        {
            double realWorldDisplayExtent = 0;
            long pixelExtent = 0;
            double sizeOfOnePixel = 0;
            ESRI.ArcGIS.Display.IDisplayTransformation pDT = null;
            tagRECT deviceRECT;
            ESRI.ArcGIS.Geometry.IEnvelope pEnv = null;
            ESRI.ArcGIS.Carto.IActiveView pActiveView = null;

            // Get the width of the display extents in Pixels
            // and get the extent of the displayed data
            // work out the size of one pixel and then return
            // the pixels units passed in mulitplied by that value

            pActiveView = this.ActiveView;

            // Get IDisplayTransformation
            pDT = pActiveView.ScreenDisplay.DisplayTransformation;

            // Get the device frame which will give us the number of pixels in the X direction
            deviceRECT = pDT.get_DeviceFrame();
            pixelExtent = deviceRECT.right - deviceRECT.left;

            // Now get the map extent of the currently visible area
            pEnv = pDT.VisibleBounds;

            // Calculate the size of one pixel
            realWorldDisplayExtent = pEnv.Width;
            sizeOfOnePixel = realWorldDisplayExtent / pixelExtent;

            //Multiply this by the input argument to get the result
            return pixelUnits * sizeOfOnePixel;
        }

        /// <summary>
        /// Finds the first layer on the focus map whose source feature class 
        /// has the given name AND where the workspace matches that of the 
        /// currently selected telecom workspace.
        /// </summary>
        /// <param name="ftClassName">Name to look for</param>
        /// <returns>IFeatureLayer</returns>
        public ESRI.ArcGIS.Carto.IFeatureLayer FindFeatureLayer(string ftClassName)
        {
            ESRI.ArcGIS.Carto.IFeatureLayer result = null;

            if (1 > ftClassName.Length)
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "ERROR", "FindFeatureLayer: ", "ftClassName not specified");        
                throw new ArgumentException("ftClassName not specified");
            }

            // Check the cache first for the layer
            if (_featureLayerCache.ContainsKey(ftClassName))
            {
                _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Found Feature Layer in cache", ftClassName);
                return _featureLayerCache[ftClassName];
            }

            ESRI.ArcGIS.esriSystem.UID geoFeatureLayerID = new ESRI.ArcGIS.esriSystem.UIDClass();
            geoFeatureLayerID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";

            ESRI.ArcGIS.Carto.IEnumLayer enumLayer = this.FocusMap.get_Layers(geoFeatureLayerID, true);

            // Step through each geofeature layer in the map
            enumLayer.Reset();
            ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = enumLayer.Next() as ESRI.ArcGIS.Carto.IFeatureLayer;

            string testName = ftClassName.ToLower(); // No reason to do this every time in the loop
            int testDotPosition = testName.LastIndexOf('.');
            testName = testName.Substring(testDotPosition + 1);

            while (ftLayer != null)
            {
                if (ftLayer.Valid)
                {
                    ESRI.ArcGIS.Geodatabase.IDataset dataset = ftLayer.FeatureClass as ESRI.ArcGIS.Geodatabase.IDataset;
                    if (null != dataset && dataset.Workspace == _wkspHelper.CurrentWorkspace)
                    {
                        string tableName = GdbUtils.ParseTableName(dataset);
                        if (tableName.ToLower() == testName)
                        {
                            result = ftLayer;
                            _featureLayerCache.Add(ftClassName, result);
                            break;
                        }
                    }
                }

                ftLayer = enumLayer.Next() as ESRI.ArcGIS.Carto.IFeatureLayer;
            }

            return result;
        }

        /// <summary>
        /// Flashes the feature 3 times with a 250ms pause
        /// </summary>
        /// <param name="feature">Feature to flash</param>
        public void FlashFeature(ESRI.ArcGIS.Geodatabase.IFeature feature)
        {
            FlashFeature(feature, 3, 250);
        }

        /// <summary>
        /// Flashes the given feature on the hook's active view
        /// </summary>
        /// <param name="msPause">Pause between flashes</param>
        /// <param name="times">Number of times to flash</param>
        /// <param name="feature">Feature to flash</param>
        public void FlashFeature(ESRI.ArcGIS.Geodatabase.IFeature feature, int times, int msPause)
        {
            if (null == feature)
            {
                throw new ArgumentNullException("feature");
            }

            if (null != this.ActiveView)
            {
                ESRI.ArcGIS.Display.IScreenDisplay screenDisplay = this.ActiveView.ScreenDisplay;
                if (null != screenDisplay)
                {
                    ESRI.ArcGIS.Carto.IIdentifyObj obj = new FeatureIdentifyObjectClass();
                    ((ESRI.ArcGIS.Carto.IFeatureIdentifyObj)obj).Feature = feature;

                    for (int i = 0; i < times; i++)
                    {
                        obj.Flash(screenDisplay);
                        System.Threading.Thread.Sleep(msPause);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the features from the selection that are from a given ft class
        /// </summary>
        /// <param name="ftClassName">ft class name</param>
        /// <returns>List of IFeature</returns>
        public List<ESRI.ArcGIS.Geodatabase.IFeature> GetSelectedFeatures(IFeatureLayer layer)
        {
            List<ESRI.ArcGIS.Geodatabase.IFeature> result = new List<ESRI.ArcGIS.Geodatabase.IFeature>();
            if (layer == null) return result;

            ESRI.ArcGIS.Carto.IFeatureSelection ftSelection = layer as ESRI.ArcGIS.Carto.IFeatureSelection;

            if (null != ftSelection
                && null != ftSelection.SelectionSet)
            {
                ESRI.ArcGIS.Geodatabase.ISelectionSet selectionSet = ftSelection.SelectionSet;
                ESRI.ArcGIS.Geodatabase.ICursor cursor = null;

                try
                {
                    selectionSet.Search(null, false, out cursor);
                    ESRI.ArcGIS.Geodatabase.IRow ft = cursor.NextRow();
                    while (null != ft)
                    {
                        result.Add((ESRI.ArcGIS.Geodatabase.IFeature)ft);
                        ft = cursor.NextRow();
                    }
                }
                finally
                {
                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(cursor);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the index of the display field for the feature's layer, if it is on the map
        /// </summary>
        /// <param name="feature">IFeature to look for</param>
        /// <returns>int</returns>
        //public int GetDisplayIndex(ESRI.ArcGIS.Geodatabase.IFeature feature)
        //{
        //    if (null == feature)
        //    {
        //        throw new ArgumentNullException("feature");
        //    }
        //    else if (null == feature.Class)
        //    {
        //        throw new InvalidOperationException("feature.Class cannot be null");
        //    }

        //    int result = -1;

        //    int key = feature.Class.ObjectClassID;

        //    if (_displayIndices.ContainsKey(key))
        //    {
        //        result = _displayIndices[key];
        //    }
        //    else
        //    {
        //        ESRI.ArcGIS.Geodatabase.IDataset dataset = feature.Class as ESRI.ArcGIS.Geodatabase.IDataset;
        //        if (null != dataset)
        //        {
        //            string className = GdbUtils.ParseTableName(dataset);
        //            ESRI.ArcGIS.Carto.IFeatureLayer ftLayer = this.FindFeatureLayer(className);
        //            if (null != ftLayer)
        //            {
        //                result = feature.Fields.FindField(ftLayer.DisplayField);
        //            }
        //        }

        //        _displayIndices[key] = result;  // Putting it here means that we may store -1 even if the layer was undiscoverable
        //                                        // even one time...we could move it into where result is assigned if we wanted
        //                                        // it to recheck every time this is called
        //    }

        //    return result;
        //}

        /// <summary>
        /// Creates a new Progress Dialog in the hook, with a given ITrackCancel
        /// </summary>
        /// <param name="trackCancel">Tracks cancel</param>
        /// <param name="message">The message</param>
        /// <param name="min">Minimum progress</param>
        /// <param name="max">Maximum progress</param>
        /// <param name="step">Progress per step</param>
        /// <param name="desc">Description</param>
        /// <param name="title">Title</param>
        /// <returns>IProgressDialog2</returns>
        public ESRI.ArcGIS.Framework.IProgressDialog2 CreateProgressDialog(ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel, string message, int min, int max, int step, string desc, string title)
        {
            int? hWnd = this.hWnd;

            if (null == hWnd)
            {
                throw new InvalidOperationException("Unable to determine hWnd of Hook");
            }

            //ProgressBar
            ESRI.ArcGIS.Framework.IProgressDialogFactory progressDialogFactory = new ESRI.ArcGIS.Framework.ProgressDialogFactoryClass();

            // Set the properties of the Step Progressor
            ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = progressDialogFactory.Create(trackCancel, (int)hWnd);
            stepProgressor.MinRange = min;
            stepProgressor.MaxRange = max;
            stepProgressor.StepValue = step;
            stepProgressor.Message = message;

            // Create the ProgressDialog. This automatically displays the dialog
            ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog = (ESRI.ArcGIS.Framework.IProgressDialog2)stepProgressor; // Explict Cast

            // Set the properties of the ProgressDialog
            progressDialog.CancelEnabled = true;
            progressDialog.Description = desc;
            progressDialog.Title = title;
            progressDialog.Animation = ESRI.ArcGIS.Framework.esriProgressAnimationTypes.esriProgressGlobe;

            return progressDialog;
        }

        /// <summary>
        /// Execute the Selection Tool 
        /// </summary>
        public void ExecuteSelectionTool()
        {
            ESRI.ArcGIS.Framework.IApplication app = this.Hook as ESRI.ArcGIS.Framework.IApplication;
            
            // TODO: Handle Engine where the hook is a mapcontrol or toolbar control
            if (null != app)
            {
                ESRI.ArcGIS.esriSystem.UID uid = new ESRI.ArcGIS.esriSystem.UID();
                uid.Value = "esriControls.SelectFeaturesTool";

                ESRI.ArcGIS.Framework.ICommandItem selectCommand = app.Document.CommandBars.Find(uid, false, false);
                if (null != selectCommand)
                {
                    selectCommand.Execute();
                }
            }
        }

        /// <summary>
        /// Rehook the active view events for drawing and selection stuff
        /// </summary>
        /// <param name="hookEvent">Type of hook update</param>
        private void this_OnHookUpdated(ESRI.ArcGIS.Controls.esriHookHelperEvents hookEvent)
        {
            if (null != this.ActiveView)
            {
                HookActiveView(this.ActiveView);
            }
        }

        /// <summary>
        /// Passes the event along when the map selection changes
        /// </summary>
        private void activeViewEvents_SelectionChanged()
        {
            if (null != this.ActiveViewSelectionChanged)
            {
                this.ActiveViewSelectionChanged();
            }
        }

        /// <summary>
        /// Passes the event along when the active view redraws
        /// </summary>
        //private void activeViewEvents_AfterDraw(ESRI.ArcGIS.Display.IDisplay Display, ESRI.ArcGIS.Carto.esriViewDrawPhase phase)
        //{
        //    if (null != this.ActiveViewAfterDraw)
        //    {
        //        this.ActiveViewAfterDraw(Display, phase);
        //    }
        //}

        /// <summary>
        /// Make sure the current active view is being monitored
        /// </summary>
        /// <param name="activeView">Active view to monitor</param>
        private void HookActiveView(ESRI.ArcGIS.Carto.IActiveView activeView)
        {
            if (null != _activeViewEvents)
            {
                try
                {
//                    _activeViewEvents.AfterDraw -= new ESRI.ArcGIS.Carto.IActiveViewEvents_AfterDrawEventHandler(activeViewEvents_AfterDraw);
                    _activeViewEvents.SelectionChanged -= new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(activeViewEvents_SelectionChanged);
                    _activeViewEvents.FocusMapChanged -= new IActiveViewEvents_FocusMapChangedEventHandler(_activeViewEvents_FocusMapChanged);
                }
                catch
                {
                    // This is here to debug the instances when it has detached from the RCW
                }
            }

            _activeViewEvents = (ESRI.ArcGIS.Carto.IActiveViewEvents_Event)activeView;
            
            try
            {
//                _activeViewEvents.AfterDraw += new ESRI.ArcGIS.Carto.IActiveViewEvents_AfterDrawEventHandler(activeViewEvents_AfterDraw);
                _activeViewEvents.SelectionChanged += new ESRI.ArcGIS.Carto.IActiveViewEvents_SelectionChangedEventHandler(activeViewEvents_SelectionChanged);
                _activeViewEvents.FocusMapChanged +=new IActiveViewEvents_FocusMapChangedEventHandler(_activeViewEvents_FocusMapChanged);
            }
            catch
            {
                // This is here to debug the instances when it has detached from the RCW
            }
        }

        void _activeViewEvents_FocusMapChanged()
        {
            _logHelper.addLogEntry(DateTime.Now.ToString(), "INFO", "Focus Map Changed.", "Clearing hook helper layer cache");
            _featureLayerCache.Clear();
        }

        /// <summary>
        /// Tries to find the hWnd from the various hook options. Returns null if no suitable hook hWnd is found 
        /// </summary>
        private int? hWnd
        {
            get
            {
                ESRI.ArcGIS.Framework.IApplication arcMap = base.Hook as ESRI.ArcGIS.Framework.IApplication;
                if (null != arcMap)
                {
                    return arcMap.hWnd;
                }

                ESRI.ArcGIS.Controls.IToolbarControl toolbarControl = base.Hook as ESRI.ArcGIS.Controls.IToolbarControl;
                if (null != toolbarControl)
                {
                    return toolbarControl.hWnd;
                }

                ESRI.ArcGIS.Controls.IMapControl2 mapControl = base.Hook as ESRI.ArcGIS.Controls.IMapControl2;
                if (null != mapControl)
                {
                    return mapControl.hWnd;
                }

                return null;
            }
        }

        #region IWin32Window Methods
        public System.IntPtr Handle
        {
            get
            {
                int? hWnd = this.hWnd;
                if (null != hWnd)
                {
                    return new IntPtr((int)hWnd);
                }
                else
                {
                    return new IntPtr();
                }
            }
        }
        #endregion

    }
}