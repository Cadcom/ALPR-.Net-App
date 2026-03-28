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
            txtLog = new RichTextBox();
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
            chkPlakaOku = new CheckBox();
            chkDirectOcr = new CheckBox();
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
            btnStartVideo.Size = new Size(98, 47);
            btnStartVideo.TabIndex = 2;
            btnStartVideo.Text = "Başlat";
            btnStartVideo.UseVisualStyleBackColor = true;
            btnStartVideo.Click += btnStartVideo_Click;
            // 
            // btnStopVideo
            // 
            btnStopVideo.Enabled = false;
            btnStopVideo.Location = new Point(167, 77);
            btnStopVideo.Margin = new Padding(3, 4, 3, 4);
            btnStopVideo.Name = "btnStopVideo";
            btnStopVideo.Size = new Size(98, 47);
            btnStopVideo.TabIndex = 3;
            btnStopVideo.Text = "Durdur";
            btnStopVideo.UseVisualStyleBackColor = true;
            btnStopVideo.Click += btnStopVideo_Click;
            // 
            // btnModelComparison
            // 
            btnModelComparison.BackColor = Color.LightBlue;
            btnModelComparison.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnModelComparison.Location = new Point(272, 27);
            btnModelComparison.Margin = new Padding(3, 4, 3, 4);
            btnModelComparison.Name = "btnModelComparison";
            btnModelComparison.Size = new Size(171, 36);
            btnModelComparison.TabIndex = 20;
            btnModelComparison.Text = "Model Karşılaştır";
            btnModelComparison.UseVisualStyleBackColor = false;
            btnModelComparison.Click += btnModelComparison_Click;
            // 
            // btnPaddleOCR
            // 
            btnPaddleOCR.BackColor = Color.LightGreen;
            btnPaddleOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnPaddleOCR.Location = new Point(272, 108);
            btnPaddleOCR.Margin = new Padding(3, 4, 3, 4);
            btnPaddleOCR.Name = "btnPaddleOCR";
            btnPaddleOCR.Size = new Size(86, 31);
            btnPaddleOCR.TabIndex = 23;
            btnPaddleOCR.Text = "📝 PaddleOCR";
            btnPaddleOCR.UseVisualStyleBackColor = false;
            btnPaddleOCR.Click += btnPaddleOCR_Click;
            // 
            // btnTesseractOCR
            // 
            btnTesseractOCR.BackColor = Color.LightCyan;
            btnTesseractOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnTesseractOCR.Location = new Point(363, 108);
            btnTesseractOCR.Margin = new Padding(3, 4, 3, 4);
            btnTesseractOCR.Name = "btnTesseractOCR";
            btnTesseractOCR.Size = new Size(86, 31);
            btnTesseractOCR.TabIndex = 26;
            btnTesseractOCR.Text = "📝 Tesseract";
            btnTesseractOCR.UseVisualStyleBackColor = false;
            btnTesseractOCR.Click += btnTesseractOCR_Click;
            // 
            // btnSelectPlateModel
            // 
            btnSelectPlateModel.BackColor = Color.LightCoral;
            btnSelectPlateModel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSelectPlateModel.Location = new Point(272, 69);
            btnSelectPlateModel.Margin = new Padding(3, 4, 3, 4);
            btnSelectPlateModel.Name = "btnSelectPlateModel";
            btnSelectPlateModel.Size = new Size(171, 31);
            btnSelectPlateModel.TabIndex = 21;
            btnSelectPlateModel.Text = "Plaka Modeli Seç";
            btnSelectPlateModel.UseVisualStyleBackColor = false;
            btnSelectPlateModel.Click += btnSelectPlateModel_Click;
            // 
            // btnBatchProcess
            // 
            btnBatchProcess.BackColor = Color.Gold;
            btnBatchProcess.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBatchProcess.Location = new Point(1166, 27);
            btnBatchProcess.Margin = new Padding(3, 4, 3, 4);
            btnBatchProcess.Name = "btnBatchProcess";
            btnBatchProcess.Size = new Size(171, 47);
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
            lblCurrentModel.Location = new Point(842, 111);
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
            chkEnableNMS.Location = new Point(472, 112);
            chkEnableNMS.Margin = new Padding(3, 4, 3, 4);
            chkEnableNMS.Name = "chkEnableNMS";
            chkEnableNMS.Size = new Size(99, 24);
            chkEnableNMS.TabIndex = 4;
            chkEnableNMS.Text = "NMS Etkin";
            chkEnableNMS.UseVisualStyleBackColor = true;
            // 
            // chkShowCharBoxes
            // 
            chkShowCharBoxes.AutoSize = true;
            chkShowCharBoxes.Location = new Point(842, 75);
            chkShowCharBoxes.Margin = new Padding(3, 4, 3, 4);
            chkShowCharBoxes.Name = "chkShowCharBoxes";
            chkShowCharBoxes.Size = new Size(141, 24);
            chkShowCharBoxes.TabIndex = 5;
            chkShowCharBoxes.Text = "Karakter Kutuları";
            chkShowCharBoxes.UseVisualStyleBackColor = true;
            // 
            // chkSavePlates
            // 
            chkSavePlates.AutoSize = true;
            chkSavePlates.Location = new Point(472, 33);
            chkSavePlates.Margin = new Padding(3, 4, 3, 4);
            chkSavePlates.Name = "chkSavePlates";
            chkSavePlates.Size = new Size(137, 24);
            chkSavePlates.TabIndex = 18;
            chkSavePlates.Text = "Plakaları Kaydet";
            chkSavePlates.UseVisualStyleBackColor = true;
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Checked = true;
            chkUseGpu.CheckState = CheckState.Checked;
            chkUseGpu.Location = new Point(472, 73);
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
            chkDebugMode.Location = new Point(1026, 33);
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
            nudConfidenceThreshold.Value = new decimal(new int[] { 6, 0, 0, 65536 });
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
            // btnFastOCR
            // 
            btnFastOCR.BackColor = Color.LightCyan;
            btnFastOCR.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnFastOCR.Location = new Point(1166, 93);
            btnFastOCR.Margin = new Padding(3, 4, 3, 4);
            btnFastOCR.Name = "btnFastOCR";
            btnFastOCR.Size = new Size(86, 31);
            btnFastOCR.TabIndex = 27;
            btnFastOCR.Text = "FastOCR";
            btnFastOCR.UseVisualStyleBackColor = false;
            btnFastOCR.Click += btnFastOCR_Click;
            // 
            // chkPlakaOku
            // 
            chkPlakaOku.AutoSize = true;
            chkPlakaOku.Location = new Point(1166, 69);
            chkPlakaOku.Name = "chkPlakaOku";
            chkPlakaOku.Size = new Size(117, 24);
            chkPlakaOku.TabIndex = 28;
            chkPlakaOku.Text = "Plakaları Oku";
            chkPlakaOku.UseVisualStyleBackColor = true;
            // 
            // chkDirectOcr
            // 
            chkDirectOcr.AutoSize = true;
            chkDirectOcr.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chkDirectOcr.ForeColor = Color.DarkGreen;
            chkDirectOcr.Location = new Point(1026, 73);
            chkDirectOcr.Name = "chkDirectOcr";
            chkDirectOcr.Size = new Size(135, 24);
            chkDirectOcr.TabIndex = 29;
            chkDirectOcr.Text = "Doğrudan OCR";
            chkDirectOcr.UseVisualStyleBackColor = true;
            // 
            // frmALPR
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1360, 700);
            Controls.Add(chkDirectOcr);
            Controls.Add(chkPlakaOku);
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
        private Button btnModelComparison;
        private Button btnPaddleOCR;
        private Button btnTesseractOCR;
        private Button btnSelectPlateModel;
        private Button btnBatchProcess; // YENİ buton
        private Label lblCurrentModel;
        private PictureBox pictureBoxImage;
        private RichTextBox txtLog;
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
        private CheckBox chkPlakaOku;
        private CheckBox chkDirectOcr;
    }
}
