namespace ALPR
{
    partial class FullPlateList
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlTop = new Panel();
            btnTighten = new Button();
            chkAll = new CheckBox();
            btnAddClassGlobal = new Button();
            cmbFilterClass = new ComboBox();
            lblStatus = new Label();
            btnNextPage = new Button();
            txtPageInfo = new TextBox();
            btnPrevPage = new Button();
            flpPlates = new FlowLayoutPanel();
            pnlTop.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.Controls.Add(btnTighten);
            pnlTop.Controls.Add(chkAll);
            pnlTop.Controls.Add(btnAddClassGlobal);
            pnlTop.Controls.Add(cmbFilterClass);
            pnlTop.Controls.Add(lblStatus);
            pnlTop.Controls.Add(btnNextPage);
            pnlTop.Controls.Add(txtPageInfo);
            pnlTop.Controls.Add(btnPrevPage);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.Size = new Size(1334, 42);
            pnlTop.TabIndex = 0;
            // 
            // btnTighten
            // 
            btnTighten.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTighten.BackColor = Color.LightSkyBlue;
            btnTighten.FlatStyle = FlatStyle.Flat;
            btnTighten.Location = new Point(844, 5);
            btnTighten.Name = "btnTighten";
            btnTighten.Size = new Size(95, 29);
            btnTighten.TabIndex = 4;
            btnTighten.Text = "Sıkılaştır";
            btnTighten.UseVisualStyleBackColor = false;
            // 
            // chkAll
            // 
            chkAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkAll.AutoSize = true;
            chkAll.Location = new Point(1210, 8);
            chkAll.Name = "chkAll";
            chkAll.Size = new Size(115, 24);
            chkAll.TabIndex = 3;
            chkAll.Text = "Tüm Dataset";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // btnAddClassGlobal
            // 
            btnAddClassGlobal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddClassGlobal.Location = new Point(948, 5);
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
            cmbFilterClass.Location = new Point(1054, 6);
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
            // btnNextPage
            // 
            btnNextPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNextPage.Location = new Point(732, 5);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new Size(80, 30);
            btnNextPage.TabIndex = 2;
            btnNextPage.Text = "Sonraki >";
            btnNextPage.UseVisualStyleBackColor = true;
            // 
            // txtPageInfo
            // 
            txtPageInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtPageInfo.Location = new Point(622, 6);
            txtPageInfo.Name = "txtPageInfo";
            txtPageInfo.Size = new Size(110, 27);
            txtPageInfo.TabIndex = 1;
            txtPageInfo.Text = "Sayfa 1 / 1";
            txtPageInfo.TextAlign = HorizontalAlignment.Center;
            txtPageInfo.KeyDown += txtPageInfo_KeyDown;
            // 
            // btnPrevPage
            // 
            btnPrevPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrevPage.Location = new Point(542, 5);
            btnPrevPage.Name = "btnPrevPage";
            btnPrevPage.Size = new Size(80, 30);
            btnPrevPage.TabIndex = 0;
            btnPrevPage.Text = "< Önceki";
            btnPrevPage.UseVisualStyleBackColor = true;
            // 
            // flpPlates
            // 
            flpPlates.AutoScroll = true;
            flpPlates.Dock = DockStyle.Fill;
            flpPlates.Location = new Point(0, 42);
            flpPlates.Name = "flpPlates";
            flpPlates.Size = new Size(1334, 558);
            flpPlates.TabIndex = 1;
            // 
            // FullPlateList
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1334, 600);
            Controls.Add(flpPlates);
            Controls.Add(pnlTop);
            KeyPreview = true;
            Name = "FullPlateList";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Tüm Plakalar";
            FormClosing += FullPlateList_FormClosing;
            Load += FullPlateList_Load;
            KeyDown += FullPlateList_KeyDown;
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
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.TextBox txtPageInfo;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Button btnTighten;
    }
}