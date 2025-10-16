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
            btnModelComparison = new Button();
            btnPaddleOCR = new Button();
            btnTesseractOCR = new Button();
            btnSelectPlateModel = new Button();
            btnBatchProcess = new Button();
            lblCurrentModel = new Label();
            pictureBoxImage = new PictureBox();
            txtLog = new TextBox();
            lblLog = new Label();
            chkEnableNMS = new CheckBox();
            chkShowCharBoxes = new CheckBox();
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
            btnFastOCR = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudNMSThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudConfidenceThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFrameSkip).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCharConfidence).BeginInit();
            SuspendLayout();
            // 
            // btnSelectImage
            // 
            btnSelectImage.Location = new Point(20, 20);
            btnSelectImage.Name = "btnSelectImage";
            btnSelectImage.Size = new Size(120, 35);
            btnSelectImage.TabIndex = 0;
            btnSelectImage.Text = "Resim Seç";
            btnSelectImage.UseVisualStyleBackColor = true;
            btnSelectImage.Click += btnSelectImage_Click;
            // 
            // btnSelectVideo
            // 
            btnSelectVideo.Location = new Point(20, 58);
            btnSelectVideo.Name = "btnSelectVideo";
            btnSelectVideo.Size = new Size(120, 35);
            btnSelectVideo.TabIndex = 1;
            btnSelectVideo.Text = "Video Seç";
            btnSelectVideo.UseVisualStyleBackColor = true;
            btnSelectVideo.Click += btnSelectVideo_Click;
            // 
            // btnStartVideo
            // 
            btnStartVideo.Enabled = false;
            btnStartVideo.Location = new Point(146, 20);
            btnStartVideo.Name = "btnStartVideo";
            btnStartVideo.Size = new Size(86, 35);
            btnStartVideo.TabIndex = 2;
            btnStartVideo.Text = "Baþlat";
            btnStartVideo.UseVisualStyleBackColor = true;
            btnStartVideo.Click += btnStartVideo_Click;
            // 
            // btnStopVideo
            // 
            btnStopVideo.Enabled = false;
            btnStopVideo.Location = new Point(146, 58);
            btnStopVideo.Name = "btnStopVideo";
            btnStopVideo.Size = new Size(86, 35);
            btnStopVideo.TabIndex = 3;
            btnStopVideo.Text = "Durdur";
            btnStopVideo.UseVisualStyleBackColor = true;
            btnStopVideo.Click += btnStopVideo_Click;
            // 
            // btnModelComparison
            // 
            btnModelComparison.BackColor = Color.LightBlue;
            btnModelComparison.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnModelComparison.Location = new Point(238, 20);
            btnModelComparison.Name = "btnModelComparison";
            btnModelComparison.Size = new Size(150, 27);
            btnModelComparison.TabIndex = 20;
            btnModelComparison.Text = "Model Karþýlaþtýr";
            btnModelComparison.UseVisualStyleBackColor = false;
            btnModelComparison.Click += btnModelComparison_Click;
            // 
            // btnPaddleOCR
            // 
            btnPaddleOCR.BackColor = Color.LightGreen;
            btnPaddleOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnPaddleOCR.Location = new Point(238, 81);
            btnPaddleOCR.Name = "btnPaddleOCR";
            btnPaddleOCR.Size = new Size(75, 23);
            btnPaddleOCR.TabIndex = 23;
            btnPaddleOCR.Text = "?? PaddleOCR";
            btnPaddleOCR.UseVisualStyleBackColor = false;
            btnPaddleOCR.Click += btnPaddleOCR_Click;
            // 
            // btnTesseractOCR
            // 
            btnTesseractOCR.BackColor = Color.LightCyan;
            btnTesseractOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnTesseractOCR.Location = new Point(318, 81);
            btnTesseractOCR.Name = "btnTesseractOCR";
            btnTesseractOCR.Size = new Size(75, 23);
            btnTesseractOCR.TabIndex = 26;
            btnTesseractOCR.Text = "?? Tesseract";
            btnTesseractOCR.UseVisualStyleBackColor = false;
            btnTesseractOCR.Click += btnTesseractOCR_Click;
            // 
            // btnSelectPlateModel
            // 
            btnSelectPlateModel.BackColor = Color.LightCoral;
            btnSelectPlateModel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSelectPlateModel.Location = new Point(238, 52);
            btnSelectPlateModel.Name = "btnSelectPlateModel";
            btnSelectPlateModel.Size = new Size(150, 23);
            btnSelectPlateModel.TabIndex = 21;
            btnSelectPlateModel.Text = "Plaka Modeli Seç";
            btnSelectPlateModel.UseVisualStyleBackColor = false;
            btnSelectPlateModel.Click += btnSelectPlateModel_Click;
            // 
            // btnBatchProcess
            // 
            btnBatchProcess.BackColor = Color.Gold;
            btnBatchProcess.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBatchProcess.Location = new Point(1020, 20);
            btnBatchProcess.Name = "btnBatchProcess";
            btnBatchProcess.Size = new Size(150, 35);
            btnBatchProcess.TabIndex = 25;
            btnBatchProcess.Text = "?? Toplu Ýþle";
            btnBatchProcess.UseVisualStyleBackColor = false;
            btnBatchProcess.Click += btnBatchProcess_Click;
            // 
            // lblCurrentModel
            // 
            lblCurrentModel.AutoSize = true;
            lblCurrentModel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblCurrentModel.ForeColor = Color.DarkBlue;
            lblCurrentModel.Location = new Point(737, 83);
            lblCurrentModel.Name = "lblCurrentModel";
            lblCurrentModel.Size = new Size(99, 13);
            lblCurrentModel.TabIndex = 22;
            lblCurrentModel.Text = "Model: Varsayýlan";
            // 
            // pictureBoxImage
            // 
            pictureBoxImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBoxImage.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxImage.Location = new Point(20, 110);
            pictureBoxImage.Name = "pictureBoxImage";
            pictureBoxImage.Size = new Size(1150, 245);
            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.TabIndex = 15;
            pictureBoxImage.TabStop = false;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.Black;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ForeColor = Color.Lime;
            txtLog.Location = new Point(20, 385);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1150, 120);
            txtLog.TabIndex = 17;
            txtLog.Text = "Log kutusu hazýr...";
            // 
            // lblLog
            // 
            lblLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLog.AutoSize = true;
            lblLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLog.Location = new Point(20, 365);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(88, 15);
            lblLog.TabIndex = 16;
            lblLog.Text = "Tespit Bilgileri:";
            // 
            // chkEnableNMS
            // 
            chkEnableNMS.AutoSize = true;
            chkEnableNMS.Checked = true;
            chkEnableNMS.CheckState = CheckState.Checked;
            chkEnableNMS.Location = new Point(413, 84);
            chkEnableNMS.Name = "chkEnableNMS";
            chkEnableNMS.Size = new Size(81, 19);
            chkEnableNMS.TabIndex = 4;
            chkEnableNMS.Text = "NMS Etkin";
            chkEnableNMS.UseVisualStyleBackColor = true;
            // 
            // chkShowCharBoxes
            // 
            chkShowCharBoxes.AutoSize = true;
            chkShowCharBoxes.Location = new Point(737, 56);
            chkShowCharBoxes.Name = "chkShowCharBoxes";
            chkShowCharBoxes.Size = new Size(113, 19);
            chkShowCharBoxes.TabIndex = 5;
            chkShowCharBoxes.Text = "Karakter Kutularý";
            chkShowCharBoxes.UseVisualStyleBackColor = true;
            // 
            // chkSavePlates
            // 
            chkSavePlates.AutoSize = true;
            chkSavePlates.Location = new Point(413, 25);
            chkSavePlates.Name = "chkSavePlates";
            chkSavePlates.Size = new Size(109, 19);
            chkSavePlates.TabIndex = 18;
            chkSavePlates.Text = "Plakalarý Kaydet";
            chkSavePlates.UseVisualStyleBackColor = true;
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Enabled = false;
            chkUseGpu.Location = new Point(413, 55);
            chkUseGpu.Name = "chkUseGpu";
            chkUseGpu.Size = new Size(85, 19);
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
            chkDebugMode.Location = new Point(898, 25);
            chkDebugMode.Name = "chkDebugMode";
            chkDebugMode.Size = new Size(76, 17);
            chkDebugMode.TabIndex = 24;
            chkDebugMode.Text = "?? DEBUG";
            chkDebugMode.UseVisualStyleBackColor = true;
            // 
            // nudNMSThreshold
            // 
            nudNMSThreshold.DecimalPlaces = 2;
            nudNMSThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudNMSThreshold.Location = new Point(634, 79);
            nudNMSThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudNMSThreshold.Name = "nudNMSThreshold";
            nudNMSThreshold.Size = new Size(60, 23);
            nudNMSThreshold.TabIndex = 7;
            nudNMSThreshold.Value = new decimal(new int[] { 45, 0, 0, 131072 });
            // 
            // lblNMSThreshold
            // 
            lblNMSThreshold.AutoSize = true;
            lblNMSThreshold.Location = new Point(561, 81);
            lblNMSThreshold.Name = "lblNMSThreshold";
            lblNMSThreshold.Size = new Size(36, 15);
            lblNMSThreshold.TabIndex = 6;
            lblNMSThreshold.Text = "NMS:";
            // 
            // nudConfidenceThreshold
            // 
            nudConfidenceThreshold.DecimalPlaces = 2;
            nudConfidenceThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudConfidenceThreshold.Location = new Point(817, 24);
            nudConfidenceThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudConfidenceThreshold.Name = "nudConfidenceThreshold";
            nudConfidenceThreshold.Size = new Size(60, 23);
            nudConfidenceThreshold.TabIndex = 9;
            nudConfidenceThreshold.Value = new decimal(new int[] { 6, 0, 0, 65536 });
            // 
            // lblConfidenceThreshold
            // 
            lblConfidenceThreshold.AutoSize = true;
            lblConfidenceThreshold.Location = new Point(737, 26);
            lblConfidenceThreshold.Name = "lblConfidenceThreshold";
            lblConfidenceThreshold.Size = new Size(75, 15);
            lblConfidenceThreshold.TabIndex = 8;
            lblConfidenceThreshold.Text = "Plaka Güven:";
            // 
            // lblFps
            // 
            lblFps.AutoSize = true;
            lblFps.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFps.Location = new Point(20, 110);
            lblFps.Name = "lblFps";
            lblFps.Size = new Size(57, 15);
            lblFps.TabIndex = 14;
            lblFps.Text = "FPS: 0.00";
            // 
            // nudFrameSkip
            // 
            nudFrameSkip.Location = new Point(634, 23);
            nudFrameSkip.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudFrameSkip.Name = "nudFrameSkip";
            nudFrameSkip.Size = new Size(60, 23);
            nudFrameSkip.TabIndex = 11;
            nudFrameSkip.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblFrameSkip
            // 
            lblFrameSkip.AutoSize = true;
            lblFrameSkip.Location = new Point(561, 25);
            lblFrameSkip.Name = "lblFrameSkip";
            lblFrameSkip.Size = new Size(67, 15);
            lblFrameSkip.TabIndex = 10;
            lblFrameSkip.Text = "Frame Atla:";
            // 
            // nudCharConfidence
            // 
            nudCharConfidence.DecimalPlaces = 2;
            nudCharConfidence.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudCharConfidence.Location = new Point(634, 50);
            nudCharConfidence.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudCharConfidence.Name = "nudCharConfidence";
            nudCharConfidence.Size = new Size(60, 23);
            nudCharConfidence.TabIndex = 13;
            nudCharConfidence.Value = new decimal(new int[] { 3, 0, 0, 65536 });
            // 
            // lblCharConfidence
            // 
            lblCharConfidence.AutoSize = true;
            lblCharConfidence.Location = new Point(561, 55);
            lblCharConfidence.Name = "lblCharConfidence";
            lblCharConfidence.Size = new Size(67, 15);
            lblCharConfidence.TabIndex = 12;
            lblCharConfidence.Text = "Kar. Güven:";
            // 
            // btnFastOCR
            // 
            btnFastOCR.BackColor = Color.LightCyan;
            btnFastOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnFastOCR.Location = new Point(1020, 70);
            btnFastOCR.Name = "btnFastOCR";
            btnFastOCR.Size = new Size(75, 23);
            btnFastOCR.TabIndex = 27;
            btnFastOCR.Text = "FastOCR";
            btnFastOCR.UseVisualStyleBackColor = false;
            btnFastOCR.Click += btnFastOCR_Click;
            // 
            // frmALPR
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1190, 525);
            Controls.Add(btnFastOCR);
            Controls.Add(btnTesseractOCR);
            Controls.Add(btnBatchProcess);
            Controls.Add(chkDebugMode);
            Controls.Add(lblFps);
            Controls.Add(lblCurrentModel);
            Controls.Add(btnSelectPlateModel);
            Controls.Add(btnPaddleOCR);
            Controls.Add(btnModelComparison);
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
            Controls.Add(chkShowCharBoxes);
            Controls.Add(chkEnableNMS);
            Controls.Add(btnStopVideo);
            Controls.Add(btnStartVideo);
            Controls.Add(btnSelectVideo);
            Controls.Add(btnSelectImage);
            Name = "frmALPR";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ALPR - Resim & Video Ýþleme (?? Debug Mode)";
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
        private Button btnModelComparison;
        private Button btnPaddleOCR;
        private Button btnTesseractOCR;
        private Button btnSelectPlateModel;
        private Button btnBatchProcess; // YENÝ buton
        private Label lblCurrentModel;
        private PictureBox pictureBoxImage;
        private TextBox txtLog;
        private Label lblLog;
        private CheckBox chkEnableNMS;
        private CheckBox chkShowCharBoxes;
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
        private Button btnFastOCR;
    }
}