namespace ALPR
{
    partial class frmModelComparison
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.groupBoxModels = new GroupBox();
            this.btnSelectModel1 = new Button();
            this.btnSelectModel2 = new Button();
            this.btnSelectModel3 = new Button();
            this.lblModel1 = new Label();
            this.lblModel2 = new Label();
            this.lblModel3 = new Label();
            this.groupBoxTestData = new GroupBox();
            this.btnSelectTestData = new Button();
            this.lblTestData = new Label();
            this.lblImageCount = new Label();
            this.groupBoxSettings = new GroupBox();
            this.nudConfidence = new NumericUpDown();
            this.lblConfidence = new Label();
            this.nudNMSThreshold = new NumericUpDown();
            this.lblNMSThreshold = new Label();
            this.chkEnableNMS = new CheckBox();
            this.chkUseGpu = new CheckBox();
            this.groupBoxTest = new GroupBox();
            this.btnStartTest = new Button();
            this.btnExportResults = new Button();
            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.groupBoxResults = new GroupBox();
            this.dataGridResults = new DataGridView();
            this.splitContainer = new SplitContainer();
            this.groupBoxSummary = new GroupBox();
            this.txtSummary = new TextBox();
            this.groupBoxPreview = new GroupBox();
            this.pictureBoxPreview = new PictureBox();
            this.lblPreviewInfo = new Label();
            
            this.groupBoxModels.SuspendLayout();
            this.groupBoxTestData.SuspendLayout();
            this.groupBoxSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.nudConfidence).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.nudNMSThreshold).BeginInit();
            this.groupBoxTest.SuspendLayout();
            this.groupBoxResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.dataGridResults).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.groupBoxSummary.SuspendLayout();
            this.groupBoxPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.pictureBoxPreview).BeginInit();
            this.SuspendLayout();

            // 
            // groupBoxModels
            // 
            this.groupBoxModels.Controls.Add(this.btnSelectModel1);
            this.groupBoxModels.Controls.Add(this.btnSelectModel2);
            this.groupBoxModels.Controls.Add(this.btnSelectModel3);
            this.groupBoxModels.Controls.Add(this.lblModel1);
            this.groupBoxModels.Controls.Add(this.lblModel2);
            this.groupBoxModels.Controls.Add(this.lblModel3);
            this.groupBoxModels.Location = new Point(12, 12);
            this.groupBoxModels.Name = "groupBoxModels";
            this.groupBoxModels.Size = new Size(300, 140);
            this.groupBoxModels.TabIndex = 0;
            this.groupBoxModels.TabStop = false;
            this.groupBoxModels.Text = "?? Model Seçimi";

            // 
            // btnSelectModel1
            // 
            this.btnSelectModel1.Location = new Point(15, 25);
            this.btnSelectModel1.Name = "btnSelectModel1";
            this.btnSelectModel1.Size = new Size(80, 25);
            this.btnSelectModel1.TabIndex = 0;
            this.btnSelectModel1.Text = "Model 1";
            this.btnSelectModel1.UseVisualStyleBackColor = true;
            this.btnSelectModel1.Click += this.btnSelectModel1_Click;

            // 
            // lblModel1
            // 
            this.lblModel1.AutoSize = true;
            this.lblModel1.Location = new Point(105, 30);
            this.lblModel1.Name = "lblModel1";
            this.lblModel1.Size = new Size(90, 15);
            this.lblModel1.TabIndex = 1;
            this.lblModel1.Text = "Model 1: Seçilmedi";

            // 
            // btnSelectModel2
            // 
            this.btnSelectModel2.Location = new Point(15, 60);
            this.btnSelectModel2.Name = "btnSelectModel2";
            this.btnSelectModel2.Size = new Size(80, 25);
            this.btnSelectModel2.TabIndex = 2;
            this.btnSelectModel2.Text = "Model 2";
            this.btnSelectModel2.UseVisualStyleBackColor = true;
            this.btnSelectModel2.Click += this.btnSelectModel2_Click;

            // 
            // lblModel2
            // 
            this.lblModel2.AutoSize = true;
            this.lblModel2.Location = new Point(105, 65);
            this.lblModel2.Name = "lblModel2";
            this.lblModel2.Size = new Size(90, 15);
            this.lblModel2.TabIndex = 3;
            this.lblModel2.Text = "Model 2: Seçilmedi";

            // 
            // btnSelectModel3
            // 
            this.btnSelectModel3.Location = new Point(15, 95);
            this.btnSelectModel3.Name = "btnSelectModel3";
            this.btnSelectModel3.Size = new Size(80, 25);
            this.btnSelectModel3.TabIndex = 4;
            this.btnSelectModel3.Text = "Model 3";
            this.btnSelectModel3.UseVisualStyleBackColor = true;
            this.btnSelectModel3.Click += this.btnSelectModel3_Click;

            // 
            // lblModel3
            // 
            this.lblModel3.AutoSize = true;
            this.lblModel3.Location = new Point(105, 100);
            this.lblModel3.Name = "lblModel3";
            this.lblModel3.Size = new Size(90, 15);
            this.lblModel3.TabIndex = 5;
            this.lblModel3.Text = "Model 3: Seçilmedi";

            // 
            // groupBoxTestData
            // 
            this.groupBoxTestData.Controls.Add(this.btnSelectTestData);
            this.groupBoxTestData.Controls.Add(this.lblTestData);
            this.groupBoxTestData.Controls.Add(this.lblImageCount);
            this.groupBoxTestData.Location = new Point(330, 12);
            this.groupBoxTestData.Name = "groupBoxTestData";
            this.groupBoxTestData.Size = new Size(300, 80);
            this.groupBoxTestData.TabIndex = 1;
            this.groupBoxTestData.TabStop = false;
            this.groupBoxTestData.Text = "?? Test Verisi";

            // 
            // btnSelectTestData
            // 
            this.btnSelectTestData.Location = new Point(15, 25);
            this.btnSelectTestData.Name = "btnSelectTestData";
            this.btnSelectTestData.Size = new Size(100, 25);
            this.btnSelectTestData.TabIndex = 0;
            this.btnSelectTestData.Text = "Klasör Seç";
            this.btnSelectTestData.UseVisualStyleBackColor = true;
            this.btnSelectTestData.Click += this.btnSelectTestData_Click;

            // 
            // lblTestData
            // 
            this.lblTestData.AutoSize = true;
            this.lblTestData.Location = new Point(125, 30);
            this.lblTestData.Name = "lblTestData";
            this.lblTestData.Size = new Size(130, 15);
            this.lblTestData.TabIndex = 1;
            this.lblTestData.Text = "Test Verisi: Seçilmedi";

            // 
            // lblImageCount
            // 
            this.lblImageCount.AutoSize = true;
            this.lblImageCount.Location = new Point(15, 50);
            this.lblImageCount.Name = "lblImageCount";
            this.lblImageCount.Size = new Size(90, 15);
            this.lblImageCount.TabIndex = 2;
            this.lblImageCount.Text = "Resim Sayýsý: 0";

            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Controls.Add(this.nudConfidence);
            this.groupBoxSettings.Controls.Add(this.lblConfidence);
            this.groupBoxSettings.Controls.Add(this.nudNMSThreshold);
            this.groupBoxSettings.Controls.Add(this.lblNMSThreshold);
            this.groupBoxSettings.Controls.Add(this.chkEnableNMS);
            this.groupBoxSettings.Controls.Add(this.chkUseGpu);
            this.groupBoxSettings.Location = new Point(330, 100);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Size = new Size(300, 80);
            this.groupBoxSettings.TabIndex = 2;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "?? Test Ayarlarý";

            // 
            // nudConfidence
            // 
            this.nudConfidence.DecimalPlaces = 2;
            this.nudConfidence.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this.nudConfidence.Location = new Point(80, 25);
            this.nudConfidence.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudConfidence.Name = "nudConfidence";
            this.nudConfidence.Size = new Size(60, 23);
            this.nudConfidence.TabIndex = 0;
            this.nudConfidence.Value = new decimal(new int[] { 6, 0, 0, 65536 });

            // 
            // lblConfidence
            // 
            this.lblConfidence.AutoSize = true;
            this.lblConfidence.Location = new Point(15, 27);
            this.lblConfidence.Name = "lblConfidence";
            this.lblConfidence.Size = new Size(45, 15);
            this.lblConfidence.TabIndex = 1;
            this.lblConfidence.Text = "Güven:";

            // 
            // nudNMSThreshold
            // 
            this.nudNMSThreshold.DecimalPlaces = 2;
            this.nudNMSThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this.nudNMSThreshold.Location = new Point(200, 25);
            this.nudNMSThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudNMSThreshold.Name = "nudNMSThreshold";
            this.nudNMSThreshold.Size = new Size(60, 23);
            this.nudNMSThreshold.TabIndex = 2;
            this.nudNMSThreshold.Value = new decimal(new int[] { 45, 0, 0, 131072 });

            // 
            // lblNMSThreshold
            // 
            this.lblNMSThreshold.AutoSize = true;
            this.lblNMSThreshold.Location = new Point(155, 27);
            this.lblNMSThreshold.Name = "lblNMSThreshold";
            this.lblNMSThreshold.Size = new Size(35, 15);
            this.lblNMSThreshold.TabIndex = 3;
            this.lblNMSThreshold.Text = "NMS:";

            // 
            // chkEnableNMS
            // 
            this.chkEnableNMS.AutoSize = true;
            this.chkEnableNMS.Checked = true;
            this.chkEnableNMS.CheckState = CheckState.Checked;
            this.chkEnableNMS.Location = new Point(15, 55);
            this.chkEnableNMS.Name = "chkEnableNMS";
            this.chkEnableNMS.Size = new Size(80, 19);
            this.chkEnableNMS.TabIndex = 4;
            this.chkEnableNMS.Text = "NMS Etkin";
            this.chkEnableNMS.UseVisualStyleBackColor = true;

            // 
            // chkUseGpu
            // 
            this.chkUseGpu.AutoSize = true;
            this.chkUseGpu.Location = new Point(155, 55);
            this.chkUseGpu.Name = "chkUseGpu";
            this.chkUseGpu.Size = new Size(95, 19);
            this.chkUseGpu.TabIndex = 5;
            this.chkUseGpu.Text = "GPU Kullan";
            this.chkUseGpu.UseVisualStyleBackColor = true;

            // 
            // groupBoxTest
            // 
            this.groupBoxTest.Controls.Add(this.btnStartTest);
            this.groupBoxTest.Controls.Add(this.btnExportResults);
            this.groupBoxTest.Controls.Add(this.progressBar);
            this.groupBoxTest.Controls.Add(this.lblStatus);
            this.groupBoxTest.Location = new Point(650, 12);
            this.groupBoxTest.Name = "groupBoxTest";
            this.groupBoxTest.Size = new Size(320, 140);
            this.groupBoxTest.TabIndex = 3;
            this.groupBoxTest.TabStop = false;
            this.groupBoxTest.Text = "?? Test Kontrolü";

            // 
            // btnStartTest
            // 
            this.btnStartTest.BackColor = Color.LightGreen;
            this.btnStartTest.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStartTest.Location = new Point(15, 25);
            this.btnStartTest.Name = "btnStartTest";
            this.btnStartTest.Size = new Size(140, 35);
            this.btnStartTest.TabIndex = 0;
            this.btnStartTest.Text = "?? Testi Baþlat";
            this.btnStartTest.UseVisualStyleBackColor = false;
            this.btnStartTest.Click += this.btnStartTest_Click;

            // 
            // btnExportResults
            // 
            this.btnExportResults.Location = new Point(165, 25);
            this.btnExportResults.Name = "btnExportResults";
            this.btnExportResults.Size = new Size(140, 35);
            this.btnExportResults.TabIndex = 1;
            this.btnExportResults.Text = "?? Sonuçlarý Dýþa Aktar";
            this.btnExportResults.UseVisualStyleBackColor = true;
            this.btnExportResults.Click += this.btnExportResults_Click;

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(15, 75);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(290, 23);
            this.progressBar.TabIndex = 2;

            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(15, 105);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(120, 15);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Model test hazýr.";

            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.splitContainer.Location = new Point(12, 160);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = Orientation.Horizontal;
            
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.groupBoxResults);
            
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.groupBoxSummary);
            this.splitContainer.Panel2.Controls.Add(this.groupBoxPreview);
            this.splitContainer.Size = new Size(958, 450);
            this.splitContainer.SplitterDistance = 250;
            this.splitContainer.TabIndex = 4;

            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Controls.Add(this.dataGridResults);
            this.groupBoxResults.Dock = DockStyle.Fill;
            this.groupBoxResults.Location = new Point(0, 0);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new Size(958, 250);
            this.groupBoxResults.TabIndex = 0;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "?? Test Sonuçlarý";

            // 
            // dataGridResults
            // 
            this.dataGridResults.AllowUserToAddRows = false;
            this.dataGridResults.AllowUserToDeleteRows = false;
            this.dataGridResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridResults.Dock = DockStyle.Fill;
            this.dataGridResults.Location = new Point(3, 19);
            this.dataGridResults.MultiSelect = false;
            this.dataGridResults.Name = "dataGridResults";
            this.dataGridResults.ReadOnly = true;
            this.dataGridResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridResults.Size = new Size(952, 228);
            this.dataGridResults.TabIndex = 0;
            this.dataGridResults.SelectionChanged += this.dataGridResults_SelectionChanged;

            // 
            // groupBoxSummary
            // 
            this.groupBoxSummary.Controls.Add(this.txtSummary);
            this.groupBoxSummary.Location = new Point(3, 3);
            this.groupBoxSummary.Name = "groupBoxSummary";
            this.groupBoxSummary.Size = new Size(500, 190);
            this.groupBoxSummary.TabIndex = 0;
            this.groupBoxSummary.TabStop = false;
            this.groupBoxSummary.Text = "?? Performans Özeti";

            // 
            // txtSummary
            // 
            this.txtSummary.BackColor = Color.Black;
            this.txtSummary.Dock = DockStyle.Fill;
            this.txtSummary.Font = new Font("Consolas", 9F);
            this.txtSummary.ForeColor = Color.Lime;
            this.txtSummary.Location = new Point(3, 19);
            this.txtSummary.Multiline = true;
            this.txtSummary.Name = "txtSummary";
            this.txtSummary.ReadOnly = true;
            this.txtSummary.ScrollBars = ScrollBars.Vertical;
            this.txtSummary.Size = new Size(494, 168);
            this.txtSummary.TabIndex = 0;
            this.txtSummary.Text = "Test sonuçlarý burada görünecek...";

            // 
            // groupBoxPreview
            // 
            this.groupBoxPreview.Controls.Add(this.pictureBoxPreview);
            this.groupBoxPreview.Controls.Add(this.lblPreviewInfo);
            this.groupBoxPreview.Location = new Point(510, 3);
            this.groupBoxPreview.Name = "groupBoxPreview";
            this.groupBoxPreview.Size = new Size(445, 190);
            this.groupBoxPreview.TabIndex = 1;
            this.groupBoxPreview.TabStop = false;
            this.groupBoxPreview.Text = "??? Resim Önizleme";

            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBoxPreview.Location = new Point(6, 19);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new Size(433, 140);
            this.pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBoxPreview.TabIndex = 0;
            this.pictureBoxPreview.TabStop = false;

            // 
            // lblPreviewInfo
            // 
            this.lblPreviewInfo.AutoSize = true;
            this.lblPreviewInfo.Location = new Point(6, 165);
            this.lblPreviewInfo.Name = "lblPreviewInfo";
            this.lblPreviewInfo.Size = new Size(150, 15);
            this.lblPreviewInfo.TabIndex = 1;
            this.lblPreviewInfo.Text = "Resim seçin...";

            // 
            // frmModelComparison
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(982, 622);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.groupBoxTest);
            this.Controls.Add(this.groupBoxSettings);
            this.Controls.Add(this.groupBoxTestData);
            this.Controls.Add(this.groupBoxModels);
            this.MinimumSize = new Size(1000, 660);
            this.Name = "frmModelComparison";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "?? Model Performans Karþýlaþtýrmasý - ALPR";
            this.FormClosing += this.frmModelComparison_FormClosing;
            
            this.groupBoxModels.ResumeLayout(false);
            this.groupBoxModels.PerformLayout();
            this.groupBoxTestData.ResumeLayout(false);
            this.groupBoxTestData.PerformLayout();
            this.groupBoxSettings.ResumeLayout(false);
            this.groupBoxSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.nudConfidence).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.nudNMSThreshold).EndInit();
            this.groupBoxTest.ResumeLayout(false);
            this.groupBoxTest.PerformLayout();
            this.groupBoxResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.dataGridResults).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.splitContainer).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.groupBoxSummary.ResumeLayout(false);
            this.groupBoxSummary.PerformLayout();
            this.groupBoxPreview.ResumeLayout(false);
            this.groupBoxPreview.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.pictureBoxPreview).EndInit();
            this.ResumeLayout(false);
        }

        private GroupBox groupBoxModels;
        private Button btnSelectModel1;
        private Button btnSelectModel2;
        private Button btnSelectModel3;
        private Label lblModel1;
        private Label lblModel2;
        private Label lblModel3;
        private GroupBox groupBoxTestData;
        private Button btnSelectTestData;
        private Label lblTestData;
        private Label lblImageCount;
        private GroupBox groupBoxSettings;
        private NumericUpDown nudConfidence;
        private Label lblConfidence;
        private NumericUpDown nudNMSThreshold;
        private Label lblNMSThreshold;
        private CheckBox chkEnableNMS;
        private CheckBox chkUseGpu;
        private GroupBox groupBoxTest;
        private Button btnStartTest;
        private Button btnExportResults;
        private ProgressBar progressBar;
        private Label lblStatus;
        private SplitContainer splitContainer;
        private GroupBox groupBoxResults;
        private DataGridView dataGridResults;
        private GroupBox groupBoxSummary;
        private TextBox txtSummary;
        private GroupBox groupBoxPreview;
        private PictureBox pictureBoxPreview;
        private Label lblPreviewInfo;
    }
}