namespace Esri_Telecom_Tools.Windows
{
    partial class FiberSpliceWindow
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblAvailableB = new System.Windows.Forms.Label();
            this.lblAvailableA = new System.Windows.Forms.Label();
            this.cboSpliceClosure = new System.Windows.Forms.ComboBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.lblFlashFrom = new System.Windows.Forms.LinkLabel();
            this.lblSpliceClosure = new System.Windows.Forms.Label();
            this.cboCableA = new System.Windows.Forms.ComboBox();
            this.cboCableB = new System.Windows.Forms.ComboBox();
            this.lblCableB = new System.Windows.Forms.Label();
            this.lblCableA = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.grdSplices = new System.Windows.Forms.DataGridView();
            this.colRangeA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRangeB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLoss = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdSplices)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.grdSplices, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.MinimumSize = new System.Drawing.Size(701, 343);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(701, 343);
            this.tableLayoutPanel1.TabIndex = 29;
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox1.Controls.Add(this.lblAvailableB);
            this.groupBox1.Controls.Add(this.lblAvailableA);
            this.groupBox1.Controls.Add(this.cboSpliceClosure);
            this.groupBox1.Controls.Add(this.linkLabel1);
            this.groupBox1.Controls.Add(this.linkLabel2);
            this.groupBox1.Controls.Add(this.lblFlashFrom);
            this.groupBox1.Controls.Add(this.lblSpliceClosure);
            this.groupBox1.Controls.Add(this.cboCableA);
            this.groupBox1.Controls.Add(this.cboCableB);
            this.groupBox1.Controls.Add(this.lblCableB);
            this.groupBox1.Controls.Add(this.lblCableA);
            this.groupBox1.Controls.Add(this.btnSave);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(211, 337);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection Info";
            // 
            // lblAvailableB
            // 
            this.lblAvailableB.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lblAvailableB.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAvailableB.Location = new System.Drawing.Point(7, 242);
            this.lblAvailableB.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAvailableB.Name = "lblAvailableB";
            this.lblAvailableB.Size = new System.Drawing.Size(197, 53);
            this.lblAvailableB.TabIndex = 16;
            // 
            // lblAvailableA
            // 
            this.lblAvailableA.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lblAvailableA.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblAvailableA.Location = new System.Drawing.Point(7, 130);
            this.lblAvailableA.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAvailableA.Name = "lblAvailableA";
            this.lblAvailableA.Size = new System.Drawing.Size(197, 56);
            this.lblAvailableA.TabIndex = 15;
            // 
            // cboSpliceClosure
            // 
            this.cboSpliceClosure.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSpliceClosure.Location = new System.Drawing.Point(7, 42);
            this.cboSpliceClosure.Margin = new System.Windows.Forms.Padding(4);
            this.cboSpliceClosure.Name = "cboSpliceClosure";
            this.cboSpliceClosure.Size = new System.Drawing.Size(197, 24);
            this.cboSpliceClosure.TabIndex = 0;
            this.cboSpliceClosure.SelectedIndexChanged += new System.EventHandler(this.cboSpliceClosure_SelectedIndexChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(162, 193);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(42, 17);
            this.linkLabel1.TabIndex = 25;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Flash";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashB_LinkClicked);
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(162, 21);
            this.linkLabel2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(42, 17);
            this.linkLabel2.TabIndex = 26;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Flash";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashSplice_LinkClicked);
            // 
            // lblFlashFrom
            // 
            this.lblFlashFrom.AutoSize = true;
            this.lblFlashFrom.Location = new System.Drawing.Point(162, 81);
            this.lblFlashFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFlashFrom.Name = "lblFlashFrom";
            this.lblFlashFrom.Size = new System.Drawing.Size(42, 17);
            this.lblFlashFrom.TabIndex = 24;
            this.lblFlashFrom.TabStop = true;
            this.lblFlashFrom.Text = "Flash";
            this.lblFlashFrom.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashA_LinkClicked);
            // 
            // lblSpliceClosure
            // 
            this.lblSpliceClosure.AutoSize = true;
            this.lblSpliceClosure.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSpliceClosure.Location = new System.Drawing.Point(7, 21);
            this.lblSpliceClosure.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSpliceClosure.Name = "lblSpliceClosure";
            this.lblSpliceClosure.Size = new System.Drawing.Size(117, 17);
            this.lblSpliceClosure.TabIndex = 10;
            this.lblSpliceClosure.Text = "Splice Closure:";
            // 
            // cboCableA
            // 
            this.cboCableA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCableA.Location = new System.Drawing.Point(7, 102);
            this.cboCableA.Margin = new System.Windows.Forms.Padding(4);
            this.cboCableA.Name = "cboCableA";
            this.cboCableA.Size = new System.Drawing.Size(197, 24);
            this.cboCableA.TabIndex = 0;
            this.cboCableA.SelectedIndexChanged += new System.EventHandler(this.cboCableA_SelectedIndexChanged);
            // 
            // cboCableB
            // 
            this.cboCableB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCableB.Location = new System.Drawing.Point(7, 214);
            this.cboCableB.Margin = new System.Windows.Forms.Padding(4);
            this.cboCableB.Name = "cboCableB";
            this.cboCableB.Size = new System.Drawing.Size(197, 24);
            this.cboCableB.TabIndex = 13;
            this.cboCableB.SelectedIndexChanged += new System.EventHandler(this.cboCableB_SelectedIndexChanged);
            // 
            // lblCableB
            // 
            this.lblCableB.AutoSize = true;
            this.lblCableB.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCableB.Location = new System.Drawing.Point(7, 193);
            this.lblCableB.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCableB.Name = "lblCableB";
            this.lblCableB.Size = new System.Drawing.Size(69, 17);
            this.lblCableB.TabIndex = 12;
            this.lblCableB.Text = "Cable B:";
            // 
            // lblCableA
            // 
            this.lblCableA.AutoSize = true;
            this.lblCableA.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCableA.Location = new System.Drawing.Point(7, 82);
            this.lblCableA.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCableA.Name = "lblCableA";
            this.lblCableA.Size = new System.Drawing.Size(69, 17);
            this.lblCableA.TabIndex = 0;
            this.lblCableA.Text = "Cable A:";
            // 
            // btnSave
            // 
            this.btnSave.AutoSize = true;
            this.btnSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(3, 307);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(205, 27);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "Update";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // grdSplices
            // 
            this.grdSplices.AllowUserToResizeRows = false;
            this.grdSplices.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdSplices.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.grdSplices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdSplices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRangeA,
            this.colRangeB,
            this.colLoss,
            this.colType});
            this.grdSplices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdSplices.Location = new System.Drawing.Point(221, 4);
            this.grdSplices.Margin = new System.Windows.Forms.Padding(4);
            this.grdSplices.MinimumSize = new System.Drawing.Size(476, 335);
            this.grdSplices.MultiSelect = false;
            this.grdSplices.Name = "grdSplices";
            this.grdSplices.RowTemplate.Height = 24;
            this.grdSplices.Size = new System.Drawing.Size(476, 335);
            this.grdSplices.StandardTab = true;
            this.grdSplices.TabIndex = 14;
            this.grdSplices.TabStop = false;
            this.grdSplices.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdSplices_CellEndEdit);
            this.grdSplices.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.grdSplices_UserDeletingRow);
            // 
            // colRangeA
            // 
            this.colRangeA.FillWeight = 25F;
            this.colRangeA.HeaderText = "Range A";
            this.colRangeA.MaxInputLength = 15;
            this.colRangeA.Name = "colRangeA";
            this.colRangeA.ToolTipText = "Specify the fiber(s) to connect.\\n Separate range values with\'-\' character eg. \"1" +
    "-2\"";
            // 
            // colRangeB
            // 
            this.colRangeB.FillWeight = 25F;
            this.colRangeB.HeaderText = "Range B";
            this.colRangeB.MaxInputLength = 15;
            this.colRangeB.Name = "colRangeB";
            this.colRangeB.ToolTipText = "Specify the fiber(s) to connect.\\n Separate range values with\'-\' character eg. \"1" +
    "-2\"";
            // 
            // colLoss
            // 
            this.colLoss.FillWeight = 20F;
            this.colLoss.HeaderText = "Loss";
            this.colLoss.Name = "colLoss";
            // 
            // colType
            // 
            this.colType.FillWeight = 30F;
            this.colType.HeaderText = "Type";
            this.colType.Name = "colType";
            // 
            // FiberSpliceWindow
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(701, 343);
            this.Name = "FiberSpliceWindow";
            this.Size = new System.Drawing.Size(701, 343);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdSplices)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblAvailableB;
        private System.Windows.Forms.Label lblAvailableA;
        private System.Windows.Forms.ComboBox cboSpliceClosure;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel lblFlashFrom;
        private System.Windows.Forms.Label lblSpliceClosure;
        private System.Windows.Forms.ComboBox cboCableA;
        private System.Windows.Forms.ComboBox cboCableB;
        private System.Windows.Forms.Label lblCableB;
        private System.Windows.Forms.Label lblCableA;
        private System.Windows.Forms.DataGridView grdSplices;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRangeA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRangeB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoss;
        private System.Windows.Forms.DataGridViewComboBoxColumn colType;
        private System.Windows.Forms.Button btnSave;
//        private System.Windows.Forms.Panel panel1;




    }
}
