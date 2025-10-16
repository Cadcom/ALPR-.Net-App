namespace ALPR
{
    partial class frmFastOCR
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
            lblStatus = new ToolStripStatusLabel();
            txtResults = new TextBox();
            lblProcessingTime = new Label();
            lblDetectedRegions = new Label();
            lblRecognizedText = new Label();
            groupBoxResults = new GroupBox();
            lblTime = new Label();
            lblResult = new Label();
            nudDetThreshold = new NumericUpDown();
            lblDetThreshold = new Label();
            chkUseGpu = new CheckBox();
            btnProcess = new Button();
            statusStrip = new StatusStrip();
            groupBoxSettings = new GroupBox();
            btnSelectImage = new Button();
            groupBoxImage = new GroupBox();
            pictureBoxImage = new PictureBox();
            btnAnalyzeRecModel = new Button();
            btnSelectRecModel = new Button();
            lblRecModel = new Label();
            groupBoxModels = new GroupBox();
            groupBoxResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudDetThreshold).BeginInit();
            statusStrip.SuspendLayout();
            groupBoxSettings.SuspendLayout();
            groupBoxImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).BeginInit();
            groupBoxModels.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(182, 17);
            lblStatus.Text = "Tesseract OCR hazır - Resim seçin";
            // 
            // txtResults
            // 
            txtResults.Location = new Point(6, 92);
            txtResults.Multiline = true;
            txtResults.Name = "txtResults";
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Vertical;
            txtResults.Size = new Size(438, 150);
            txtResults.TabIndex = 0;
            // 
            // lblProcessingTime
            // 
            lblProcessingTime.AutoSize = true;
            lblProcessingTime.Location = new Point(6, 25);
            lblProcessingTime.Name = "lblProcessingTime";
            lblProcessingTime.Size = new Size(86, 15);
            lblProcessingTime.TabIndex = 1;
            lblProcessingTime.Text = "İşleme Süresi: -";
            // 
            // lblDetectedRegions
            // 
            lblDetectedRegions.AutoSize = true;
            lblDetectedRegions.Location = new Point(6, 45);
            lblDetectedRegions.Name = "lblDetectedRegions";
            lblDetectedRegions.Size = new Size(83, 15);
            lblDetectedRegions.TabIndex = 2;
            lblDetectedRegions.Text = "Tespit Edilen: -";
            // 
            // lblRecognizedText
            // 
            lblRecognizedText.AutoSize = true;
            lblRecognizedText.Location = new Point(6, 65);
            lblRecognizedText.Name = "lblRecognizedText";
            lblRecognizedText.Size = new Size(85, 15);
            lblRecognizedText.TabIndex = 3;
            lblRecognizedText.Text = "Tanınan Metin:";
            // 
            // groupBoxResults
            // 
            groupBoxResults.Controls.Add(lblTime);
            groupBoxResults.Controls.Add(lblResult);
            groupBoxResults.Controls.Add(txtResults);
            groupBoxResults.Controls.Add(lblProcessingTime);
            groupBoxResults.Controls.Add(lblDetectedRegions);
            groupBoxResults.Controls.Add(lblRecognizedText);
            groupBoxResults.Location = new Point(12, 264);
            groupBoxResults.Name = "groupBoxResults";
            groupBoxResults.Size = new Size(450, 250);
            groupBoxResults.TabIndex = 8;
            groupBoxResults.TabStop = false;
            groupBoxResults.Text = "Sonuçlar";
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Location = new Point(146, 45);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(38, 15);
            lblTime.TabIndex = 5;
            lblTime.Text = "label1";
            // 
            // lblResult
            // 
            lblResult.AutoSize = true;
            lblResult.Location = new Point(146, 23);
            lblResult.Name = "lblResult";
            lblResult.Size = new Size(38, 15);
            lblResult.TabIndex = 4;
            lblResult.Text = "label1";
            // 
            // nudDetThreshold
            // 
            nudDetThreshold.DecimalPlaces = 2;
            nudDetThreshold.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudDetThreshold.Location = new Point(150, 25);
            nudDetThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudDetThreshold.Name = "nudDetThreshold";
            nudDetThreshold.Size = new Size(80, 23);
            nudDetThreshold.TabIndex = 0;
            nudDetThreshold.Value = new decimal(new int[] { 3, 0, 0, 65536 });
            // 
            // lblDetThreshold
            // 
            lblDetThreshold.AutoSize = true;
            lblDetThreshold.Location = new Point(6, 27);
            lblDetThreshold.Name = "lblDetThreshold";
            lblDetThreshold.Size = new Size(116, 15);
            lblDetThreshold.TabIndex = 1;
            lblDetThreshold.Text = "Detection Threshold:";
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Location = new Point(6, 54);
            chkUseGpu.Name = "chkUseGpu";
            chkUseGpu.Size = new Size(85, 19);
            chkUseGpu.TabIndex = 2;
            chkUseGpu.Text = "GPU Kullan";
            chkUseGpu.UseVisualStyleBackColor = true;
            // 
            // btnProcess
            // 
            btnProcess.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnProcess.Location = new Point(330, 25);
            btnProcess.Name = "btnProcess";
            btnProcess.Size = new Size(100, 48);
            btnProcess.TabIndex = 3;
            btnProcess.Text = "?? İşle";
            btnProcess.UseVisualStyleBackColor = true;
            btnProcess.Click += btnProcess_Click;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 551);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1059, 22);
            statusStrip.TabIndex = 9;
            statusStrip.Text = "statusStrip1";
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Controls.Add(nudDetThreshold);
            groupBoxSettings.Controls.Add(lblDetThreshold);
            groupBoxSettings.Controls.Add(chkUseGpu);
            groupBoxSettings.Controls.Add(btnProcess);
            groupBoxSettings.Location = new Point(12, 144);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Size = new Size(450, 100);
            groupBoxSettings.TabIndex = 7;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "İşleme Ayarları";
            // 
            // btnSelectImage
            // 
            btnSelectImage.Location = new Point(319, 22);
            btnSelectImage.Name = "btnSelectImage";
            btnSelectImage.Size = new Size(75, 23);
            btnSelectImage.TabIndex = 0;
            btnSelectImage.Text = "Resim Seç";
            btnSelectImage.UseVisualStyleBackColor = true;
            btnSelectImage.Click += btnSelectImage_Click;
            // 
            // groupBoxImage
            // 
            groupBoxImage.Controls.Add(btnSelectImage);
            groupBoxImage.Controls.Add(pictureBoxImage);
            groupBoxImage.Location = new Point(480, 6);
            groupBoxImage.Name = "groupBoxImage";
            groupBoxImage.Size = new Size(400, 350);
            groupBoxImage.TabIndex = 6;
            groupBoxImage.TabStop = false;
            groupBoxImage.Text = "Test Resmi";
            // 
            // pictureBoxImage
            // 
            pictureBoxImage.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxImage.Location = new Point(6, 51);
            pictureBoxImage.Name = "pictureBoxImage";
            pictureBoxImage.Size = new Size(388, 290);
            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.TabIndex = 1;
            pictureBoxImage.TabStop = false;
            // 
            // btnAnalyzeRecModel
            // 
            btnAnalyzeRecModel.Location = new Point(348, 22);
            btnAnalyzeRecModel.Name = "btnAnalyzeRecModel";
            btnAnalyzeRecModel.Size = new Size(70, 23);
            btnAnalyzeRecModel.TabIndex = 6;
            btnAnalyzeRecModel.Text = "?? Analiz";
            btnAnalyzeRecModel.UseVisualStyleBackColor = true;
            // 
            // btnSelectRecModel
            // 
            btnSelectRecModel.Location = new Point(278, 22);
            btnSelectRecModel.Name = "btnSelectRecModel";
            btnSelectRecModel.Size = new Size(60, 23);
            btnSelectRecModel.TabIndex = 3;
            btnSelectRecModel.Text = "Seç...";
            btnSelectRecModel.UseVisualStyleBackColor = true;
            // 
            // lblRecModel
            // 
            lblRecModel.AutoSize = true;
            lblRecModel.ForeColor = Color.DarkRed;
            lblRecModel.Location = new Point(4, 26);
            lblRecModel.Name = "lblRecModel";
            lblRecModel.Size = new Size(165, 15);
            lblRecModel.TabIndex = 4;
            lblRecModel.Text = "Recognition Model: Seçilmedi";
            // 
            // groupBoxModels
            // 
            groupBoxModels.Controls.Add(btnAnalyzeRecModel);
            groupBoxModels.Controls.Add(btnSelectRecModel);
            groupBoxModels.Controls.Add(lblRecModel);
            groupBoxModels.Location = new Point(12, 6);
            groupBoxModels.Name = "groupBoxModels";
            groupBoxModels.Size = new Size(450, 120);
            groupBoxModels.TabIndex = 5;
            groupBoxModels.TabStop = false;
            groupBoxModels.Text = "Tesseract OCR Modelleri";
            // 
            // frmFastOCR
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1059, 573);
            Controls.Add(groupBoxResults);
            Controls.Add(statusStrip);
            Controls.Add(groupBoxSettings);
            Controls.Add(groupBoxImage);
            Controls.Add(groupBoxModels);
            Name = "frmFastOCR";
            Text = "frmFastOCR";
            groupBoxResults.ResumeLayout(false);
            groupBoxResults.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudDetThreshold).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            groupBoxSettings.ResumeLayout(false);
            groupBoxSettings.PerformLayout();
            groupBoxImage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxImage).EndInit();
            groupBoxModels.ResumeLayout(false);
            groupBoxModels.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStripStatusLabel lblStatus;
        private TextBox txtResults;
        private Label lblProcessingTime;
        private Label lblDetectedRegions;
        private Label lblRecognizedText;
        private GroupBox groupBoxResults;
        private NumericUpDown nudDetThreshold;
        private Label lblDetThreshold;
        private CheckBox chkUseGpu;
        private Button btnProcess;
        private StatusStrip statusStrip;
        private GroupBox groupBoxSettings;
        private Button btnSelectImage;
        private GroupBox groupBoxImage;
        private PictureBox pictureBoxImage;
        private Button btnAnalyzeRecModel;
        private Button btnSelectRecModel;
        private Label lblRecModel;
        private GroupBox groupBoxModels;
        private Label lblTime;
        private Label lblResult;
    }
}