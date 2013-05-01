namespace Esri_Telecom_Tools.Windows
{
    partial class FiberDeviceConnectionWindow
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.lblFlashTo = new System.Windows.Forms.LinkLabel();
            this.lblAvailableFrom = new System.Windows.Forms.Label();
            this.cboTo = new System.Windows.Forms.ComboBox();
            this.lblFlashFrom = new System.Windows.Forms.LinkLabel();
            this.lblAvailableTo = new System.Windows.Forms.Label();
            this.cboFrom = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.grdConnections = new System.Windows.Forms.DataGridView();
            this.colFromRange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colToRange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdConnections)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.grdConnections, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.MinimumSize = new System.Drawing.Size(591, 302);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(591, 302);
            this.tableLayoutPanel1.TabIndex = 27;
            // 
            // groupBox2
            // 
            this.groupBox2.AutoSize = true;
            this.groupBox2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox2.Controls.Add(this.lblFrom);
            this.groupBox2.Controls.Add(this.lblTo);
            this.groupBox2.Controls.Add(this.lblFlashTo);
            this.groupBox2.Controls.Add(this.lblAvailableFrom);
            this.groupBox2.Controls.Add(this.cboTo);
            this.groupBox2.Controls.Add(this.lblFlashFrom);
            this.groupBox2.Controls.Add(this.lblAvailableTo);
            this.groupBox2.Controls.Add(this.cboFrom);
            this.groupBox2.Controls.Add(this.btnSave);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(240, 296);
            this.groupBox2.TabIndex = 27;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Connection Info";
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFrom.Location = new System.Drawing.Point(7, 18);
            this.lblFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(159, 17);
            this.lblFrom.TabIndex = 16;
            this.lblFrom.Text = "From Device / Cable:";
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTo.Location = new System.Drawing.Point(7, 131);
            this.lblTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(142, 17);
            this.lblTo.TabIndex = 18;
            this.lblTo.Text = "To Device / Cable:";
            // 
            // lblFlashTo
            // 
            this.lblFlashTo.AutoSize = true;
            this.lblFlashTo.Location = new System.Drawing.Point(191, 131);
            this.lblFlashTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFlashTo.Name = "lblFlashTo";
            this.lblFlashTo.Size = new System.Drawing.Size(42, 17);
            this.lblFlashTo.TabIndex = 24;
            this.lblFlashTo.TabStop = true;
            this.lblFlashTo.Text = "Flash";
            this.lblFlashTo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashTo_LinkClicked);
            // 
            // lblAvailableFrom
            // 
            this.lblAvailableFrom.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lblAvailableFrom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAvailableFrom.Location = new System.Drawing.Point(10, 67);
            this.lblAvailableFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAvailableFrom.Name = "lblAvailableFrom";
            this.lblAvailableFrom.Size = new System.Drawing.Size(223, 54);
            this.lblAvailableFrom.TabIndex = 21;
            // 
            // cboTo
            // 
            this.cboTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTo.Location = new System.Drawing.Point(10, 152);
            this.cboTo.Margin = new System.Windows.Forms.Padding(4);
            this.cboTo.Name = "cboTo";
            this.cboTo.Size = new System.Drawing.Size(223, 24);
            this.cboTo.TabIndex = 19;
            this.cboTo.SelectedIndexChanged += new System.EventHandler(this.cboTo_SelectedIndexChanged);
            // 
            // lblFlashFrom
            // 
            this.lblFlashFrom.AutoSize = true;
            this.lblFlashFrom.Location = new System.Drawing.Point(191, 18);
            this.lblFlashFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFlashFrom.Name = "lblFlashFrom";
            this.lblFlashFrom.Size = new System.Drawing.Size(42, 17);
            this.lblFlashFrom.TabIndex = 23;
            this.lblFlashFrom.TabStop = true;
            this.lblFlashFrom.Text = "Flash";
            this.lblFlashFrom.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashFrom_LinkClicked);
            // 
            // lblAvailableTo
            // 
            this.lblAvailableTo.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lblAvailableTo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAvailableTo.Location = new System.Drawing.Point(10, 180);
            this.lblAvailableTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAvailableTo.Name = "lblAvailableTo";
            this.lblAvailableTo.Size = new System.Drawing.Size(223, 52);
            this.lblAvailableTo.TabIndex = 22;
            // 
            // cboFrom
            // 
            this.cboFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFrom.Location = new System.Drawing.Point(10, 39);
            this.cboFrom.Margin = new System.Windows.Forms.Padding(4);
            this.cboFrom.Name = "cboFrom";
            this.cboFrom.Size = new System.Drawing.Size(223, 24);
            this.cboFrom.TabIndex = 15;
            this.cboFrom.SelectedIndexChanged += new System.EventHandler(this.cboFrom_SelectedIndexChanged);
            // 
            // btnSave
            // 
            this.btnSave.AutoSize = true;
            this.btnSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(3, 266);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(234, 27);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "Update";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // grdConnections
            // 
            this.grdConnections.AllowUserToResizeRows = false;
            this.grdConnections.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdConnections.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.grdConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdConnections.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colFromRange,
            this.colToRange});
            this.grdConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdConnections.Location = new System.Drawing.Point(250, 4);
            this.grdConnections.Margin = new System.Windows.Forms.Padding(4);
            this.grdConnections.MinimumSize = new System.Drawing.Size(337, 294);
            this.grdConnections.MultiSelect = false;
            this.grdConnections.Name = "grdConnections";
            this.grdConnections.RowTemplate.Height = 24;
            this.grdConnections.Size = new System.Drawing.Size(337, 294);
            this.grdConnections.StandardTab = true;
            this.grdConnections.TabIndex = 20;
            this.grdConnections.TabStop = false;
            this.grdConnections.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdConnections_CellEndEdit);
            this.grdConnections.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.grdConnections_UserDeletingRow);
            // 
            // colFromRange
            // 
            this.colFromRange.FillWeight = 50F;
            this.colFromRange.HeaderText = "From Range";
            this.colFromRange.MaxInputLength = 15;
            this.colFromRange.Name = "colFromRange";
            // 
            // colToRange
            // 
            this.colToRange.FillWeight = 50F;
            this.colToRange.HeaderText = "To Range";
            this.colToRange.MaxInputLength = 15;
            this.colToRange.Name = "colToRange";
            // 
            // FiberDeviceConnectionWindow
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(591, 302);
            this.Name = "FiberDeviceConnectionWindow";
            this.Size = new System.Drawing.Size(591, 302);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdConnections)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.LinkLabel lblFlashTo;
        private System.Windows.Forms.Label lblAvailableFrom;
        private System.Windows.Forms.ComboBox cboTo;
        private System.Windows.Forms.LinkLabel lblFlashFrom;
        private System.Windows.Forms.Label lblAvailableTo;
        private System.Windows.Forms.ComboBox cboFrom;
        private System.Windows.Forms.DataGridView grdConnections;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFromRange;
        private System.Windows.Forms.DataGridViewTextBoxColumn colToRange;
//        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnSave;
    }
}
