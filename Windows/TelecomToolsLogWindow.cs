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
using Esri_Telecom_Tools.Events;
using System.Runtime.InteropServices;

namespace Esri_Telecom_Tools.Windows
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class TelecomToolsLogWindow : UserControl
    {
        private LogHelper _helper = null;

        public TelecomToolsLogWindow(object hook)
        {
            InitializeComponent();
            this.Hook = hook;
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
            private TelecomToolsLogWindow m_windowUI;

            public AddinImpl()
            {
            }

            internal TelecomToolsLogWindow UI
            {
                get { return m_windowUI; }
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new TelecomToolsLogWindow(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }

        }

        public void InitLog(LogHelper hookHelper)
        {
            if(_helper == null)
            {
                _helper = hookHelper;
            }
            _helper.LogUpdated -= new EventHandler(_helper_LogUpdated);
            _helper.LogUpdated += new EventHandler(_helper_LogUpdated);
        }

        void _helper_LogUpdated(object sender, EventArgs e)
        {
            listView1.SuspendLayout();
            if (e is TelecomLogEvent)
            {
                TelecomLogEvent te = e as TelecomLogEvent;
                ListViewItem item = new ListViewItem();
                item.Text = te._timestamp;
                item.ToolTipText = te._description;
                item.SubItems.Add(te._type);
                item.SubItems.Add(te._description);
                item.SubItems.Add(te._details);
                listView1.Items.Add(item);
            }
            listView1.Refresh();
            listView1.ResumeLayout();
        }

        private void clearLogButton_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

    }
}
