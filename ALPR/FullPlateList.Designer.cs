namespace ALPR
{
    partial class FullPlateList
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
            pnlTop = new Panel();
            chkAll = new CheckBox();
            btnAddClassGlobal = new Button();
            cmbFilterClass = new ComboBox();
            lblStatus = new Label();
            flpPlates = new FlowLayoutPanel();
            pnlTop.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.Controls.Add(chkAll);
            pnlTop.Controls.Add(btnAddClassGlobal);
            pnlTop.Controls.Add(cmbFilterClass);
            pnlTop.Controls.Add(lblStatus);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.Size = new Size(900, 40);
            pnlTop.TabIndex = 0;
            // 
            // chkAll
            // 
            chkAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkAll.AutoSize = true;
            chkAll.Location = new Point(776, 8);
            chkAll.Name = "chkAll";
            chkAll.Size = new Size(115, 24);
            chkAll.TabIndex = 3;
            chkAll.Text = "Tüm Dataset";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // btnAddClassGlobal
            // 
            btnAddClassGlobal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddClassGlobal.Location = new Point(514, 5);
            btnAddClassGlobal.Name = "btnAddClassGlobal";
            btnAddClassGlobal.Size = new Size(100, 29);
            btnAddClassGlobal.TabIndex = 2;
            btnAddClassGlobal.Text = "+ Yeni Class";
            btnAddClassGlobal.UseVisualStyleBackColor = true;
            // 
            // cmbFilterClass
            // 
            cmbFilterClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbFilterClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterClass.FormattingEnabled = true;
            cmbFilterClass.Location = new Point(620, 6);
            cmbFilterClass.Name = "cmbFilterClass";
            cmbFilterClass.Size = new Size(150, 28);
            cmbFilterClass.TabIndex = 1;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 10F);
            lblStatus.Location = new Point(12, 9);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(100, 23);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Yükleniyor...";
            // 
            // flpPlates
            // 
            flpPlates.AutoScroll = true;
            flpPlates.Dock = DockStyle.Fill;
            flpPlates.Location = new Point(0, 40);
            flpPlates.Name = "flpPlates";
            flpPlates.Size = new Size(900, 560);
            flpPlates.TabIndex = 1;
            // 
            // FullPlateList
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 600);
            Controls.Add(flpPlates);
            Controls.Add(pnlTop);
            Name = "FullPlateList";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Tüm Plakalar";
            FormClosing += FullPlateList_FormClosing;
            Load += FullPlateList_Load;
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.FlowLayoutPanel flpPlates;
        private System.Windows.Forms.ComboBox cmbFilterClass;
        private System.Windows.Forms.Button btnAddClassGlobal;
        private CheckBox chkAll;
    }
}