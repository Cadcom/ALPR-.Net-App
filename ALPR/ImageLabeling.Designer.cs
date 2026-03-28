namespace ALPR
{
    partial class ImageLabeling
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolStrip = new ToolStrip();
            btnSelectFolder = new ToolStripButton();
            lblFolderPath = new ToolStripLabel();
            sepNav = new ToolStripSeparator();
            btnPrev = new ToolStripButton();
            btnNext = new ToolStripButton();
            sepClass = new ToolStripSeparator();
            lblClassLabel = new ToolStripLabel();
            cmbClass = new ToolStripComboBox();
            btnAddClass = new ToolStripButton();
            sepZoom = new ToolStripSeparator();
            lblZoomLabel = new ToolStripLabel();
            btnZoomIn = new ToolStripButton();
            btnZoomOut = new ToolStripButton();
            btnZoomFit = new ToolStripButton();
            lblZoomPct = new ToolStripLabel();
            sepAutoLabel = new ToolStripSeparator();
            btnAutoLabel = new ToolStripButton();
            btnAutoLabelAll = new ToolStripButton();
            sepPlateList = new ToolStripSeparator();
            btnPlateList = new ToolStripButton();
            sepTemizle = new ToolStripSeparator();
            btnTemizle = new ToolStripButton();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            pbAutoLabel = new ToolStripProgressBar();
            lblSaved = new ToolStripStatusLabel();
            pnlLeft = new Panel();
            txtThumbFilter = new TextBox();
            lvThumbnails = new ListView();
            imgListThumb = new ImageList(components);
            pnlRight = new Panel();
            lvAnnotations = new ListView();
            colAnnId = new ColumnHeader();
            colAnnClass = new ColumnHeader();
            colAnnCx = new ColumnHeader();
            colAnnCy = new ColumnHeader();
            colAnnW = new ColumnHeader();
            colAnnH = new ColumnHeader();
            lblAnnotationsHeader = new Label();
            btnEditAnnotation = new Button();
            btnDeleteAnnotation = new Button();
            btnSaveNow = new Button();
            pnlCanvas = new Panel();
            picCanvas = new PictureBox();
            splitterLeft = new Splitter();
            splitterRight = new Splitter();
            toolStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            pnlLeft.SuspendLayout();
            pnlRight.SuspendLayout();
            pnlCanvas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCanvas).BeginInit();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(20, 20);
            toolStrip.Items.AddRange(new ToolStripItem[] { btnSelectFolder, lblFolderPath, sepNav, btnPrev, btnNext, sepClass, lblClassLabel, cmbClass, btnAddClass, sepZoom, lblZoomLabel, btnZoomIn, btnZoomOut, btnZoomFit, lblZoomPct, sepAutoLabel, btnAutoLabel, btnAutoLabelAll, sepPlateList, btnPlateList, sepTemizle, btnTemizle });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1400, 28);
            toolStrip.TabIndex = 0;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(79, 25);
            btnSelectFolder.Text = "📁 Klasör";
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // lblFolderPath
            // 
            lblFolderPath.ForeColor = Color.Gray;
            lblFolderPath.Name = "lblFolderPath";
            lblFolderPath.Size = new Size(153, 25);
            lblFolderPath.Text = "— klasör seçilmedi —";
            // 
            // sepNav
            // 
            sepNav.Name = "sepNav";
            sepNav.Size = new Size(6, 28);
            // 
            // btnPrev
            // 
            btnPrev.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(75, 25);
            btnPrev.Text = "◀ Önceki";
            btnPrev.Click += btnPrev_Click;
            // 
            // btnNext
            // 
            btnNext.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(79, 25);
            btnNext.Text = "Sonraki ▶";
            btnNext.Click += btnNext_Click;
            // 
            // sepClass
            // 
            sepClass.Name = "sepClass";
            sepClass.Size = new Size(6, 28);
            // 
            // lblClassLabel
            // 
            lblClassLabel.Name = "lblClassLabel";
            lblClassLabel.Size = new Size(45, 25);
            lblClassLabel.Text = "Class:";
            // 
            // cmbClass
            // 
            cmbClass.AutoSize = false;
            cmbClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClass.Name = "cmbClass";
            cmbClass.Size = new Size(120, 28);
            // 
            // btnAddClass
            // 
            btnAddClass.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAddClass.Name = "btnAddClass";
            btnAddClass.Size = new Size(60, 25);
            btnAddClass.Text = "+ Class";
            btnAddClass.Click += btnAddClass_Click;
            // 
            // sepZoom
            // 
            sepZoom.Name = "sepZoom";
            sepZoom.Size = new Size(6, 28);
            // 
            // lblZoomLabel
            // 
            lblZoomLabel.Name = "lblZoomLabel";
            lblZoomLabel.Size = new Size(52, 25);
            lblZoomLabel.Text = "Zoom:";
            // 
            // btnZoomIn
            // 
            btnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomIn.Name = "btnZoomIn";
            btnZoomIn.Size = new Size(29, 25);
            btnZoomIn.Text = "+";
            btnZoomIn.Click += btnZoomIn_Click;
            // 
            // btnZoomOut
            // 
            btnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomOut.Name = "btnZoomOut";
            btnZoomOut.Size = new Size(29, 25);
            btnZoomOut.Text = "−";
            btnZoomOut.Click += btnZoomOut_Click;
            // 
            // btnZoomFit
            // 
            btnZoomFit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomFit.Name = "btnZoomFit";
            btnZoomFit.Size = new Size(29, 25);
            btnZoomFit.Text = "Fit";
            btnZoomFit.Click += btnZoomFit_Click;
            // 
            // lblZoomPct
            // 
            lblZoomPct.Name = "lblZoomPct";
            lblZoomPct.Size = new Size(45, 25);
            lblZoomPct.Text = "100%";
            // 
            // sepAutoLabel
            // 
            sepAutoLabel.Name = "sepAutoLabel";
            sepAutoLabel.Size = new Size(6, 28);
            // 
            // btnAutoLabel
            // 
            btnAutoLabel.BackColor = Color.FromArgb(200, 230, 255);
            btnAutoLabel.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAutoLabel.Name = "btnAutoLabel";
            btnAutoLabel.Size = new Size(153, 25);
            btnAutoLabel.Text = "🤖 Otomatik Etiketle";
            btnAutoLabel.ToolTipText = "Mevcut resmi modelle otomatik etiketle";
            btnAutoLabel.Click += btnAutoLabel_Click;
            // 
            // btnAutoLabelAll
            // 
            btnAutoLabelAll.BackColor = Color.FromArgb(220, 255, 220);
            btnAutoLabelAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAutoLabelAll.Name = "btnAutoLabelAll";
            btnAutoLabelAll.Size = new Size(144, 25);
            btnAutoLabelAll.Text = "🤖 Tümünü Etiketle";
            btnAutoLabelAll.ToolTipText = "Klasördeki tüm resimleri otomatik etiketle";
            btnAutoLabelAll.Click += btnAutoLabelAll_Click;
            // 
            // sepPlateList
            // 
            sepPlateList.Name = "sepPlateList";
            sepPlateList.Size = new Size(6, 28);
            // 
            // btnPlateList
            // 
            btnPlateList.BackColor = Color.FromArgb(255, 230, 200);
            btnPlateList.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPlateList.Name = "btnPlateList";
            btnPlateList.Size = new Size(120, 25);
            btnPlateList.Text = "📋 Plate List";
            btnPlateList.ToolTipText = "Seçili klasördeki tüm plakaları açar";
            btnPlateList.Click += btnPlateList_Click;
            // 
            // sepTemizle
            // 
            sepTemizle.Name = "sepTemizle";
            sepTemizle.Size = new Size(6, 28);
            // 
            // btnTemizle
            // 
            btnTemizle.BackColor = Color.FromArgb(255, 200, 200);
            btnTemizle.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnTemizle.Name = "btnTemizle";
            btnTemizle.Size = new Size(130, 25);
            btnTemizle.Text = "🗑 Klasörü Temizle";
            btnTemizle.ToolTipText = "Sadece bu klasördeki bütün etiketleri dataset'ten siler";
            btnTemizle.Click += btnTemizle_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, pbAutoLabel, lblSaved });
            statusStrip.Location = new Point(0, 734);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1400, 26);
            statusStrip.TabIndex = 5;
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(44, 20);
            lblStatus.Spring = true;
            lblStatus.Text = "Hazır";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbAutoLabel
            // 
            pbAutoLabel.Name = "pbAutoLabel";
            pbAutoLabel.Size = new Size(150, 18);
            pbAutoLabel.Visible = false;
            // 
            // lblSaved
            // 
            lblSaved.ForeColor = Color.Green;
            lblSaved.Name = "lblSaved";
            lblSaved.Size = new Size(0, 20);
            // 
            // pnlLeft
            // 
            // Filter textbox sits above thumbnails
            txtThumbFilter.Dock = DockStyle.Top;
            txtThumbFilter.Height = 26;
            txtThumbFilter.Margin = new Padding(3);
            txtThumbFilter.Name = "txtThumbFilter";
            txtThumbFilter.PlaceholderText = "Filtre (başlayanla)...";
            txtThumbFilter.TextChanged += txtThumbFilter_TextChanged;

            pnlLeft.Controls.Add(lvThumbnails);
            pnlLeft.Controls.Add(txtThumbFilter);
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Location = new Point(0, 28);
            pnlLeft.MinimumSize = new Size(80, 0);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(140, 706);
            pnlLeft.TabIndex = 4;
            // 
            // lvThumbnails
            // 
            lvThumbnails.Dock = DockStyle.Fill;
            lvThumbnails.LargeImageList = imgListThumb;
            lvThumbnails.Location = new Point(0, 0);
            lvThumbnails.MultiSelect = false;
            lvThumbnails.Name = "lvThumbnails";
            lvThumbnails.Size = new Size(140, 706);
            lvThumbnails.TabIndex = 0;
            lvThumbnails.UseCompatibleStateImageBehavior = false;
            lvThumbnails.SelectedIndexChanged += lvThumbnails_SelectedIndexChanged;
            // 
            // imgListThumb
            // 
            imgListThumb.ColorDepth = ColorDepth.Depth32Bit;
            imgListThumb.ImageSize = new Size(96, 72);
            imgListThumb.TransparentColor = Color.Transparent;
            // 
            // pnlRight
            // 
            pnlRight.Controls.Add(lvAnnotations);
            pnlRight.Controls.Add(lblAnnotationsHeader);
            pnlRight.Controls.Add(btnEditAnnotation);
            pnlRight.Controls.Add(btnDeleteAnnotation);
            pnlRight.Controls.Add(btnSaveNow);
            pnlRight.Dock = DockStyle.Right;
            pnlRight.Location = new Point(1060, 28);
            pnlRight.MinimumSize = new Size(200, 0);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(340, 706);
            pnlRight.TabIndex = 2;
            // 
            // lvAnnotations
            // 
            lvAnnotations.Columns.AddRange(new ColumnHeader[] { colAnnId, colAnnClass, colAnnCx, colAnnCy, colAnnW, colAnnH });
            lvAnnotations.Dock = DockStyle.Fill;
            lvAnnotations.FullRowSelect = true;
            lvAnnotations.GridLines = true;
            lvAnnotations.Location = new Point(0, 22);
            lvAnnotations.MultiSelect = false;
            lvAnnotations.Name = "lvAnnotations";
            lvAnnotations.Size = new Size(340, 600);
            lvAnnotations.TabIndex = 0;
            lvAnnotations.UseCompatibleStateImageBehavior = false;
            lvAnnotations.View = View.Details;
            lvAnnotations.SelectedIndexChanged += lvAnnotations_SelectedIndexChanged;
            lvAnnotations.KeyDown += ImageLabeling_KeyDown;
            // 
            // colAnnId
            // 
            colAnnId.Text = "#";
            colAnnId.Width = 28;
            // 
            // colAnnClass
            // 
            colAnnClass.Text = "Class";
            colAnnClass.Width = 70;
            // 
            // colAnnCx
            // 
            colAnnCx.Text = "Cx";
            colAnnCx.Width = 55;
            // 
            // colAnnCy
            // 
            colAnnCy.Text = "Cy";
            colAnnCy.Width = 55;
            // 
            // colAnnW
            // 
            colAnnW.Text = "W";
            colAnnW.Width = 55;
            // 
            // colAnnH
            // 
            colAnnH.Text = "H";
            colAnnH.Width = 55;
            // 
            // lblAnnotationsHeader
            // 
            lblAnnotationsHeader.BackColor = Color.FromArgb(230, 230, 240);
            lblAnnotationsHeader.Dock = DockStyle.Top;
            lblAnnotationsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblAnnotationsHeader.Location = new Point(0, 0);
            lblAnnotationsHeader.Name = "lblAnnotationsHeader";
            lblAnnotationsHeader.Size = new Size(340, 22);
            lblAnnotationsHeader.TabIndex = 1;
            lblAnnotationsHeader.Text = "Annotations";
            lblAnnotationsHeader.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnEditAnnotation
            // 
            btnEditAnnotation.Dock = DockStyle.Bottom;
            btnEditAnnotation.Location = new Point(0, 622);
            btnEditAnnotation.Name = "btnEditAnnotation";
            btnEditAnnotation.Size = new Size(340, 28);
            btnEditAnnotation.TabIndex = 2;
            btnEditAnnotation.Text = "✏ Düzenle";
            btnEditAnnotation.Click += btnEditAnnotation_Click;
            // 
            // btnDeleteAnnotation
            // 
            btnDeleteAnnotation.Dock = DockStyle.Bottom;
            btnDeleteAnnotation.ForeColor = Color.DarkRed;
            btnDeleteAnnotation.Location = new Point(0, 650);
            btnDeleteAnnotation.Name = "btnDeleteAnnotation";
            btnDeleteAnnotation.Size = new Size(340, 28);
            btnDeleteAnnotation.TabIndex = 3;
            btnDeleteAnnotation.Text = "🗑 Sil";
            btnDeleteAnnotation.Click += btnDeleteAnnotation_Click;
            // 
            // btnSaveNow
            // 
            btnSaveNow.Dock = DockStyle.Bottom;
            btnSaveNow.ForeColor = Color.DarkBlue;
            btnSaveNow.Location = new Point(0, 678);
            btnSaveNow.Name = "btnSaveNow";
            btnSaveNow.Size = new Size(340, 28);
            btnSaveNow.TabIndex = 4;
            btnSaveNow.Text = "💾 Kaydet";
            btnSaveNow.Click += btnSaveNow_Click;
            // 
            // pnlCanvas
            // 
            pnlCanvas.AutoScroll = true;
            pnlCanvas.BackColor = Color.DimGray;
            pnlCanvas.Controls.Add(picCanvas);
            pnlCanvas.Dock = DockStyle.Fill;
            pnlCanvas.Location = new Point(144, 28);
            pnlCanvas.Name = "pnlCanvas";
            pnlCanvas.Size = new Size(912, 706);
            pnlCanvas.TabIndex = 0;
            pnlCanvas.MouseWheel += pnlCanvas_MouseWheel;
            // 
            // picCanvas
            // 
            picCanvas.BackColor = Color.DimGray;
            picCanvas.Cursor = Cursors.Cross;
            picCanvas.Location = new Point(0, 0);
            picCanvas.Name = "picCanvas";
            picCanvas.Size = new Size(800, 600);
            picCanvas.TabIndex = 0;
            picCanvas.TabStop = false;
            picCanvas.Paint += picCanvas_Paint;
            picCanvas.MouseDown += picCanvas_MouseDown;
            picCanvas.MouseMove += picCanvas_MouseMove;
            picCanvas.MouseUp += picCanvas_MouseUp;
            // 
            // splitterLeft
            // 
            splitterLeft.Location = new Point(140, 28);
            splitterLeft.Name = "splitterLeft";
            splitterLeft.Size = new Size(4, 706);
            splitterLeft.TabIndex = 3;
            splitterLeft.TabStop = false;
            // 
            // splitterRight
            // 
            splitterRight.Dock = DockStyle.Right;
            splitterRight.Location = new Point(1056, 28);
            splitterRight.Name = "splitterRight";
            splitterRight.Size = new Size(4, 706);
            splitterRight.TabIndex = 1;
            splitterRight.TabStop = false;
            // 
            // ImageLabeling
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 760);
            Controls.Add(pnlCanvas);
            Controls.Add(splitterRight);
            Controls.Add(pnlRight);
            Controls.Add(splitterLeft);
            Controls.Add(pnlLeft);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            KeyPreview = true;
            MinimumSize = new Size(900, 600);
            Name = "ImageLabeling";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Image Labeling — YOLO BBox Editor";
            FormClosing += ImageLabeling_FormClosing;
            KeyDown += ImageLabeling_KeyDown;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            pnlLeft.ResumeLayout(false);
            pnlRight.ResumeLayout(false);
            pnlCanvas.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picCanvas).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // Controls
        private System.Windows.Forms.ToolStrip           toolStrip;
        private System.Windows.Forms.ToolStripButton     btnSelectFolder;
        private System.Windows.Forms.ToolStripLabel      lblFolderPath;
        private System.Windows.Forms.ToolStripSeparator  sepNav;
        private System.Windows.Forms.ToolStripButton     btnPrev;
        private System.Windows.Forms.ToolStripButton     btnNext;
        private System.Windows.Forms.ToolStripSeparator  sepClass;
        private System.Windows.Forms.ToolStripLabel      lblClassLabel;
        private System.Windows.Forms.ToolStripComboBox   cmbClass;
        private System.Windows.Forms.ToolStripButton     btnAddClass;
        private System.Windows.Forms.ToolStripSeparator  sepZoom;
        private System.Windows.Forms.ToolStripLabel      lblZoomLabel;
        private System.Windows.Forms.ToolStripButton     btnZoomIn;
        private System.Windows.Forms.ToolStripButton     btnZoomOut;
        private System.Windows.Forms.ToolStripButton     btnZoomFit;
        private System.Windows.Forms.ToolStripLabel      lblZoomPct;
        private System.Windows.Forms.ToolStripSeparator  sepAutoLabel;
        private System.Windows.Forms.ToolStripButton     btnAutoLabel;
        private System.Windows.Forms.ToolStripButton     btnAutoLabelAll;
        private System.Windows.Forms.ToolStripSeparator  sepPlateList;
        private System.Windows.Forms.ToolStripButton     btnPlateList;
        private System.Windows.Forms.ToolStripSeparator  sepTemizle;
        private System.Windows.Forms.ToolStripButton     btnTemizle;

        private System.Windows.Forms.StatusStrip         statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar pbAutoLabel;
        private System.Windows.Forms.ToolStripStatusLabel lblSaved;

        private System.Windows.Forms.Panel               pnlLeft;
        private System.Windows.Forms.TextBox             txtThumbFilter;
        private System.Windows.Forms.ListView            lvThumbnails;
        private System.Windows.Forms.ImageList           imgListThumb;
        private System.Windows.Forms.Splitter            splitterLeft;

        private System.Windows.Forms.Panel               pnlRight;
        private System.Windows.Forms.Label               lblAnnotationsHeader;
        private System.Windows.Forms.ListView            lvAnnotations;
        private System.Windows.Forms.ColumnHeader        colAnnId;
        private System.Windows.Forms.ColumnHeader        colAnnClass;
        private System.Windows.Forms.ColumnHeader        colAnnCx;
        private System.Windows.Forms.ColumnHeader        colAnnCy;
        private System.Windows.Forms.ColumnHeader        colAnnW;
        private System.Windows.Forms.ColumnHeader        colAnnH;
        private System.Windows.Forms.Button              btnDeleteAnnotation;
        private System.Windows.Forms.Button              btnEditAnnotation;
        private System.Windows.Forms.Button              btnSaveNow;
        private System.Windows.Forms.Splitter            splitterRight;

        private System.Windows.Forms.Panel               pnlCanvas;
        private System.Windows.Forms.PictureBox          picCanvas;
    }
}