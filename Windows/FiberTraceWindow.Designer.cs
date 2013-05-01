namespace Esri_Telecom_Tools.Windows
{
    partial class FiberTraceWindow
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
            this.lblFlashFrom = new System.Windows.Forms.LinkLabel();
            this.cboPortType = new System.Windows.Forms.ComboBox();
            this.lblPortType = new System.Windows.Forms.Label();
            this.txtUnit = new System.Windows.Forms.TextBox();
            this.lblUnit = new System.Windows.Forms.Label();
            this.btnTrace = new System.Windows.Forms.Button();
            this.cboExisting = new System.Windows.Forms.ComboBox();
            this.lblExisting = new System.Windows.Forms.Label();
            this.showReportCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblFlashFrom
            // 
            this.lblFlashFrom.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblFlashFrom.AutoSize = true;
            this.lblFlashFrom.Location = new System.Drawing.Point(389, 44);
            this.lblFlashFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFlashFrom.Name = "lblFlashFrom";
            this.lblFlashFrom.Size = new System.Drawing.Size(42, 17);
            this.lblFlashFrom.TabIndex = 33;
            this.lblFlashFrom.TabStop = true;
            this.lblFlashFrom.Text = "Flash";
            this.lblFlashFrom.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFlashFrom_LinkClicked);
            // 
            // cboPortType
            // 
            this.cboPortType.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cboPortType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPortType.Enabled = false;
            this.cboPortType.FormattingEnabled = true;
            this.cboPortType.Items.AddRange(new object[] {
            "Input",
            "Output"});
            this.cboPortType.Location = new System.Drawing.Point(206, 71);
            this.cboPortType.Margin = new System.Windows.Forms.Padding(4);
            this.cboPortType.Name = "cboPortType";
            this.cboPortType.Size = new System.Drawing.Size(95, 24);
            this.cboPortType.TabIndex = 32;
            // 
            // lblPortType
            // 
            this.lblPortType.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblPortType.AutoSize = true;
            this.lblPortType.Enabled = false;
            this.lblPortType.Location = new System.Drawing.Point(5, 75);
            this.lblPortType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPortType.Name = "lblPortType";
            this.lblPortType.Size = new System.Drawing.Size(74, 17);
            this.lblPortType.TabIndex = 31;
            this.lblPortType.Text = "Port Type:";
            // 
            // txtUnit
            // 
            this.txtUnit.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtUnit.Enabled = false;
            this.txtUnit.Location = new System.Drawing.Point(206, 41);
            this.txtUnit.Margin = new System.Windows.Forms.Padding(4);
            this.txtUnit.MaxLength = 4;
            this.txtUnit.Name = "txtUnit";
            this.txtUnit.Size = new System.Drawing.Size(95, 22);
            this.txtUnit.TabIndex = 30;
            this.txtUnit.TextChanged += new System.EventHandler(this.txtFiberNumber_TextChanged);
            // 
            // lblUnit
            // 
            this.lblUnit.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblUnit.AutoSize = true;
            this.lblUnit.Location = new System.Drawing.Point(5, 44);
            this.lblUnit.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(132, 17);
            this.lblUnit.TabIndex = 29;
            this.lblUnit.Text = "Unit # (strand/port):";
            // 
            // btnTrace
            // 
            this.btnTrace.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnTrace.Enabled = false;
            this.btnTrace.Location = new System.Drawing.Point(8, 109);
            this.btnTrace.Margin = new System.Windows.Forms.Padding(4);
            this.btnTrace.Name = "btnTrace";
            this.btnTrace.Size = new System.Drawing.Size(95, 28);
            this.btnTrace.TabIndex = 28;
            this.btnTrace.Text = "Run Trace";
            this.btnTrace.UseVisualStyleBackColor = true;
            this.btnTrace.Click += new System.EventHandler(this.btnTrace_Click);
            // 
            // cboExisting
            // 
            this.cboExisting.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cboExisting.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboExisting.FormattingEnabled = true;
            this.cboExisting.Location = new System.Drawing.Point(206, 9);
            this.cboExisting.Margin = new System.Windows.Forms.Padding(4);
            this.cboExisting.Name = "cboExisting";
            this.cboExisting.Size = new System.Drawing.Size(225, 24);
            this.cboExisting.TabIndex = 27;
            this.cboExisting.SelectedIndexChanged += new System.EventHandler(this.cboExisting_SelectedIndexChanged);
            // 
            // lblExisting
            // 
            this.lblExisting.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblExisting.AutoSize = true;
            this.lblExisting.Location = new System.Drawing.Point(5, 12);
            this.lblExisting.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblExisting.Name = "lblExisting";
            this.lblExisting.Size = new System.Drawing.Size(161, 17);
            this.lblExisting.TabIndex = 26;
            this.lblExisting.Text = "Existing Cables/Devices:";
            // 
            // showReportCheckBox
            // 
            this.showReportCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.showReportCheckBox.AutoSize = true;
            this.showReportCheckBox.Location = new System.Drawing.Point(110, 114);
            this.showReportCheckBox.Name = "showReportCheckBox";
            this.showReportCheckBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.showReportCheckBox.Size = new System.Drawing.Size(215, 21);
            this.showReportCheckBox.TabIndex = 34;
            this.showReportCheckBox.Text = "Show Report When Complete";
            this.showReportCheckBox.UseVisualStyleBackColor = true;
            // 
            // FiberTraceWindow
            // 
            this.Controls.Add(this.showReportCheckBox);
            this.Controls.Add(this.lblFlashFrom);
            this.Controls.Add(this.cboPortType);
            this.Controls.Add(this.lblPortType);
            this.Controls.Add(this.txtUnit);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.btnTrace);
            this.Controls.Add(this.cboExisting);
            this.Controls.Add(this.lblExisting);
            this.Name = "FiberTraceWindow";
            this.Size = new System.Drawing.Size(444, 153);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel lblFlashFrom;
        private System.Windows.Forms.ComboBox cboPortType;
        private System.Windows.Forms.Label lblPortType;
        private System.Windows.Forms.TextBox txtUnit;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.Button btnTrace;
        private System.Windows.Forms.ComboBox cboExisting;
        private System.Windows.Forms.Label lblExisting;
        private System.Windows.Forms.CheckBox showReportCheckBox;

    }
}
