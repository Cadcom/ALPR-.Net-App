namespace ALPR
{
    partial class frmALPR
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            btnSelectImage = new Button();
            btnSelectVideo = new Button();
            btnStartVideo = new Button();
            btnStopVideo = new Button();
            btnBatchProcess = new Button();
            lblCurrentModel = new Label();
            pictureBoxImage = new PictureBox();
            txtLog = new RichTextBox();
            lblLog = new Label();
            chkEnableNMS = new CheckBox();
            chkSavePlates = new CheckBox();
            chkUseGpu = new CheckBox();
            chkDebugMode = new CheckBox();
            nudNMSThreshold = new NumericUpDown();
            lblNMSThreshold = new Label();
            nudConfidenceThreshold = new NumericUpDown();
            lblConfidenceThreshold = new Label();
            lblFps = new Label();
            nudFrameSkip = new NumericUpDown();
            lblFrameSkip = new Label();
            nudCharConfidence = new NumericUpDown();
            lblCharConfidence = new Label();
            chkDirectOcr = new CheckBox();
            cmbPlateModelType = new ComboBox();
            cbOcrModel = new ComboBox();
            btnImageLabeling = new Button();
            chkMultiModel = new CheckBox();
            btnPause = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudNMSThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudConfidenceThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFrameSkip).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCharConfidence).BeginInit();
            SuspendLayout();
            // 
            // btnSelectImage
            // 
            btnSelectImage.Location = new Point(23, 27);
            btnSelectImage.Margin = new Padding(3, 4, 3, 4);
            btnSelectImage.Name = "btnSelectImage";
            btnSelectImage.Size = new Size(137, 47);
            btnSelectImage.TabIndex = 0;
            btnSelectImage.Text = "Resim Seç";
            btnSelectImage.UseVisualStyleBackColor = true;
            btnSelectImage.Click += btnSelectImage_Click;
            // 
            // btnSelectVideo
            // 
            btnSelectVideo.Location = new Point(23, 77);
            btnSelectVideo.Margin = new Padding(3, 4, 3, 4);
            btnSelectVideo.Name = "btnSelectVideo";
            btnSelectVideo.Size = new Size(137, 47);
            btnSelectVideo.TabIndex = 1;
            btnSelectVideo.Text = "Video Seç";
            btnSelectVideo.UseVisualStyleBackColor = true;
            btnSelectVideo.Click += btnSelectVideo_Click;
            // 
            // btnStartVideo
            // 
            btnStartVideo.Enabled = false;
            btnStartVideo.Location = new Point(167, 27);
            btnStartVideo.Margin = new Padding(3, 4, 3, 4);
            btnStartVideo.Name = "btnStartVideo";
            btnStartVideo.Size = new Size(98, 36);
            btnStartVideo.TabIndex = 2;
            btnStartVideo.Text = "Başlat";
            btnStartVideo.UseVisualStyleBackColor = true;
            btnStartVideo.Click += btnStartVideo_Click;
            // 
            // btnStopVideo
            // 
            btnStopVideo.Enabled = false;
            btnStopVideo.Location = new Point(167, 99);
            btnStopVideo.Margin = new Padding(3, 4, 3, 4);
            btnStopVideo.Name = "btnStopVideo";
            btnStopVideo.Size = new Size(98, 29);
            btnStopVideo.TabIndex = 3;
            btnStopVideo.Text = "Bitir";
            btnStopVideo.UseVisualStyleBackColor = true;
            btnStopVideo.Click += btnStopVideo_Click;
            // 
            // btnBatchProcess
            // 
            btnBatchProcess.BackColor = Color.Gold;
            btnBatchProcess.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBatchProcess.Location = new Point(1166, 22);
            btnBatchProcess.Margin = new Padding(3, 4, 3, 4);
            btnBatchProcess.Name = "btnBatchProcess";
            btnBatchProcess.Size = new Size(171, 41);
            btnBatchProcess.TabIndex = 25;
            btnBatchProcess.Text = "⚡ Toplu İşle";
            btnBatchProcess.UseVisualStyleBackColor = false;
            btnBatchProcess.Click += btnBatchProcess_Click;
            // 
            // lblCurrentModel
            // 
            lblCurrentModel.AutoSize = true;
            lblCurrentModel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblCurrentModel.ForeColor = Color.DarkBlue;
            lblCurrentModel.Location = new Point(272, 104);
            lblCurrentModel.Name = "lblCurrentModel";
            lblCurrentModel.Size = new Size(128, 19);
            lblCurrentModel.TabIndex = 22;
            lblCurrentModel.Text = "Model: Varsayılan";
            // 
            // pictureBoxImage
            // 
            pictureBoxImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBoxImage.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxImage.Location = new Point(23, 147);
            pictureBoxImage.Margin = new Padding(3, 4, 3, 4);
            pictureBoxImage.Name = "pictureBoxImage";
            pictureBoxImage.Size = new Size(1314, 326);
            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.TabIndex = 15;
            pictureBoxImage.TabStop = false;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.WhiteSmoke;
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Font = new Font("Consolas", 10F, FontStyle.Bold);
            txtLog.ForeColor = Color.Black;
            txtLog.Location = new Point(23, 513);
            txtLog.Margin = new Padding(3, 4, 3, 4);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.Size = new Size(1314, 159);
            txtLog.TabIndex = 17;
            txtLog.Text = "Log kutusu hazır...\n";
            // 
            // lblLog
            // 
            lblLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLog.AutoSize = true;
            lblLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLog.Location = new Point(23, 487);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(112, 20);
            lblLog.TabIndex = 16;
            lblLog.Text = "Tespit Bilgileri:";
            // 
            // chkEnableNMS
            // 
            chkEnableNMS.AutoSize = true;
            chkEnableNMS.Checked = true;
            chkEnableNMS.CheckState = CheckState.Checked;
            chkEnableNMS.Location = new Point(505, 73);
            chkEnableNMS.Margin = new Padding(3, 4, 3, 4);
            chkEnableNMS.Name = "chkEnableNMS";
            chkEnableNMS.Size = new Size(99, 24);
            chkEnableNMS.TabIndex = 4;
            chkEnableNMS.Text = "NMS Etkin";
            chkEnableNMS.UseVisualStyleBackColor = true;
            // 
            // chkSavePlates
            // 
            chkSavePlates.AutoSize = true;
            chkSavePlates.Location = new Point(842, 104);
            chkSavePlates.Margin = new Padding(3, 4, 3, 4);
            chkSavePlates.Name = "chkSavePlates";
            chkSavePlates.Size = new Size(181, 24);
            chkSavePlates.TabIndex = 18;
            chkSavePlates.Text = "Okunan Plakayı Kaydet";
            chkSavePlates.UseVisualStyleBackColor = true;
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Location = new Point(505, 39);
            chkUseGpu.Margin = new Padding(3, 4, 3, 4);
            chkUseGpu.Name = "chkUseGpu";
            chkUseGpu.Size = new Size(104, 24);
            chkUseGpu.TabIndex = 19;
            chkUseGpu.Text = "GPU Kullan";
            chkUseGpu.UseVisualStyleBackColor = true;
            chkUseGpu.CheckedChanged += chkUseGpu_CheckedChanged;
            // 
            // chkDebugMode
            // 
            chkDebugMode.AutoSize = true;
            chkDebugMode.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            chkDebugMode.ForeColor = Color.DarkRed;
            chkDebugMode.Location = new Point(902, 70);
            chkDebugMode.Margin = new Padding(3, 4, 3, 4);
            chkDebugMode.Name = "chkDebugMode";
            chkDebugMode.Size = new Size(101, 23);
            chkDebugMode.TabIndex = 24;
            chkDebugMode.Text = "🐞 DEBUG";
            chkDebugMode.UseVisualStyleBackColor = true;
            // 
            // nudNMSThreshold
            // 
            nudNMSThreshold.DecimalPlaces = 2;
            nudNMSThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudNMSThreshold.Location = new Point(725, 105);
            nudNMSThreshold.Margin = new Padding(3, 4, 3, 4);
            nudNMSThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudNMSThreshold.Name = "nudNMSThreshold";
            nudNMSThreshold.Size = new Size(69, 27);
            nudNMSThreshold.TabIndex = 7;
            nudNMSThreshold.Value = new decimal(new int[] { 45, 0, 0, 131072 });
            // 
            // lblNMSThreshold
            // 
            lblNMSThreshold.AutoSize = true;
            lblNMSThreshold.Location = new Point(641, 108);
            lblNMSThreshold.Name = "lblNMSThreshold";
            lblNMSThreshold.Size = new Size(44, 20);
            lblNMSThreshold.TabIndex = 6;
            lblNMSThreshold.Text = "NMS:";
            // 
            // nudConfidenceThreshold
            // 
            nudConfidenceThreshold.DecimalPlaces = 2;
            nudConfidenceThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudConfidenceThreshold.Location = new Point(934, 32);
            nudConfidenceThreshold.Margin = new Padding(3, 4, 3, 4);
            nudConfidenceThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudConfidenceThreshold.Name = "nudConfidenceThreshold";
            nudConfidenceThreshold.Size = new Size(69, 27);
            nudConfidenceThreshold.TabIndex = 9;
            nudConfidenceThreshold.Value = new decimal(new int[] { 11, 0, 0, 131072 });
            // 
            // lblConfidenceThreshold
            // 
            lblConfidenceThreshold.AutoSize = true;
            lblConfidenceThreshold.Location = new Point(842, 35);
            lblConfidenceThreshold.Name = "lblConfidenceThreshold";
            lblConfidenceThreshold.Size = new Size(92, 20);
            lblConfidenceThreshold.TabIndex = 8;
            lblConfidenceThreshold.Text = "Plaka Güven:";
            // 
            // lblFps
            // 
            lblFps.AutoSize = true;
            lblFps.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFps.Location = new Point(23, 147);
            lblFps.Name = "lblFps";
            lblFps.Size = new Size(73, 20);
            lblFps.TabIndex = 14;
            lblFps.Text = "FPS: 0.00";
            // 
            // nudFrameSkip
            // 
            nudFrameSkip.Location = new Point(725, 31);
            nudFrameSkip.Margin = new Padding(3, 4, 3, 4);
            nudFrameSkip.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudFrameSkip.Name = "nudFrameSkip";
            nudFrameSkip.Size = new Size(69, 27);
            nudFrameSkip.TabIndex = 11;
            nudFrameSkip.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblFrameSkip
            // 
            lblFrameSkip.AutoSize = true;
            lblFrameSkip.Location = new Point(641, 33);
            lblFrameSkip.Name = "lblFrameSkip";
            lblFrameSkip.Size = new Size(84, 20);
            lblFrameSkip.TabIndex = 10;
            lblFrameSkip.Text = "Frame Atla:";
            // 
            // nudCharConfidence
            // 
            nudCharConfidence.DecimalPlaces = 2;
            nudCharConfidence.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudCharConfidence.Location = new Point(725, 67);
            nudCharConfidence.Margin = new Padding(3, 4, 3, 4);
            nudCharConfidence.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudCharConfidence.Name = "nudCharConfidence";
            nudCharConfidence.Size = new Size(69, 27);
            nudCharConfidence.TabIndex = 13;
            nudCharConfidence.Value = new decimal(new int[] { 3, 0, 0, 65536 });
            // 
            // lblCharConfidence
            // 
            lblCharConfidence.AutoSize = true;
            lblCharConfidence.Location = new Point(641, 73);
            lblCharConfidence.Name = "lblCharConfidence";
            lblCharConfidence.Size = new Size(82, 20);
            lblCharConfidence.TabIndex = 12;
            lblCharConfidence.Text = "Kar. Güven:";
            // 
            // chkDirectOcr
            // 
            chkDirectOcr.AutoSize = true;
            chkDirectOcr.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chkDirectOcr.ForeColor = Color.DarkGreen;
            chkDirectOcr.Location = new Point(505, 104);
            chkDirectOcr.Name = "chkDirectOcr";
            chkDirectOcr.Size = new Size(128, 24);
            chkDirectOcr.TabIndex = 29;
            chkDirectOcr.Text = "Plakadan OCR";
            chkDirectOcr.UseVisualStyleBackColor = true;
            // 
            // cmbPlateModelType
            // 
            cmbPlateModelType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPlateModelType.FormattingEnabled = true;
            cmbPlateModelType.Items.AddRange(new object[] { "V1 - LicencePlateDetection_Gpu", "V2 - plateReconitionV2" });
            cmbPlateModelType.Location = new Point(271, 27);
            cmbPlateModelType.Name = "cmbPlateModelType";
            cmbPlateModelType.Size = new Size(171, 28);
            cmbPlateModelType.TabIndex = 30;
            cmbPlateModelType.SelectedIndexChanged += cmbPlateModelType_SelectedIndexChanged;
            // 
            // cbOcrModel
            // 
            cbOcrModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cbOcrModel.FormattingEnabled = true;
            cbOcrModel.Items.AddRange(new object[] { "Model S", "Titan V8", "Parseq" });
            cbOcrModel.Location = new Point(271, 65);
            cbOcrModel.Name = "cbOcrModel";
            cbOcrModel.Size = new Size(171, 28);
            cbOcrModel.TabIndex = 31;
            // 
            // btnImageLabeling
            // 
            btnImageLabeling.Location = new Point(1166, 99);
            btnImageLabeling.Name = "btnImageLabeling";
            btnImageLabeling.Size = new Size(171, 29);
            btnImageLabeling.TabIndex = 32;
            btnImageLabeling.Text = "Resim Etiketleme";
            btnImageLabeling.UseVisualStyleBackColor = true;
            btnImageLabeling.Click += btnImageLabeling_Click;
            // 
            // chkMultiModel
            // 
            chkMultiModel.Location = new Point(1166, 64);
            chkMultiModel.Name = "chkMultiModel";
            chkMultiModel.Size = new Size(171, 32);
            chkMultiModel.TabIndex = 33;
            chkMultiModel.Text = "2 Model Kullan";
            chkMultiModel.UseVisualStyleBackColor = true;
            // 
            // btnPause
            // 
            btnPause.Location = new Point(167, 65);
            btnPause.Margin = new Padding(3, 4, 3, 4);
            btnPause.Name = "btnPause";
            btnPause.Size = new Size(98, 30);
            btnPause.TabIndex = 34;
            btnPause.Text = "Duraklat";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            // 
            // frmALPR
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1360, 700);
            Controls.Add(btnPause);
            Controls.Add(chkMultiModel);
            Controls.Add(btnImageLabeling);
            Controls.Add(cbOcrModel);
            Controls.Add(chkDirectOcr);
            Controls.Add(cmbPlateModelType);
            Controls.Add(btnBatchProcess);
            Controls.Add(chkDebugMode);
            Controls.Add(lblFps);
            Controls.Add(lblCurrentModel);
            Controls.Add(chkUseGpu);
            Controls.Add(chkSavePlates);
            Controls.Add(txtLog);
            Controls.Add(lblLog);
            Controls.Add(pictureBoxImage);
            Controls.Add(nudCharConfidence);
            Controls.Add(lblCharConfidence);
            Controls.Add(nudFrameSkip);
            Controls.Add(lblFrameSkip);
            Controls.Add(nudConfidenceThreshold);
            Controls.Add(lblConfidenceThreshold);
            Controls.Add(nudNMSThreshold);
            Controls.Add(lblNMSThreshold);
            Controls.Add(chkEnableNMS);
            Controls.Add(btnStopVideo);
            Controls.Add(btnStartVideo);
            Controls.Add(btnSelectVideo);
            Controls.Add(btnSelectImage);
            Margin = new Padding(3, 4, 3, 4);
            Name = "frmALPR";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ALPR - Resim & Video İşleme (🐞 Debug Mode)";
            WindowState = FormWindowState.Maximized;
            FormClosing += frmALPR_FormClosing;
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudNMSThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudConfidenceThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudFrameSkip).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudCharConfidence).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Button btnSelectImage;
        private Button btnSelectVideo;
        private Button btnStartVideo;
        private Button btnStopVideo;
        private Button btnBatchProcess; // YENİ buton
        private Label lblCurrentModel;
        private PictureBox pictureBoxImage;
        private RichTextBox txtLog;
        private Label lblLog;
        private CheckBox chkEnableNMS;
        private CheckBox chkSavePlates;
        private CheckBox chkUseGpu;
        private CheckBox chkDebugMode;
        private NumericUpDown nudNMSThreshold;
        private Label lblNMSThreshold;
        private NumericUpDown nudConfidenceThreshold;
        private Label lblConfidenceThreshold;
        private Label lblFps;
        private NumericUpDown nudFrameSkip;
        private Label lblFrameSkip;
        private NumericUpDown nudCharConfidence;
        private Label lblCharConfidence;
        private CheckBox chkDirectOcr;
        private ComboBox cmbPlateModelType;
        private ComboBox cbOcrModel;
        private Button btnImageLabeling;
        private CheckBox chkMultiModel;
        private Button btnPause;
    }
}
