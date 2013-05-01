namespace Esri_Telecom_Tools.Windows
{
    partial class OpenWorkspaceWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.Path = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Exists = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SelectWorkspaceButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.currentWorkspaceLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(360, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select a valid Esri Telecom workspace in this document:";
            // 
            // listView1
            // 
            this.listView1.Alignment = System.Windows.Forms.ListViewAlignment.Default;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Path,
            this.Type,
            this.Exists});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Location = new System.Drawing.Point(0, 61);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(437, 405);
            this.listView1.TabIndex = 2;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            // 
            // Path
            // 
            this.Path.Text = "Path";
            this.Path.Width = 380;
            // 
            // Type
            // 
            this.Type.Text = "Type";
            this.Type.Width = 131;
            // 
            // Exists
            // 
            this.Exists.Text = "Exists";
            this.Exists.Width = 87;
            // 
            // SelectWorkspaceButton
            // 
            this.SelectWorkspaceButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SelectWorkspaceButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.SelectWorkspaceButton.Enabled = false;
            this.SelectWorkspaceButton.Location = new System.Drawing.Point(0, 466);
            this.SelectWorkspaceButton.Name = "SelectWorkspaceButton";
            this.SelectWorkspaceButton.Size = new System.Drawing.Size(437, 32);
            this.SelectWorkspaceButton.TabIndex = 3;
            this.SelectWorkspaceButton.Text = "Ok";
            this.SelectWorkspaceButton.UseVisualStyleBackColor = true;
            this.SelectWorkspaceButton.Click += new System.EventHandler(this.SelectWorkspaceButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(134, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Current Workspace:";
            // 
            // currentWorkspaceLabel
            // 
            this.currentWorkspaceLabel.AutoSize = true;
            this.currentWorkspaceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentWorkspaceLabel.Location = new System.Drawing.Point(3, 17);
            this.currentWorkspaceLabel.Name = "currentWorkspaceLabel";
            this.currentWorkspaceLabel.Size = new System.Drawing.Size(80, 17);
            this.currentWorkspaceLabel.TabIndex = 5;
            this.currentWorkspaceLabel.Text = "<Not Set>";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Controls.Add(this.label2);
            this.flowLayoutPanel2.Controls.Add(this.currentWorkspaceLabel);
            this.flowLayoutPanel2.Controls.Add(this.label1);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(437, 61);
            this.flowLayoutPanel2.TabIndex = 6;
            // 
            // OpenWorkspaceWindow
            // 
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.SelectWorkspaceButton);
            this.Name = "OpenWorkspaceWindow";
            this.Size = new System.Drawing.Size(437, 498);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader Path;
        private System.Windows.Forms.ColumnHeader Type;
        private System.Windows.Forms.ColumnHeader Exists;
        private System.Windows.Forms.Button SelectWorkspaceButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label currentWorkspaceLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;

    }
}
