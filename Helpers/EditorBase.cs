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
using ESRI.ArcGIS.Editor;

namespace Esri_Telecom_Tools.Helpers
{
    public abstract class EditorBase 
    {
        protected IEditor3 _editor = null;

        // ---------------------------------
        // Events this object can throw.
        // ---------------------------------
        protected event ESRI.ArcGIS.Editor.IEditEvents_OnCreateFeatureEventHandler OnCreateFeature;

        public EditorBase(IEditor3 editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Allows event firing to be enabled
        /// </summary>
        public void onStartEditing()
        {
            if (_editor != null)
            {
                // Creation 
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnCreateFeature -= new IEditEvents_OnCreateFeatureEventHandler(editor_OnCreateFeature);
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnCreateFeature += new IEditEvents_OnCreateFeatureEventHandler(editor_OnCreateFeature);

                // Deletion
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnDeleteFeature -= new IEditEvents_OnDeleteFeatureEventHandler(editor_OnDeleteFeature);
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnDeleteFeature += new IEditEvents_OnDeleteFeatureEventHandler(editor_OnDeleteFeature);

                // Change
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnChangeFeature -= new IEditEvents_OnChangeFeatureEventHandler(editor_OnChangeFeature);
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnChangeFeature += new IEditEvents_OnChangeFeatureEventHandler(editor_OnChangeFeature);

                // EditSelectionChanged
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnSelectionChanged -= new IEditEvents_OnSelectionChangedEventHandler(editor_OnSelectionChanged);
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnSelectionChanged += new IEditEvents_OnSelectionChangedEventHandler(editor_OnSelectionChanged);
            }
        }

        /// <summary>
        /// Allows event firing to be disabled
        /// </summary>
        public void onStopEditing()
        {
            if (_editor != null)
            {
                // Create 
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnCreateFeature -= new IEditEvents_OnCreateFeatureEventHandler(editor_OnCreateFeature);
                // Delete
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnDeleteFeature -= new IEditEvents_OnDeleteFeatureEventHandler(editor_OnDeleteFeature);
                // Change
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnChangeFeature -= new IEditEvents_OnChangeFeatureEventHandler(editor_OnChangeFeature);
                // Selection
                ((ESRI.ArcGIS.Editor.IEditEvents_Event)_editor).OnSelectionChanged -= new IEditEvents_OnSelectionChangedEventHandler(editor_OnSelectionChanged);
            }
        }

        /// <summary>
        /// Passes the event along when a feature is created
        /// </summary>
        protected abstract void editor_OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj);

        /// <summary>
        /// Passes the event along when a feature is deleted
        /// </summary>
        protected abstract void editor_OnDeleteFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        
        /// <summary>
        /// Passes the event along when a feature is changed
        /// </summary>
        protected abstract void editor_OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        
        /// <summary>
        /// Passes the event along when a feature is selected
        /// </summary>
        protected abstract void editor_OnSelectionChanged();
    }
}
