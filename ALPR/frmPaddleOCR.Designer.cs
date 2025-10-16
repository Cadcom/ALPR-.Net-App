namespace ALPR
{
    partial class frmPaddleOCR
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

        private void InitializeComponent()
        {
            this.groupBoxModels = new GroupBox();
            this.btnAnalyzeDetModel = new Button();
            this.btnAnalyzeRecModel = new Button();
            this.btnSelectRecModel = new Button();
            this.btnSelectDetModel = new Button();
            this.lblRecModel = new Label();
            this.lblDetModel = new Label();
            this.chkUseRecModel = new CheckBox();
            this.groupBoxImage = new GroupBox();
            this.btnSelectImage = new Button();
            this.pictureBoxImage = new PictureBox();
            this.groupBoxSettings = new GroupBox();
            this.nudDetThreshold = new NumericUpDown();
            this.lblDetThreshold = new Label();
            this.chkUseGpu = new CheckBox();
            this.btnProcess = new Button();
            this.groupBoxResults = new GroupBox();
            this.txtResults = new TextBox();
            this.lblProcessingTime = new Label();
            this.lblDetectedRegions = new Label();
            this.lblRecognizedText = new Label();
            this.statusStrip = new StatusStrip();
            this.lblStatus = new ToolStripStatusLabel();
            
            this.groupBoxModels.SuspendLayout();
            this.groupBoxImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).BeginInit();
            this.groupBoxSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDetThreshold)).BeginInit();
            this.groupBoxResults.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // 
            // groupBoxModels
            // 
            this.groupBoxModels.Controls.Add(this.btnAnalyzeDetModel);
            this.groupBoxModels.Controls.Add(this.btnAnalyzeRecModel);
            this.groupBoxModels.Controls.Add(this.btnSelectRecModel);
            this.groupBoxModels.Controls.Add(this.btnSelectDetModel);
            this.groupBoxModels.Controls.Add(this.lblRecModel);
            this.groupBoxModels.Controls.Add(this.lblDetModel);
            this.groupBoxModels.Controls.Add(this.chkUseRecModel);
            this.groupBoxModels.Location = new Point(12, 12);
            this.groupBoxModels.Name = "groupBoxModels";
            this.groupBoxModels.Size = new Size(450, 120);
            this.groupBoxModels.TabIndex = 0;
            this.groupBoxModels.TabStop = false;
            this.groupBoxModels.Text = "PaddleOCR Modelleri";

            // 
            // btnSelectDetModel
            // 
            this.btnSelectDetModel.Location = new Point(280, 25);
            this.btnSelectDetModel.Name = "btnSelectDetModel";
            this.btnSelectDetModel.Size = new Size(60, 23);
            this.btnSelectDetModel.TabIndex = 0;
            this.btnSelectDetModel.Text = "Seç...";
            this.btnSelectDetModel.UseVisualStyleBackColor = true;
            this.btnSelectDetModel.Click += new EventHandler(this.btnSelectDetModel_Click);

            // 
            // btnAnalyzeDetModel
            // 
            this.btnAnalyzeDetModel.Location = new Point(350, 25);
            this.btnAnalyzeDetModel.Name = "btnAnalyzeDetModel";
            this.btnAnalyzeDetModel.Size = new Size(70, 23);
            this.btnAnalyzeDetModel.TabIndex = 5;
            this.btnAnalyzeDetModel.Text = "?? Analiz";
            this.btnAnalyzeDetModel.UseVisualStyleBackColor = true;
            this.btnAnalyzeDetModel.Click += new EventHandler(this.btnAnalyzeDetModel_Click);

            // 
            // lblDetModel
            // 
            this.lblDetModel.AutoSize = true;
            this.lblDetModel.Location = new Point(6, 29);
            this.lblDetModel.Name = "lblDetModel";
            this.lblDetModel.Size = new Size(200, 15);
            this.lblDetModel.TabIndex = 1;
            this.lblDetModel.Text = "Detection Model: Seçilmedi";
            this.lblDetModel.ForeColor = Color.DarkRed;

            // 
            // chkUseRecModel
            // 
            this.chkUseRecModel.AutoSize = true;
            this.chkUseRecModel.Location = new Point(6, 55);
            this.chkUseRecModel.Name = "chkUseRecModel";
            this.chkUseRecModel.Size = new Size(200, 19);
            this.chkUseRecModel.TabIndex = 2;
            this.chkUseRecModel.Text = "Recognition Modeli Kullan";
            this.chkUseRecModel.UseVisualStyleBackColor = true;
            this.chkUseRecModel.CheckedChanged += new EventHandler(this.chkUseRecModel_CheckedChanged);

            // 
            // btnSelectRecModel
            // 
            this.btnSelectRecModel.Location = new Point(280, 80);
            this.btnSelectRecModel.Name = "btnSelectRecModel";
            this.btnSelectRecModel.Size = new Size(60, 23);
            this.btnSelectRecModel.TabIndex = 3;
            this.btnSelectRecModel.Text = "Seç...";
            this.btnSelectRecModel.UseVisualStyleBackColor = true;
            this.btnSelectRecModel.Click += new EventHandler(this.btnSelectRecModel_Click);

            // 
            // btnAnalyzeRecModel
            // 
            this.btnAnalyzeRecModel.Location = new Point(350, 80);
            this.btnAnalyzeRecModel.Name = "btnAnalyzeRecModel";
            this.btnAnalyzeRecModel.Size = new Size(70, 23);
            this.btnAnalyzeRecModel.TabIndex = 6;
            this.btnAnalyzeRecModel.Text = "?? Analiz";
            this.btnAnalyzeRecModel.UseVisualStyleBackColor = true;
            this.btnAnalyzeRecModel.Click += new EventHandler(this.btnAnalyzeRecModel_Click);

            // 
            // lblRecModel
            // 
            this.lblRecModel.AutoSize = true;
            this.lblRecModel.Location = new Point(6, 84);
            this.lblRecModel.Name = "lblRecModel";
            this.lblRecModel.Size = new Size(200, 15);
            this.lblRecModel.TabIndex = 4;
            this.lblRecModel.Text = "Recognition Model: Seçilmedi";
            this.lblRecModel.ForeColor = Color.DarkRed;

            // 
            // groupBoxImage
            // 
            this.groupBoxImage.Controls.Add(this.btnSelectImage);
            this.groupBoxImage.Controls.Add(this.pictureBoxImage);
            this.groupBoxImage.Location = new Point(480, 12);
            this.groupBoxImage.Name = "groupBoxImage";
            this.groupBoxImage.Size = new Size(400, 350);
            this.groupBoxImage.TabIndex = 1;
            this.groupBoxImage.TabStop = false;
            this.groupBoxImage.Text = "Test Resmi";

            // 
            // btnSelectImage
            // 
            this.btnSelectImage.Location = new Point(319, 22);
            this.btnSelectImage.Name = "btnSelectImage";
            this.btnSelectImage.Size = new Size(75, 23);
            this.btnSelectImage.TabIndex = 0;
            this.btnSelectImage.Text = "Resim Seç";
            this.btnSelectImage.UseVisualStyleBackColor = true;
            this.btnSelectImage.Click += new EventHandler(this.btnSelectImage_Click);

            // 
            // pictureBoxImage
            // 
            this.pictureBoxImage.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBoxImage.Location = new Point(6, 51);
            this.pictureBoxImage.Name = "pictureBoxImage";
            this.pictureBoxImage.Size = new Size(388, 290);
            this.pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBoxImage.TabIndex = 1;
            this.pictureBoxImage.TabStop = false;

            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Controls.Add(this.nudDetThreshold);
            this.groupBoxSettings.Controls.Add(this.lblDetThreshold);
            this.groupBoxSettings.Controls.Add(this.chkUseGpu);
            this.groupBoxSettings.Controls.Add(this.btnProcess);
            this.groupBoxSettings.Location = new Point(12, 150);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Size = new Size(450, 100);
            this.groupBoxSettings.TabIndex = 2;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "Ýþleme Ayarlarý";

            // 
            // nudDetThreshold
            // 
            this.nudDetThreshold.DecimalPlaces = 2;
            this.nudDetThreshold.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudDetThreshold.Location = new Point(150, 25);
            this.nudDetThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudDetThreshold.Name = "nudDetThreshold";
            this.nudDetThreshold.Size = new Size(80, 23);
            this.nudDetThreshold.TabIndex = 0;
            this.nudDetThreshold.Value = new decimal(new int[] { 3, 0, 0, 65536 });

            // 
            // lblDetThreshold
            // 
            this.lblDetThreshold.AutoSize = true;
            this.lblDetThreshold.Location = new Point(6, 27);
            this.lblDetThreshold.Name = "lblDetThreshold";
            this.lblDetThreshold.Size = new Size(138, 15);
            this.lblDetThreshold.TabIndex = 1;
            this.lblDetThreshold.Text = "Detection Threshold:";

            // 
            // chkUseGpu
            // 
            this.chkUseGpu.AutoSize = true;
            this.chkUseGpu.Location = new Point(6, 54);
            this.chkUseGpu.Name = "chkUseGpu";
            this.chkUseGpu.Size = new Size(91, 19);
            this.chkUseGpu.TabIndex = 2;
            this.chkUseGpu.Text = "GPU Kullan";
            this.chkUseGpu.UseVisualStyleBackColor = true;

            // 
            // btnProcess
            // 
            this.btnProcess.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnProcess.Location = new Point(330, 25);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new Size(100, 48);
            this.btnProcess.TabIndex = 3;
            this.btnProcess.Text = "?? Ýþle";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new EventHandler(this.btnProcess_Click);

            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Controls.Add(this.txtResults);
            this.groupBoxResults.Controls.Add(this.lblProcessingTime);
            this.groupBoxResults.Controls.Add(this.lblDetectedRegions);
            this.groupBoxResults.Controls.Add(this.lblRecognizedText);
            this.groupBoxResults.Location = new Point(12, 270);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new Size(450, 250);
            this.groupBoxResults.TabIndex = 3;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "Sonuçlar";

            // 
            // txtResults
            // 
            this.txtResults.Location = new Point(6, 92);
            this.txtResults.Multiline = true;
            this.txtResults.Name = "txtResults";
            this.txtResults.ReadOnly = true;
            this.txtResults.ScrollBars = ScrollBars.Vertical;
            this.txtResults.Size = new Size(438, 150);
            this.txtResults.TabIndex = 0;

            // 
            // lblProcessingTime
            // 
            this.lblProcessingTime.AutoSize = true;
            this.lblProcessingTime.Location = new Point(6, 25);
            this.lblProcessingTime.Name = "lblProcessingTime";
            this.lblProcessingTime.Size = new Size(100, 15);
            this.lblProcessingTime.TabIndex = 1;
            this.lblProcessingTime.Text = "Ýþleme Süresi: -";

            // 
            // lblDetectedRegions
            // 
            this.lblDetectedRegions.AutoSize = true;
            this.lblDetectedRegions.Location = new Point(6, 45);
            this.lblDetectedRegions.Name = "lblDetectedRegions";
            this.lblDetectedRegions.Size = new Size(120, 15);
            this.lblDetectedRegions.TabIndex = 2;
            this.lblDetectedRegions.Text = "Tespit Edilen: -";

            // 
            // lblRecognizedText
            // 
            this.lblRecognizedText.AutoSize = true;
            this.lblRecognizedText.Location = new Point(6, 65);
            this.lblRecognizedText.Name = "lblRecognizedText";
            this.lblRecognizedText.Size = new Size(100, 15);
            this.lblRecognizedText.TabIndex = 3;
            this.lblRecognizedText.Text = "Tanýnan Metin:";

            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus });
            this.statusStrip.Location = new Point(0, 540);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(904, 22);
            this.statusStrip.TabIndex = 4;
            this.statusStrip.Text = "statusStrip1";

            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(200, 17);
            this.lblStatus.Text = "PaddleOCR hazýr - Model seçin";

            // 
            // frmPaddleOCR
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(904, 562);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.groupBoxResults);
            this.Controls.Add(this.groupBoxSettings);
            this.Controls.Add(this.groupBoxImage);
            this.Controls.Add(this.groupBoxModels);
            this.Name = "frmPaddleOCR";
            this.Text = "?? PaddleOCR Plaka Tanýma";
            this.FormClosing += new FormClosingEventHandler(this.frmPaddleOCR_FormClosing);
            
            this.groupBoxModels.ResumeLayout(false);
            this.groupBoxModels.PerformLayout();
            this.groupBoxImage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).EndInit();
            this.groupBoxSettings.ResumeLayout(false);
            this.groupBoxSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDetThreshold)).EndInit();
            this.groupBoxResults.ResumeLayout(false);
            this.groupBoxResults.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private GroupBox groupBoxModels;
        private Button btnAnalyzeDetModel;
        private Button btnAnalyzeRecModel;
        private Button btnSelectRecModel;
        private Button btnSelectDetModel;
        private Label lblRecModel;
        private Label lblDetModel;
        private CheckBox chkUseRecModel;
        private GroupBox groupBoxImage;
        private Button btnSelectImage;
        private PictureBox pictureBoxImage;
        private GroupBox groupBoxSettings;
        private NumericUpDown nudDetThreshold;
        private Label lblDetThreshold;
        private CheckBox chkUseGpu;
        private Button btnProcess;
        private GroupBox groupBoxResults;
        private TextBox txtResults;
        private Label lblProcessingTime;
        private Label lblDetectedRegions;
        private Label lblRecognizedText;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
    }
}