namespace ALPR
{
    partial class frmDBNetTesting
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
            splitContainer1 = new SplitContainer();
            groupBoxModels = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            btnSelectDBNet1 = new Button();
            btnSelectDBNet2 = new Button();
            btnSelectDBNet3 = new Button();
            btnSelectDBNet4 = new Button();
            lblDBNet1 = new Label();
            lblDBNet2 = new Label();
            lblDBNet3 = new Label();
            lblDBNet4 = new Label();
            groupBoxTestImage = new GroupBox();
            btnSelectTestImage = new Button();
            pictureBoxTestImage = new PictureBox();
            groupBoxSettings = new GroupBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            label1 = new Label();
            nudConfidenceThreshold = new NumericUpDown();
            chkUseGpu = new CheckBox();
            btnRunTests = new Button();
            tabControlResults = new TabControl();
            tabPageComparison = new TabPage();
            dataGridViewResults = new DataGridView();
            tabPageVisualization = new TabPage();
            splitContainer2 = new SplitContainer();
            listBoxModels = new ListBox();
            pictureBoxVisualization = new PictureBox();
            tabPageJsonOutput = new TabPage();
            splitContainer3 = new SplitContainer();
            comboBoxJsonModel = new ComboBox();
            txtJsonOutput = new TextBox();
            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            groupBoxModels.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBoxTestImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxTestImage).BeginInit();
            groupBoxSettings.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudConfidenceThreshold).BeginInit();
            tabControlResults.SuspendLayout();
            tabPageComparison.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            tabPageVisualization.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVisualization).BeginInit();
            tabPageJsonOutput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(groupBoxModels);
            splitContainer1.Panel1.Controls.Add(groupBoxTestImage);
            splitContainer1.Panel1.Controls.Add(groupBoxSettings);
            splitContainer1.Panel1.Controls.Add(btnRunTests);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControlResults);
            splitContainer1.Size = new Size(1200, 700);
            splitContainer1.SplitterDistance = 200;
            splitContainer1.TabIndex = 0;
            // 
            // groupBoxModels
            // 
            groupBoxModels.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxModels.Controls.Add(tableLayoutPanel1);
            groupBoxModels.Location = new Point(12, 12);
            groupBoxModels.Name = "groupBoxModels";
            groupBoxModels.Size = new Size(600, 180);
            groupBoxModels.TabIndex = 0;
            groupBoxModels.TabStop = false;
            groupBoxModels.Text = "DBNet Modelleri";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(btnSelectDBNet1, 0, 0);
            tableLayoutPanel1.Controls.Add(btnSelectDBNet2, 0, 1);
            tableLayoutPanel1.Controls.Add(btnSelectDBNet3, 0, 2);
            tableLayoutPanel1.Controls.Add(btnSelectDBNet4, 0, 3);
            tableLayoutPanel1.Controls.Add(lblDBNet1, 1, 0);
            tableLayoutPanel1.Controls.Add(lblDBNet2, 1, 1);
            tableLayoutPanel1.Controls.Add(lblDBNet3, 1, 2);
            tableLayoutPanel1.Controls.Add(lblDBNet4, 1, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 19);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.Size = new Size(594, 158);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // btnSelectDBNet1
            // 
            btnSelectDBNet1.Dock = DockStyle.Fill;
            btnSelectDBNet1.Location = new Point(3, 3);
            btnSelectDBNet1.Name = "btnSelectDBNet1";
            btnSelectDBNet1.Size = new Size(114, 33);
            btnSelectDBNet1.TabIndex = 0;
            btnSelectDBNet1.Text = "Model 1 Seç";
            btnSelectDBNet1.UseVisualStyleBackColor = true;
            btnSelectDBNet1.Click += btnSelectDBNet1_Click;
            // 
            // btnSelectDBNet2
            // 
            btnSelectDBNet2.Dock = DockStyle.Fill;
            btnSelectDBNet2.Location = new Point(3, 42);
            btnSelectDBNet2.Name = "btnSelectDBNet2";
            btnSelectDBNet2.Size = new Size(114, 33);
            btnSelectDBNet2.TabIndex = 1;
            btnSelectDBNet2.Text = "Model 2 Seç";
            btnSelectDBNet2.UseVisualStyleBackColor = true;
            btnSelectDBNet2.Click += btnSelectDBNet2_Click;
            // 
            // btnSelectDBNet3
            // 
            btnSelectDBNet3.Dock = DockStyle.Fill;
            btnSelectDBNet3.Location = new Point(3, 81);
            btnSelectDBNet3.Name = "btnSelectDBNet3";
            btnSelectDBNet3.Size = new Size(114, 33);
            btnSelectDBNet3.TabIndex = 2;
            btnSelectDBNet3.Text = "Model 3 Seç";
            btnSelectDBNet3.UseVisualStyleBackColor = true;
            btnSelectDBNet3.Click += btnSelectDBNet3_Click;
            // 
            // btnSelectDBNet4
            // 
            btnSelectDBNet4.Dock = DockStyle.Fill;
            btnSelectDBNet4.Location = new Point(3, 120);
            btnSelectDBNet4.Name = "btnSelectDBNet4";
            btnSelectDBNet4.Size = new Size(114, 35);
            btnSelectDBNet4.TabIndex = 3;
            btnSelectDBNet4.Text = "Model 4 Seç";
            btnSelectDBNet4.UseVisualStyleBackColor = true;
            btnSelectDBNet4.Click += btnSelectDBNet4_Click;
            // 
            // lblDBNet1
            // 
            lblDBNet1.AutoSize = true;
            lblDBNet1.Dock = DockStyle.Fill;
            lblDBNet1.Location = new Point(123, 0);
            lblDBNet1.Name = "lblDBNet1";
            lblDBNet1.Size = new Size(468, 39);
            lblDBNet1.TabIndex = 4;
            lblDBNet1.Text = "Model 1: Seçilmemiþ";
            lblDBNet1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDBNet2
            // 
            lblDBNet2.AutoSize = true;
            lblDBNet2.Dock = DockStyle.Fill;
            lblDBNet2.Location = new Point(123, 39);
            lblDBNet2.Name = "lblDBNet2";
            lblDBNet2.Size = new Size(468, 39);
            lblDBNet2.TabIndex = 5;
            lblDBNet2.Text = "Model 2: Seçilmemiþ";
            lblDBNet2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDBNet3
            // 
            lblDBNet3.AutoSize = true;
            lblDBNet3.Dock = DockStyle.Fill;
            lblDBNet3.Location = new Point(123, 78);
            lblDBNet3.Name = "lblDBNet3";
            lblDBNet3.Size = new Size(468, 39);
            lblDBNet3.TabIndex = 6;
            lblDBNet3.Text = "Model 3: Seçilmemiþ";
            lblDBNet3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDBNet4
            // 
            lblDBNet4.AutoSize = true;
            lblDBNet4.Dock = DockStyle.Fill;
            lblDBNet4.Location = new Point(123, 117);
            lblDBNet4.Name = "lblDBNet4";
            lblDBNet4.Size = new Size(468, 41);
            lblDBNet4.TabIndex = 7;
            lblDBNet4.Text = "Model 4: Seçilmemiþ";
            lblDBNet4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // groupBoxTestImage
            // 
            groupBoxTestImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBoxTestImage.Controls.Add(btnSelectTestImage);
            groupBoxTestImage.Controls.Add(pictureBoxTestImage);
            groupBoxTestImage.Location = new Point(630, 12);
            groupBoxTestImage.Name = "groupBoxTestImage";
            groupBoxTestImage.Size = new Size(250, 180);
            groupBoxTestImage.TabIndex = 1;
            groupBoxTestImage.TabStop = false;
            groupBoxTestImage.Text = "Test Resmi";
            // 
            // btnSelectTestImage
            // 
            btnSelectTestImage.Dock = DockStyle.Bottom;
            btnSelectTestImage.Location = new Point(3, 152);
            btnSelectTestImage.Name = "btnSelectTestImage";
            btnSelectTestImage.Size = new Size(244, 25);
            btnSelectTestImage.TabIndex = 1;
            btnSelectTestImage.Text = "Resim Seç";
            btnSelectTestImage.UseVisualStyleBackColor = true;
            btnSelectTestImage.Click += btnSelectTestImage_Click;
            // 
            // pictureBoxTestImage
            // 
            pictureBoxTestImage.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxTestImage.Dock = DockStyle.Fill;
            pictureBoxTestImage.Location = new Point(3, 19);
            pictureBoxTestImage.Name = "pictureBoxTestImage";
            pictureBoxTestImage.Size = new Size(244, 158);
            pictureBoxTestImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxTestImage.TabIndex = 0;
            pictureBoxTestImage.TabStop = false;
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBoxSettings.Controls.Add(tableLayoutPanel2);
            groupBoxSettings.Location = new Point(900, 12);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Size = new Size(200, 120);
            groupBoxSettings.TabIndex = 2;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Ayarlar";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(label1, 0, 0);
            tableLayoutPanel2.Controls.Add(nudConfidenceThreshold, 0, 1);
            tableLayoutPanel2.Controls.Add(chkUseGpu, 0, 2);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 19);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.Size = new Size(194, 98);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 0;
            label1.Text = "Güven Eþiði (0-1):";
            // 
            // nudConfidenceThreshold
            // 
            nudConfidenceThreshold.DecimalPlaces = 2;
            nudConfidenceThreshold.Dock = DockStyle.Fill;
            nudConfidenceThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            nudConfidenceThreshold.Location = new Point(3, 18);
            nudConfidenceThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nudConfidenceThreshold.Name = "nudConfidenceThreshold";
            nudConfidenceThreshold.Size = new Size(188, 23);
            nudConfidenceThreshold.TabIndex = 1;
            nudConfidenceThreshold.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // chkUseGpu
            // 
            chkUseGpu.AutoSize = true;
            chkUseGpu.Checked = true;
            chkUseGpu.CheckState = CheckState.Checked;
            chkUseGpu.Location = new Point(3, 47);
            chkUseGpu.Name = "chkUseGpu";
            chkUseGpu.Size = new Size(98, 19);
            chkUseGpu.TabIndex = 2;
            chkUseGpu.Text = "GPU Kullan";
            chkUseGpu.UseVisualStyleBackColor = true;
            // 
            // btnRunTests
            // 
            btnRunTests.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRunTests.BackColor = Color.LightGreen;
            btnRunTests.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnRunTests.Location = new Point(900, 140);
            btnRunTests.Name = "btnRunTests";
            btnRunTests.Size = new Size(200, 50);
            btnRunTests.TabIndex = 3;
            btnRunTests.Text = "Testleri Baþlat";
            btnRunTests.UseVisualStyleBackColor = false;
            btnRunTests.Click += btnRunTests_Click;
            // 
            // tabControlResults
            // 
            tabControlResults.Controls.Add(tabPageComparison);
            tabControlResults.Controls.Add(tabPageVisualization);
            tabControlResults.Controls.Add(tabPageJsonOutput);
            tabControlResults.Dock = DockStyle.Fill;
            tabControlResults.Location = new Point(0, 0);
            tabControlResults.Name = "tabControlResults";
            tabControlResults.SelectedIndex = 0;
            tabControlResults.Size = new Size(1200, 496);
            tabControlResults.TabIndex = 0;
            // 
            // tabPageComparison
            // 
            tabPageComparison.Controls.Add(dataGridViewResults);
            tabPageComparison.Location = new Point(4, 24);
            tabPageComparison.Name = "tabPageComparison";
            tabPageComparison.Padding = new Padding(3);
            tabPageComparison.Size = new Size(1192, 468);
            tabPageComparison.TabIndex = 0;
            tabPageComparison.Text = "Model Karþýlaþtýrmasý";
            tabPageComparison.UseVisualStyleBackColor = true;
            // 
            // dataGridViewResults
            // 
            dataGridViewResults.AllowUserToAddRows = false;
            dataGridViewResults.AllowUserToDeleteRows = false;
            dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Dock = DockStyle.Fill;
            dataGridViewResults.Location = new Point(3, 3);
            dataGridViewResults.Name = "dataGridViewResults";
            dataGridViewResults.ReadOnly = true;
            dataGridViewResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewResults.Size = new Size(1186, 462);
            dataGridViewResults.TabIndex = 0;
            // 
            // tabPageVisualization
            // 
            tabPageVisualization.Controls.Add(splitContainer2);
            tabPageVisualization.Location = new Point(4, 24);
            tabPageVisualization.Name = "tabPageVisualization";
            tabPageVisualization.Padding = new Padding(3);
            tabPageVisualization.Size = new Size(1192, 468);
            tabPageVisualization.TabIndex = 1;
            tabPageVisualization.Text = "Görselleþtirme";
            tabPageVisualization.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(3, 3);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(listBoxModels);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(pictureBoxVisualization);
            splitContainer2.Size = new Size(1186, 462);
            splitContainer2.SplitterDistance = 200;
            splitContainer2.TabIndex = 0;
            // 
            // listBoxModels
            // 
            listBoxModels.Dock = DockStyle.Fill;
            listBoxModels.FormattingEnabled = true;
            listBoxModels.Location = new Point(0, 0);
            listBoxModels.Name = "listBoxModels";
            listBoxModels.Size = new Size(200, 462);
            listBoxModels.TabIndex = 0;
            listBoxModels.SelectedIndexChanged += listBoxModels_SelectedIndexChanged;
            // 
            // pictureBoxVisualization
            // 
            pictureBoxVisualization.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxVisualization.Dock = DockStyle.Fill;
            pictureBoxVisualization.Location = new Point(0, 0);
            pictureBoxVisualization.Name = "pictureBoxVisualization";
            pictureBoxVisualization.Size = new Size(982, 462);
            pictureBoxVisualization.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxVisualization.TabIndex = 0;
            pictureBoxVisualization.TabStop = false;
            // 
            // tabPageJsonOutput
            // 
            tabPageJsonOutput.Controls.Add(splitContainer3);
            tabPageJsonOutput.Location = new Point(4, 24);
            tabPageJsonOutput.Name = "tabPageJsonOutput";
            tabPageJsonOutput.Size = new Size(1192, 468);
            tabPageJsonOutput.TabIndex = 2;
            tabPageJsonOutput.Text = "JSON Çýktýsý";
            tabPageJsonOutput.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(comboBoxJsonModel);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(txtJsonOutput);
            splitContainer3.Size = new Size(1192, 468);
            splitContainer3.SplitterDistance = 40;
            splitContainer3.TabIndex = 0;
            // 
            // comboBoxJsonModel
            // 
            comboBoxJsonModel.Dock = DockStyle.Fill;
            comboBoxJsonModel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxJsonModel.FormattingEnabled = true;
            comboBoxJsonModel.Location = new Point(0, 0);
            comboBoxJsonModel.Name = "comboBoxJsonModel";
            comboBoxJsonModel.Size = new Size(1192, 23);
            comboBoxJsonModel.TabIndex = 0;
            comboBoxJsonModel.SelectedIndexChanged += comboBoxJsonModel_SelectedIndexChanged;
            // 
            // txtJsonOutput
            // 
            txtJsonOutput.Dock = DockStyle.Fill;
            txtJsonOutput.Font = new Font("Consolas", 9F);
            txtJsonOutput.Location = new Point(0, 0);
            txtJsonOutput.Multiline = true;
            txtJsonOutput.Name = "txtJsonOutput";
            txtJsonOutput.ReadOnly = true;
            txtJsonOutput.ScrollBars = ScrollBars.Both;
            txtJsonOutput.Size = new Size(1192, 424);
            txtJsonOutput.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar });
            statusStrip1.Location = new Point(0, 678);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1200, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(34, 17);
            lblStatus.Text = "Hazýr";
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 16);
            progressBar.Visible = false;
            // 
            // frmDBNetTesting
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 700);
            Controls.Add(statusStrip1);
            Controls.Add(splitContainer1);
            Name = "frmDBNetTesting";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DBNet Model Test Platformu";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            groupBoxModels.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            groupBoxTestImage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxTestImage).EndInit();
            groupBoxSettings.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudConfidenceThreshold).EndInit();
            tabControlResults.ResumeLayout(false);
            tabPageComparison.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            tabPageVisualization.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxVisualization).EndInit();
            tabPageJsonOutput.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            splitContainer3.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private SplitContainer splitContainer1;
        private GroupBox groupBoxModels;
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnSelectDBNet1;
        private Button btnSelectDBNet2;
        private Button btnSelectDBNet3;
        private Button btnSelectDBNet4;
        private Label lblDBNet1;
        private Label lblDBNet2;
        private Label lblDBNet3;
        private Label lblDBNet4;
        private GroupBox groupBoxTestImage;
        private Button btnSelectTestImage;
        private PictureBox pictureBoxTestImage;
        private GroupBox groupBoxSettings;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label1;
        private NumericUpDown nudConfidenceThreshold;
        private CheckBox chkUseGpu;
        private Button btnRunTests;
        private TabControl tabControlResults;
        private TabPage tabPageComparison;
        private DataGridView dataGridViewResults;
        private TabPage tabPageVisualization;
        private SplitContainer splitContainer2;
        private ListBox listBoxModels;
        private PictureBox pictureBoxVisualization;
        private TabPage tabPageJsonOutput;
        private SplitContainer splitContainer3;
        private ComboBox comboBoxJsonModel;
        private TextBox txtJsonOutput;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
    }
}