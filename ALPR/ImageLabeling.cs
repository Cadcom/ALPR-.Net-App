using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace ALPR
{
    // ─────────────────────────────────────────────────────────────────────────
    // Form logic
    // ─────────────────────────────────────────────────────────────────────────


    public partial class ImageLabeling : Form
    {
        // ── State ────────────────────────────────────────────────────────────
        private string _folderPath = "";
        private string _datasetPath = "";
        private List<string> _imageFiles = new();
        private List<string> _classes = new() { "plate" };
        private int _selectedClassId = 0;   // ToolStrip focus kaybında SelectedIndex -1 döner
        private DatasetRoot _dataset = new();

        private int _currentIndex = -1;
        private Bitmap? _currentImage = null;

        private List<BBoxAnnotation> _annotations = new();          // current image annotations
        private int _selectedAnnot = -1;            // index in _annotations
        // Thumbnail filter text (for lvThumbnails)
        private string _thumbFilter = string.Empty;
        private System.Threading.CancellationTokenSource? _thumbCts;

        // ── Zoom ────────────────────────────────────────────────────────────
        private float _zoom = 1.0f;
        private const float ZoomStep = 0.15f;
        private const float ZoomMin = 0.05f;
        private const float ZoomMax = 10.0f;

        // ── Drawing state ───────────────────────────────────────────────────
        private enum DrawMode { None, Drawing, Moving, Resizing }
        private DrawMode _drawMode = DrawMode.None;

        private PointF _dragStart;           // canvas coords
        private RectangleF _drawRect;        // in-progress draw rect (canvas coords)

        private PointF _moveOffset;        // offset within bbox when dragging
        private RectangleF _originalRect;    // bbox rect before resize starts

        private enum Handle { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }
        private Handle _activeHandle = Handle.None;
        private const int HandleSize = 7;

        // ── Class colors (deterministic per classId) ─────────────────────────
        private static readonly Color[] _palette = {
            Color.FromArgb(255,  80,  80),   // 0 red
            Color.FromArgb( 80, 200,  80),   // 1 green
            Color.FromArgb( 80, 140, 255),   // 2 blue
            Color.FromArgb(255, 200,  50),   // 3 yellow
            Color.FromArgb(220,  80, 220),   // 4 magenta
            Color.FromArgb( 80, 210, 210),   // 5 cyan
            Color.FromArgb(255, 140,  40),   // 6 orange
            Color.FromArgb(160, 255, 100),   // 7 lime
        };

        private Color ClassColor(int id)
        {
            if (id < 0) return Color.White;
            if (id < _palette.Length) return _palette[id];

            var rnd = new Random(id * 73);
            int r = rnd.Next(60, 240);
            int g = rnd.Next(60, 240);
            int b = rnd.Next(60, 240);
            return Color.FromArgb(255, r, g, b);
        }

        // ── Constructor ──────────────────────────────────────────────────────
        public ImageLabeling()
        {
            InitializeComponent();
            cmbClass.SelectedIndexChanged += cmbClass_SelectedIndexChanged;
            RefreshClassCombo();
            UpdateNavButtons();
        }

        private void txtThumbFilter_TextChanged(object? sender, EventArgs e)
        {
            // Update filter and rebuild thumbnails
            try
            {
                _thumbFilter = txtThumbFilter?.Text?.Trim() ?? string.Empty;
                BuildThumbnailsAsync();
            }
            catch { }
        }

        // ────────────────────────────────────────────────────────────────────
        // Folder / JSON helpers
        // ────────────────────────────────────────────────────────────────────

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = "Resim klasörünü seçin" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            LoadFolder(dlg.SelectedPath);
        }

        private void LoadFolder(string path)
        {
            SaveCurrentAnnotations();
            _folderPath = path;
            _datasetPath = Path.Combine(Path.GetDirectoryName(path)!, "dataset.json");
            lblFolderPath.Text = path;
            lblFolderPath.ForeColor = System.Drawing.SystemColors.ControlLightLight;

            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".tif", ".tiff" };
            _imageFiles = Directory.GetFiles(path)
                           .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                           .OrderBy(f => f)
                           .ToList();

            LoadDataset();
            BuildThumbnailsAsync();

            _currentIndex = _imageFiles.Count > 0 ? 0 : -1;
            LoadCurrentImage();
            UpdateNavButtons();
        }

        private void LoadDataset()
        {
            if (System.IO.File.Exists(_datasetPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_datasetPath);
                    _dataset = JsonSerializer.Deserialize<DatasetRoot>(json) ?? new DatasetRoot();
                }
                catch { _dataset = new DatasetRoot(); }
            }
            else
            {
                _dataset = new DatasetRoot();
            }

            // Merge classes from dataset
            foreach (var c in _dataset.Classes)
                if (!_classes.Contains(c)) _classes.Add(c);

            RefreshClassCombo();
        }

        private void SaveDataset()
        {
            if (_datasetPath == "") return;
            var opts = new JsonSerializerOptions { WriteIndented = true };
            _dataset.Classes = new List<string>(_classes);
            System.IO.File.WriteAllText(_datasetPath, JsonSerializer.Serialize(_dataset, opts));
            lblSaved.Text = "✓ Kaydedildi";
            lblSaved.ForeColor = Color.Green;
        }

        private string GetDatasetRelativePath(string fullPath)
        {
            var dirName = new DirectoryInfo(Path.GetDirectoryName(fullPath)!).Name;
            return Path.Combine(dirName, Path.GetFileName(fullPath));
        }

        private void SaveCurrentAnnotations()
        {
            if (_currentIndex < 0 || _currentImage == null) return;

            string fileName = GetDatasetRelativePath(_imageFiles[_currentIndex]);

            var entry = _dataset.Images.FirstOrDefault(e => e.File == fileName);
            if (entry == null)
            {
                entry = new ImageEntry
                {
                    File = fileName,
                    Width = _currentImage.Width,
                    Height = _currentImage.Height
                };
                _dataset.Images.Add(entry);
            }

            entry.Width = _currentImage.Width;
            entry.Height = _currentImage.Height;
            entry.Annotations.Boxes = _annotations
                .Select(a => new float[] { a.ClassId, a.Cx, a.Cy, a.W, a.H })
                .ToList();

            SaveDataset();
        }

        // ────────────────────────────────────────────────────────────────────
        // Thumbnails
        // ────────────────────────────────────────────────────────────────────

        private async void BuildThumbnailsAsync()
        {
            // İptal token: eski görevleri iptal et
            _thumbCts?.Cancel();
            _thumbCts = new System.Threading.CancellationTokenSource();
            var cancellationToken = _thumbCts.Token;

            // Build a filtered list of files based on thumbnail filter (starts-with)
            lvThumbnails.Items.Clear();
            imgListThumb.Images.Clear();

            var filteredList = string.IsNullOrWhiteSpace(_thumbFilter)
                ? _imageFiles
                : _imageFiles.Where(p => Path.GetFileName(p).StartsWith(_thumbFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            // Placeholder items for immediate UI feedback
            for (int i = 0; i < filteredList.Count; i++)
            {
                var item = new ListViewItem(Path.GetFileName(filteredList[i]));
                // store original index for navigation mapping
                item.Tag = _imageFiles.IndexOf(filteredList[i]);
                lvThumbnails.Items.Add(item);
            }

            lblStatus.Text = "Küçük resimler yükleniyor...";
            Application.DoEvents();

            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < filteredList.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        Bitmap thumb;
                        try
                        {
                            using var fs = new FileStream(filteredList[i], FileMode.Open, FileAccess.Read);
                            using var bmp = new Bitmap(fs);
                            thumb = new Bitmap(bmp, imgListThumb.ImageSize);
                        }
                        catch
                        {
                            thumb = new Bitmap(imgListThumb.ImageSize.Width, imgListThumb.ImageSize.Height);
                        }

                        if (this.IsDisposed || this.Disposing) break;

                        int idx = i;
                        try
                        {
                            this.Invoke((Action)(() =>
                            {
                                if (this.IsDisposed || this.Disposing) return;
                                if (cancellationToken.IsCancellationRequested) return;
                                if (idx < lvThumbnails.Items.Count)
                                {
                                    imgListThumb.Images.Add(thumb);
                                    lvThumbnails.Items[idx].ImageIndex = idx;
                                }
                            }));
                        }
                        catch (ObjectDisposedException) { break; }
                        catch (InvalidOperationException) { break; }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Beklenen: yeni filtre tetiklendiğinde eski task iptal edilir
            }

            if (!this.IsDisposed && !this.Disposing)
            {
                UpdateStatus();
            }
        }

        private void lvThumbnails_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvThumbnails.SelectedIndices.Count == 0) return;
            int sel = lvThumbnails.SelectedIndices[0];
            var item = lvThumbnails.Items[sel];
            // item.Tag holds original index in _imageFiles
            int originalIdx = item.Tag is int t ? t : sel;
            if (originalIdx == _currentIndex) return;
            SaveCurrentAnnotations();
            _currentIndex = originalIdx;
            LoadCurrentImage();
            UpdateNavButtons();
        }

        // ────────────────────────────────────────────────────────────────────
        // Image loading & navigation
        // ────────────────────────────────────────────────────────────────────

        private void LoadCurrentImage()
        {
            _currentImage?.Dispose();
            _currentImage = null;
            _annotations = new List<BBoxAnnotation>();
            _selectedAnnot = -1;

            if (_currentIndex < 0 || _currentIndex >= _imageFiles.Count)
            {
                picCanvas.Invalidate();
                RefreshAnnotationList();
                UpdateStatus();
                return;
            }

            try
            {
                using var fs = new FileStream(_imageFiles[_currentIndex], FileMode.Open, FileAccess.Read);
                using var temp = new Bitmap(fs);
                _currentImage = new Bitmap(temp); // Kilidi kaldırır
            }
            catch (Exception ex)
            {
                MessageBox.Show("Resim yüklenemedi: " + ex.Message);
                return;
            }

            // Load existing annotations
            string fileName = GetDatasetRelativePath(_imageFiles[_currentIndex]);
            var entry = _dataset.Images.FirstOrDefault(e => e.File == fileName);
            if (entry != null)
            {
                foreach (var box in entry.Annotations.Boxes)
                {
                    if (box.Length >= 5)
                        _annotations.Add(new BBoxAnnotation
                        {
                            ClassId = (int)box[0],
                            Cx = box[1],
                            Cy = box[2],
                            W = box[3],
                            H = box[4]
                        });
                }
            }

            // Sync thumbnail selection (find item by Tag, not by direct index)
            // Çünkü thumbnail listesi filtrelenmiş olabilir
            var itemToSelect = lvThumbnails.Items.Cast<ListViewItem>()
                .FirstOrDefault(item => item.Tag is int t && t == _currentIndex);
            if (itemToSelect != null)
            {
                itemToSelect.Selected = true;
                itemToSelect.EnsureVisible();
            }

            FitZoom();
            RefreshAnnotationList();
            UpdateStatus();
        }

        private void btnPrev_Click(object sender, EventArgs e) => Navigate(-1);
        private void btnNext_Click(object sender, EventArgs e) => Navigate(+1);

        private void Navigate(int delta)
        {
            if (_imageFiles.Count == 0) return;
            SaveCurrentAnnotations();
            _currentIndex = Math.Clamp(_currentIndex + delta, 0, _imageFiles.Count - 1);
            LoadCurrentImage();
            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            btnPrev.Enabled = _currentIndex > 0;
            btnNext.Enabled = _currentIndex >= 0 && _currentIndex < _imageFiles.Count - 1;
        }

        private void UpdateStatus()
        {
            if (_currentIndex < 0 || _imageFiles.Count == 0)
            {
                lblStatus.Text = "Hazır";
                return;
            }
            string name = Path.GetFileName(_imageFiles[_currentIndex]);
            lblStatus.Text = $"{name}   ({_currentIndex + 1} / {_imageFiles.Count})   [{_annotations.Count} bbox]";
        }

        // ────────────────────────────────────────────────────────────────────
        // Class management
        // ────────────────────────────────────────────────────────────────────

        private void RefreshClassCombo()
        {
            // ToolStripComboBox focus kaybında SelectedIndex -1 verir,
            // bu yüzden seçimi _selectedClassId field'ında saklarız
            int prev = _selectedClassId;
            cmbClass.SelectedIndexChanged -= cmbClass_SelectedIndexChanged;
            cmbClass.Items.Clear();
            for (int i = 0; i < _classes.Count; i++)
                cmbClass.Items.Add($"{i}: {_classes[i]}");
            int safeIdx = (prev >= 0 && prev < _classes.Count) ? prev : 0;
            cmbClass.SelectedIndex = safeIdx;
            _selectedClassId = safeIdx;
            cmbClass.SelectedIndexChanged += cmbClass_SelectedIndexChanged;
        }

        private void cmbClass_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbClass.SelectedIndex >= 0)
                _selectedClassId = cmbClass.SelectedIndex;
        }

        private void btnAddClass_Click(object sender, EventArgs e)
        {
            string? name = PromptInput("Yeni class adı:", "Class Ekle");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            if (!_classes.Contains(name))
            {
                _classes.Add(name);
                RefreshClassCombo();
            }
            int idx = _classes.IndexOf(name);
            cmbClass.SelectedIndex = idx;
            _selectedClassId = idx;
        }

        private int SelectedClassId => _selectedClassId;

        // ────────────────────────────────────────────────────────────────────
        // Zoom
        // ────────────────────────────────────────────────────────────────────

        private void FitZoom()
        {
            if (_currentImage == null) return;
            float zw = (float)pnlCanvas.ClientSize.Width / _currentImage.Width;
            float zh = (float)pnlCanvas.ClientSize.Height / _currentImage.Height;
            _zoom = Math.Min(zw, zh);
            if (_zoom < ZoomMin) _zoom = ZoomMin;
            UpdateCanvas();
        }

        private void btnZoomIn_Click(object sender, EventArgs e) => ApplyZoom(_zoom + ZoomStep);
        private void btnZoomOut_Click(object sender, EventArgs e) => ApplyZoom(_zoom - ZoomStep);
        private void btnZoomFit_Click(object sender, EventArgs e) => FitZoom();

        private void ApplyZoom(float z, PointF? anchor = null)
        {
            _zoom = Math.Clamp(z, ZoomMin, ZoomMax);
            UpdateCanvas();
        }

        private void pnlCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e is HandledMouseEventArgs he) he.Handled = true; // Normal scroll'u durdurur
                float delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
                ApplyZoom(_zoom + delta);
            }
        }

        private void UpdateCanvas()
        {
            if (_currentImage == null)
            {
                picCanvas.Size = pnlCanvas.ClientSize;
                picCanvas.Invalidate();
                lblZoomPct.Text = "—";
                return;
            }

            int w = (int)(_currentImage.Width * _zoom);
            int h = (int)(_currentImage.Height * _zoom);
            picCanvas.Size = new Size(Math.Max(w, pnlCanvas.ClientSize.Width),
                                      Math.Max(h, pnlCanvas.ClientSize.Height));
            picCanvas.Location = Point.Empty;
            picCanvas.Invalidate();
            lblZoomPct.Text = $"{(int)(_zoom * 100)}%";
        }

        // ─── Coordinate helpers ───────────────────────────────────────────────

        /// Image pixel rect on the canvas at current zoom
        private RectangleF ImageRect()
        {
            if (_currentImage == null) return RectangleF.Empty;
            return new RectangleF(0, 0, _currentImage.Width * _zoom, _currentImage.Height * _zoom);
        }

        /// YOLO bbox → canvas rect
        private RectangleF YoloToCanvas(BBoxAnnotation a)
        {
            if (_currentImage == null) return RectangleF.Empty;
            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            float x = (a.Cx - a.W / 2f) * iw;
            float y = (a.Cy - a.H / 2f) * ih;
            float w = a.W * iw;
            float h = a.H * ih;
            return new RectangleF(x, y, w, h);
        }

        /// Canvas point → YOLO normalize
        private PointF CanvasToYolo(PointF p)
        {
            if (_currentImage == null) return PointF.Empty;
            return new PointF(
                p.X / (_currentImage.Width * _zoom),
                p.Y / (_currentImage.Height * _zoom));
        }

        /// Canvas rect → BBoxAnnotation
        private BBoxAnnotation RectToAnnotation(RectangleF r, int classId)
        {
            if (_currentImage == null) return new BBoxAnnotation();
            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            // Normalize
            float cx = (r.Left + r.Width / 2f) / iw;
            float cy = (r.Top + r.Height / 2f) / ih;
            float w = r.Width / iw;
            float h = r.Height / ih;
            return new BBoxAnnotation { ClassId = classId, Cx = cx, Cy = cy, W = w, H = h };
        }

        // ────────────────────────────────────────────────────────────────────
        // Paint
        // ────────────────────────────────────────────────────────────────────

        private void picCanvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.DimGray);

            if (_currentImage == null) return;

            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            e.Graphics.DrawImage(_currentImage, 0, 0, iw, ih);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw saved annotations
            for (int i = 0; i < _annotations.Count; i++)
            {
                var a = _annotations[i];
                var rect = YoloToCanvas(a);
                bool selected = i == _selectedAnnot;

                using var pen = new Pen(ClassColor(a.ClassId), selected ? 2.5f : 1.5f);
                using var fill = new SolidBrush(Color.FromArgb(selected ? 50 : 25, ClassColor(a.ClassId)));

                e.Graphics.FillRectangle(fill, rect);
                e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                // Class label
                string lbl = a.ClassId < _classes.Count ? $"{a.ClassId}:{_classes[a.ClassId]}" : $"{a.ClassId}";
                using var bgBrush = new SolidBrush(Color.FromArgb(180, ClassColor(a.ClassId)));
                using var font = new Font("Segoe UI", 8, FontStyle.Bold);
                var sz = e.Graphics.MeasureString(lbl, font);
                e.Graphics.FillRectangle(bgBrush, rect.X, rect.Y - sz.Height, sz.Width, sz.Height);
                e.Graphics.DrawString(lbl, font, Brushes.Black, rect.X, rect.Y - sz.Height);

                if (selected) DrawHandles(e.Graphics, rect);
            }

            // Draw in-progress rectangle
            if (_drawMode == DrawMode.Drawing && _drawRect.Width > 2 && _drawRect.Height > 2)
            {
                using var dashPen = new Pen(Color.Yellow, 1.5f) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(dashPen, _drawRect.X, _drawRect.Y, _drawRect.Width, _drawRect.Height);
            }
        }

        private void DrawHandles(Graphics g, RectangleF r)
        {
            var handles = GetHandleRects(r);
            foreach (var h in handles.Values)
                g.FillRectangle(Brushes.White, h);
            foreach (var h in handles.Values)
                g.DrawRectangle(Pens.Black, h.X, h.Y, h.Width, h.Height);
        }

        private Dictionary<Handle, RectangleF> GetHandleRects(RectangleF r)
        {
            float hs = HandleSize;
            float halfH = hs / 2f;
            float cx = r.Left + r.Width / 2f;
            float cy = r.Top + r.Height / 2f;
            return new Dictionary<Handle, RectangleF>
            {
                [Handle.TopLeft] = new(r.Left - halfH, r.Top - halfH, hs, hs),
                [Handle.TopRight] = new(r.Right - halfH, r.Top - halfH, hs, hs),
                [Handle.BottomLeft] = new(r.Left - halfH, r.Bottom - halfH, hs, hs),
                [Handle.BottomRight] = new(r.Right - halfH, r.Bottom - halfH, hs, hs),
                [Handle.Top] = new(cx - halfH, r.Top - halfH, hs, hs),
                [Handle.Bottom] = new(cx - halfH, r.Bottom - halfH, hs, hs),
                [Handle.Left] = new(r.Left - halfH, cy - halfH, hs, hs),
                [Handle.Right] = new(r.Right - halfH, cy - halfH, hs, hs),
            };
        }

        // ────────────────────────────────────────────────────────────────────
        // Mouse interaction
        // ────────────────────────────────────────────────────────────────────

        private void picCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_currentImage == null || e.Button != MouseButtons.Left) return;

            pnlCanvas.Focus(); // picCanvas.Focus() AutoScroll'ı resetliyordu (scroll zıplaması)
            PointF pt = e.Location;

            // 1. Check selected bbox handles first
            if (_selectedAnnot >= 0 && _selectedAnnot < _annotations.Count)
            {
                var rect = YoloToCanvas(_annotations[_selectedAnnot]);
                var handles = GetHandleRects(rect);
                foreach (var kv in handles)
                {
                    if (kv.Value.Contains(pt))
                    {
                        _drawMode = DrawMode.Resizing;
                        _activeHandle = kv.Key;
                        _originalRect = rect;
                        _dragStart = pt;
                        return;
                    }
                }

                // 2. Check if inside selected bbox → move
                if (rect.Contains(pt))
                {
                    _drawMode = DrawMode.Moving;
                    _dragStart = pt;
                    _moveOffset = new PointF(pt.X - rect.X, pt.Y - rect.Y);
                    return;
                }
            }

            // 3. Check if any other bbox clicked → select it
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                var rect = YoloToCanvas(_annotations[i]);
                if (rect.Contains(pt))
                {
                    _selectedAnnot = i;
                    RefreshAnnotationList();
                    picCanvas.Invalidate();
                    _drawMode = DrawMode.Moving;
                    _dragStart = pt;
                    _moveOffset = new PointF(pt.X - rect.X, pt.Y - rect.Y);
                    return;
                }
            }

            // 4. Start drawing new bbox
            _selectedAnnot = -1;
            RefreshAnnotationList();
            _drawMode = DrawMode.Drawing;
            _dragStart = pt;
            _drawRect = new RectangleF(pt.X, pt.Y, 0, 0);
        }

        private void picCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentImage == null) return;

            PointF pt = e.Location;
            RectangleF imgRect = ImageRect();

            // Clamp to image bounds
            pt.X = Math.Clamp(pt.X, 0, imgRect.Width);
            pt.Y = Math.Clamp(pt.Y, 0, imgRect.Height);

            if (_drawMode == DrawMode.Drawing)
            {
                float x = Math.Min(pt.X, _dragStart.X);
                float y = Math.Min(pt.Y, _dragStart.Y);
                float w = Math.Abs(pt.X - _dragStart.X);
                float h = Math.Abs(pt.Y - _dragStart.Y);
                _drawRect = new RectangleF(x, y, w, h);
                picCanvas.Invalidate();
            }
            else if (_drawMode == DrawMode.Moving && _selectedAnnot >= 0)
            {
                var a = _annotations[_selectedAnnot];
                var rect = YoloToCanvas(a);
                float newX = pt.X - _moveOffset.X;
                float newY = pt.Y - _moveOffset.Y;
                // Clamp
                newX = Math.Clamp(newX, 0, imgRect.Width - rect.Width);
                newY = Math.Clamp(newY, 0, imgRect.Height - rect.Height);
                var newRect = new RectangleF(newX, newY, rect.Width, rect.Height);
                var newA = RectToAnnotation(newRect, a.ClassId);
                a.Cx = newA.Cx; a.Cy = newA.Cy;
                picCanvas.Invalidate();
            }
            else if (_drawMode == DrawMode.Resizing && _selectedAnnot >= 0)
            {
                var r = _originalRect;
                float dx = pt.X - _dragStart.X;
                float dy = pt.Y - _dragStart.Y;

                float left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom;

                switch (_activeHandle)
                {
                    case Handle.TopLeft: left += dx; top += dy; break;
                    case Handle.TopRight: right += dx; top += dy; break;
                    case Handle.BottomLeft: left += dx; bottom += dy; break;
                    case Handle.BottomRight: right += dx; bottom += dy; break;
                    case Handle.Top: top += dy; break;
                    case Handle.Bottom: bottom += dy; break;
                    case Handle.Left: left += dx; break;
                    case Handle.Right: right += dx; break;
                }

                // Ensure min size
                if (right - left < 4) right = left + 4;
                if (bottom - top < 4) bottom = top + 4;

                // Clamp to image
                left = Math.Max(0, left);
                top = Math.Max(0, top);
                right = Math.Min(imgRect.Width, right);
                bottom = Math.Min(imgRect.Height, bottom);

                var newRect = RectangleF.FromLTRB(left, top, right, bottom);
                var a = _annotations[_selectedAnnot];
                var newA = RectToAnnotation(newRect, a.ClassId);
                a.Cx = newA.Cx; a.Cy = newA.Cy; a.W = newA.W; a.H = newA.H;

                // Update cursor
                _annotations[_selectedAnnot] = a;
                picCanvas.Invalidate();
                return;
            }

            // Cursor hint for selected bbox
            UpdateCursor(pt);
        }

        private void UpdateCursor(PointF pt)
        {
            if (_selectedAnnot < 0) return;
            var rect = YoloToCanvas(_annotations[_selectedAnnot]);
            var handles = GetHandleRects(rect);

            foreach (var kv in handles)
            {
                if (!kv.Value.Contains(pt)) continue;
                picCanvas.Cursor = kv.Key switch
                {
                    Handle.TopLeft or Handle.BottomRight => Cursors.SizeNWSE,
                    Handle.TopRight or Handle.BottomLeft => Cursors.SizeNESW,
                    Handle.Top or Handle.Bottom => Cursors.SizeNS,
                    Handle.Left or Handle.Right => Cursors.SizeWE,
                    _ => Cursors.Cross
                };
                return;
            }

            picCanvas.Cursor = rect.Contains(pt) ? Cursors.SizeAll : Cursors.Cross;
        }

        private void picCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            bool modified = false;
            if (_drawMode == DrawMode.Drawing)
            {
                if (_drawRect.Width > 5 && _drawRect.Height > 5)
                {
                    var a = RectToAnnotation(_drawRect, SelectedClassId);
                    _annotations.Add(a);
                    _selectedAnnot = _annotations.Count - 1;
                    RefreshAnnotationList();
                    UpdateStatus();
                    modified = true;
                }
                _drawRect = RectangleF.Empty;
            }
            else if (_drawMode == DrawMode.Moving || _drawMode == DrawMode.Resizing)
            {
                RefreshAnnotationList();
                modified = true;
            }

            _drawMode = DrawMode.None;
            _activeHandle = Handle.None;
            picCanvas.Invalidate();

            if (modified)
                SaveCurrentAnnotations();
        }

        // ────────────────────────────────────────────────────────────────────
        // Annotation list (right panel)
        // ────────────────────────────────────────────────────────────────────

        private bool _suppressAnnotSel = false;

        private void RefreshAnnotationList()
        {
            _suppressAnnotSel = true;
            lvAnnotations.BeginUpdate();

            // Sadece gerekiyorsa ekle/sil (tamamen clear yapmaktan kaçınarak flicker'ı önler)
            while (lvAnnotations.Items.Count > _annotations.Count)
                lvAnnotations.Items.RemoveAt(lvAnnotations.Items.Count - 1);

            while (lvAnnotations.Items.Count < _annotations.Count)
            {
                var item = new ListViewItem("");
                for (int i = 0; i < 5; i++) item.SubItems.Add("");
                lvAnnotations.Items.Add(item);
            }

            for (int i = 0; i < _annotations.Count; i++)
            {
                var a = _annotations[i];
                string cls = a.ClassId < _classes.Count ? _classes[a.ClassId] : a.ClassId.ToString();

                var item = lvAnnotations.Items[i];
                item.Text = (i + 1).ToString();
                item.SubItems[1].Text = cls;
                item.SubItems[2].Text = a.Cx.ToString("F3");
                item.SubItems[3].Text = a.Cy.ToString("F3");
                item.SubItems[4].Text = a.W.ToString("F3");
                item.SubItems[5].Text = a.H.ToString("F3");

                Color bgColor = Color.FromArgb(30, ClassColor(a.ClassId));
                if (item.BackColor != bgColor) item.BackColor = bgColor;
            }

            // Seçimi ayarla
            for (int i = 0; i < lvAnnotations.Items.Count; i++)
            {
                bool shouldBeSelected = (i == _selectedAnnot);
                if (lvAnnotations.Items[i].Selected != shouldBeSelected)
                    lvAnnotations.Items[i].Selected = shouldBeSelected;
            }

            if (_selectedAnnot >= 0 && _selectedAnnot < lvAnnotations.Items.Count)
            {
                lvAnnotations.Items[_selectedAnnot].EnsureVisible();
            }

            lvAnnotations.EndUpdate();
            _suppressAnnotSel = false;
        }

        private void lvAnnotations_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressAnnotSel) return;
            if (lvAnnotations.SelectedIndices.Count == 0) return;
            _selectedAnnot = lvAnnotations.SelectedIndices[0];
            picCanvas.Invalidate();
        }

        // ── Delete selected annotation ────────────────────────────────────────
        private void btnDeleteAnnotation_Click(object sender, EventArgs e) => DeleteSelected();

        private void DeleteSelected()
        {
            if (_selectedAnnot < 0 || _selectedAnnot >= _annotations.Count) return;
            _annotations.RemoveAt(_selectedAnnot);
            _selectedAnnot = Math.Min(_selectedAnnot, _annotations.Count - 1);
            RefreshAnnotationList();
            picCanvas.Invalidate();
            UpdateStatus();
            SaveCurrentAnnotations(); // Auto-save on deletion
            
        }

        // ── Edit selected annotation ──────────────────────────────────────────
        private void btnEditAnnotation_Click(object sender, EventArgs e)
        {
            if (_selectedAnnot < 0 || _selectedAnnot >= _annotations.Count)
            {
                MessageBox.Show("Lütfen önce bir annotation seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var a = _annotations[_selectedAnnot];
            bool changed = ShowAnnotationEditor(a);
            if (changed)
            {
                RefreshAnnotationList();
                picCanvas.Invalidate();
                UpdateStatus();
                SaveCurrentAnnotations(); // Auto-save on edit
            }
        }

        private bool ShowAnnotationEditor(BBoxAnnotation a)
        {
            using var dlg = new Form
            {
                Text = "Annotation Düzenle",
                Size = new Size(340, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            var cmbEditClass = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            for (int i = 0; i < _classes.Count; i++) cmbEditClass.Items.Add($"{i}: {_classes[i]}");
            cmbEditClass.SelectedIndex = Math.Clamp(a.ClassId, 0, _classes.Count - 1);

            var edCx = new TextBox { Dock = DockStyle.Fill, Text = a.Cx.ToString("F6") };
            var edCy = new TextBox { Dock = DockStyle.Fill, Text = a.Cy.ToString("F6") };
            var edW = new TextBox { Dock = DockStyle.Fill, Text = a.W.ToString("F6") };
            var edH = new TextBox { Dock = DockStyle.Fill, Text = a.H.ToString("F6") };

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            void AddRow(string label, Control ctrl, int row)
            {
                tbl.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
                tbl.Controls.Add(ctrl, 1, row);
            }

            AddRow("Class:", cmbEditClass, 0);
            AddRow("Cx:", edCx, 1);
            AddRow("Cy:", edCy, 2);
            AddRow("W:", edW, 3);
            AddRow("H:", edH, 4);

            var pnlBtn = new Panel { Dock = DockStyle.Fill };
            pnlBtn.Controls.Add(btnCancel);
            pnlBtn.Controls.Add(btnOk);
            btnOk.Dock = DockStyle.Right;
            btnOk.Width = 90;
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Width = 70;

            tbl.Controls.Add(pnlBtn, 0, 6);
            tbl.SetColumnSpan(pnlBtn, 2);

            dlg.Controls.Add(tbl);
            dlg.KeyPreview = true;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                int cls = cmbEditClass.SelectedIndex;

                var nfi = System.Globalization.CultureInfo.InvariantCulture;
                var ns = System.Globalization.NumberStyles.Float;
                float cx = 0, cy = 0, w = 0, h = 0;
                bool okCx = float.TryParse(edCx.Text, ns, nfi, out cx);
                bool okCy = float.TryParse(edCy.Text, ns, nfi, out cy);
                bool okW = float.TryParse(edW.Text, ns, nfi, out w);
                bool okH = float.TryParse(edH.Text, ns, nfi, out h);
                bool ok = okCx & okCy & okW & okH;

                if (!ok) { MessageBox.Show("Geçersiz sayısal değer.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }

                // Clamp 0-1
                a.ClassId = cls;
                a.Cx = Math.Clamp(cx, 0f, 1f);
                a.Cy = Math.Clamp(cy, 0f, 1f);
                a.W = Math.Clamp(w, 0f, 1f);
                a.H = Math.Clamp(h, 0f, 1f);
                return true;
            }
            return false;
        }

        // ── Save now button ───────────────────────────────────────────────────
        private void btnSaveNow_Click(object sender, EventArgs e)
        {
            SaveCurrentAnnotations();
        }

        // ────────────────────────────────────────────────────────────────────
        // Auto-Label (LicensePlateDetector)
        // ────────────────────────────────────────────────────────────────────

        // Detector is lazy-loaded; path is persisted across images
        private ALPR.Detection.LicensePlateDetector? _autoDetector;
        private string _autoDetectorPath = "";
        private const string DefaultDetectorModel = "models/LicencePlateDetection_Gpu.onnx";

        private ALPR.Detection.LicensePlateDetector? EnsureDetector()
        {
            // Try default path if not yet chosen
            if (_autoDetectorPath == "")
                _autoDetectorPath = DefaultDetectorModel;

            // Model dosyası yoksa kullanıcıdan seç
            if (!File.Exists(_autoDetectorPath))
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Plaka Tespit Modeli Seçin",
                    Filter = "ONNX Model|*.onnx|Tüm Dosyalar|*.*",
                    InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models")
                };
                if (ofd.ShowDialog(this) != DialogResult.OK) return null;
                _autoDetectorPath = ofd.FileName;
            }

            // Zaten yüklü ve aynı dosyaysa tekrar yükleme
            if (_autoDetector != null) return _autoDetector;

            try
            {
                lblStatus.Text = "🔧 Model yükleniyor...";
                Application.DoEvents();

                _autoDetector = new ALPR.Detection.LicensePlateDetector(_autoDetectorPath, useGpu: false);
                lblSaved.Text = "✓ Model yüklendi";
                lblSaved.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Model yüklenemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _autoDetectorPath = "";
                _autoDetector = null;
                return null;
            }

            return _autoDetector;
        }

        private void btnAutoLabel_Click(object sender, EventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Önce bir resim yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var detector = EnsureDetector();
            if (detector == null) return;

            AutoLabelImage(_currentImage, confirmOverwrite: true);
            RefreshAnnotationList();
            picCanvas.Invalidate();
            UpdateStatus();
            SaveCurrentAnnotations();
        }

        private void btnAutoLabelAll_Click(object sender, EventArgs e)
        {
            if (_imageFiles.Count == 0)
            {
                MessageBox.Show("Önce bir klasör seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var detector = EnsureDetector();
            if (detector == null) return;

            var res = MessageBox.Show(
                $"{_imageFiles.Count} resim otomatik etiketlenecek.\n" +
                "Var olan annotation'lar korunacak (tespit edilenler eklenecek).\n\nDevam?",
                "Tümünü Otomatik Etiketle",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes) return;

            // Önce mevcut resmi kaydet
            SaveCurrentAnnotations();

            int capturedClassId = SelectedClassId;
            int totalAdded = 0;
            pbAutoLabel.Visible = true;
            pbAutoLabel.Maximum = _imageFiles.Count;
            pbAutoLabel.Value = 0;

            for (int i = 0; i < _imageFiles.Count; i++)
            {
                lblStatus.Text = $"Otomatik etiketleniyor: {i + 1}/{_imageFiles.Count} — {Path.GetFileName(_imageFiles[i])}";
                pbAutoLabel.Value = i + 1;
                Application.DoEvents();

                try
                {
                    using var fs = new FileStream(_imageFiles[i], FileMode.Open, FileAccess.Read);
                    using var temp = new Bitmap(fs);
                    using var bmp = new Bitmap(temp); // Kilitlenmeyi önler

                    string fname = GetDatasetRelativePath(_imageFiles[i]);

                    // Dataset'teki mevcut annotation'ları yükle
                    var entry = _dataset.Images.FirstOrDefault(e => e.File == fname);
                    var existingAnnots = entry != null
                        ? entry.Annotations.Boxes.Select(b => new BBoxAnnotation
                        {
                            ClassId = (int)b[0],
                            Cx = b[1],
                            Cy = b[2],
                            W = b[3],
                            H = b[4]
                        }).ToList()
                        : new List<BBoxAnnotation>();

                    int added = RunDetectorOnBitmap(bmp, existingAnnots, appendOnly: true, classId: capturedClassId);
                    totalAdded += added;

                    // Dataset'e yaz
                    if (entry == null)
                    {
                        entry = new ImageEntry
                        {
                            File = fname,
                            Width = bmp.Width,
                            Height = bmp.Height
                        };
                        _dataset.Images.Add(entry);
                    }
                    entry.Annotations.Boxes = existingAnnots
                        .Select(a => new float[] { a.ClassId, a.Cx, a.Cy, a.W, a.H })
                        .ToList();
                }
                catch (Exception ex)
                {
                    lblSaved.Text = $"⚠ {Path.GetFileName(_imageFiles[i])}: {ex.Message}";
                    lblSaved.ForeColor = Color.OrangeRed;
                }
            }

            SaveDataset();

            // Şu an açık resmi yeniden yükle (annotation'lar güncellenmiş olabilir)
            if (_currentIndex >= 0)
            {
                string curFile = GetDatasetRelativePath(_imageFiles[_currentIndex]);
                var curEntry = _dataset.Images.FirstOrDefault(e => e.File == curFile);
                _annotations = curEntry != null
                    ? curEntry.Annotations.Boxes.Select(b => new BBoxAnnotation
                    {
                        ClassId = (int)b[0],
                        Cx = b[1],
                        Cy = b[2],
                        W = b[3],
                        H = b[4]
                    }).ToList()
                    : new List<BBoxAnnotation>();

                RefreshAnnotationList();
                picCanvas.Invalidate();
            }

            pbAutoLabel.Visible = false;

            UpdateStatus();
            MessageBox.Show(
                $"Tamamlandı! {_imageFiles.Count} resim işlendi, toplam {totalAdded} yeni bbox eklendi.",
                "Otomatik Etiketleme Bitti", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Mevcut açık resme detector çalıştırır. confirmOverwrite=true ise var olan annotation varsa sorar.
        /// </summary>
        private void AutoLabelImage(Bitmap bmp, bool confirmOverwrite)
        {
            if (confirmOverwrite && _annotations.Count > 0)
            {
                var res = MessageBox.Show(
                    $"Bu resimde zaten {_annotations.Count} annotation var.\n" +
                    "Tespit edilen yeni bbox'lar mevcut olanların üzerine mi eklensin?\n\n" +
                    "[Evet] = Ekle (var olanları koru)\n[Hayır] = Hepsini sil, sadece yenileri ekle",
                    "Annotation Mevcut", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (res == DialogResult.Cancel) return;
                if (res == DialogResult.No) _annotations.Clear();
            }

            RunDetectorOnBitmap(bmp, _annotations, appendOnly: true);
        }

        /// <summary>
        /// Detector çalıştırır, class 0 (plate) olarak tespit edilen bbox'ları annotations listesine ekler.
        /// Geri dönüş: eklenen bbox sayısı.
        /// </summary>
        private int RunDetectorOnBitmap(Bitmap bmp, List<BBoxAnnotation> annotations, bool appendOnly, int classId = -1)
        {
            if (_autoDetector == null) return 0;

            if (classId < 0) classId = SelectedClassId;

            // OnnxRuntime Format24bppRgb bekler; diğer formatları dönüştür
            Bitmap inputBmp;
            bool needsDispose = false;
            if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                inputBmp = new Bitmap(bmp.Width, bmp.Height,
                                          System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                needsDispose = true;
                using var g = Graphics.FromImage(inputBmp);
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
            }
            else
            {
                inputBmp = bmp;
            }

            try
            {
                var result = _autoDetector.Detect(inputBmp, 0.35f, true, 0.45f);
                if (result.Detections.Count == 0) return 0;

                int added = 0;
                foreach (var det in result.Detections)
                {
                    // Piksel → YOLO normalize
                    float cx = (det.X + det.Width / 2f) / inputBmp.Width;
                    float cy = (det.Y + det.Height / 2f) / inputBmp.Height;
                    float w = det.Width / (float)inputBmp.Width;
                    float h = det.Height / (float)inputBmp.Height;

                    cx = Math.Clamp(cx, 0f, 1f);
                    cy = Math.Clamp(cy, 0f, 1f);
                    w = Math.Clamp(w, 0f, 1f);
                    h = Math.Clamp(h, 0f, 1f);

                    // Duplicate kontrolü: mevcut annotation'larla örtüşüyorsa (IoU > 0.5) ekleme
                    bool duplicate = annotations.Any(a =>
                    {
                        float iou = ComputeIoU(a.Cx, a.Cy, a.W, a.H, cx, cy, w, h);
                        return iou > 0.5f;
                    });

                    if (!duplicate)
                    {
                        annotations.Add(new BBoxAnnotation
                        {
                            ClassId = classId,
                            Cx = cx,
                            Cy = cy,
                            W = w,
                            H = h
                        });
                        added++;
                    }
                }
                return added;
            }
            finally
            {
                if (needsDispose) inputBmp.Dispose();
            }
        }

        private static float ComputeIoU(float cx1, float cy1, float w1, float h1,
                                         float cx2, float cy2, float w2, float h2)
        {
            float x1l = cx1 - w1 / 2f, x1r = cx1 + w1 / 2f;
            float y1t = cy1 - h1 / 2f, y1b = cy1 + h1 / 2f;
            float x2l = cx2 - w2 / 2f, x2r = cx2 + w2 / 2f;
            float y2t = cy2 - h2 / 2f, y2b = cy2 + h2 / 2f;

            float ix = Math.Max(0, Math.Min(x1r, x2r) - Math.Max(x1l, x2l));
            float iy = Math.Max(0, Math.Min(y1b, y2b) - Math.Max(y1t, y2t));
            float inter = ix * iy;
            if (inter <= 0) return 0f;

            float union = w1 * h1 + w2 * h2 - inter;
            return union <= 0 ? 0f : inter / union;
        }



        // ────────────────────────────────────────────────────────────────────
        // Keyboard shortcuts
        // ────────────────────────────────────────────────────────────────────

        private void btnPlateList_Click(object sender, EventArgs e)
        {
            if (_dataset == null || string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(_datasetPath))
            {
                MessageBox.Show("Lütfen önce veri seti klasörünü seçin.", "Uyarı",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveCurrentAnnotations();

            string folderName = new DirectoryInfo(_folderPath).Name;
            int activeClassId = _classes.FindIndex(c => folderName.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
            if (activeClassId == -1)
            {
                activeClassId = _classes.FindIndex(c => c.IndexOf(folderName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (activeClassId == -1)
            {
                activeClassId = cmbClass.SelectedIndex;
            }

            using var frm = new FullPlateList(_folderPath, _dataset, _datasetPath, _classes, activeClassId);
            frm.ShowDialog(this);
            
            // Plaka listesinde değişiklik yapılmış olabilir, mevcut resmi yeniden yükle
            LoadCurrentImage();
        }

        private void btnTemizle_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_folderPath) || _dataset == null || _imageFiles.Count == 0) return;

            string folderName = new DirectoryInfo(_folderPath).Name;
            var result = MessageBox.Show(
                $"Dikkat: Sadece şu an seçili olan '{folderName}' klasöründeki MECUT TÜM ETİKETLER kalıcı olarak silinecek.\n\nOnaylıyor musunuz?",
                "Klasör Etiketlerini Temizle",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            int clearedCount = 0;

            foreach (var imgFile in _imageFiles)
            {
                string relPath = GetDatasetRelativePath(imgFile);
                var entry = _dataset.Images.FirstOrDefault(x => x.File == relPath);
                
                if (entry != null && entry.Annotations?.Boxes != null && entry.Annotations.Boxes.Count > 0)
                {
                    clearedCount += entry.Annotations.Boxes.Count;
                    entry.Annotations.Boxes.Clear();
                }
            }

            // Şu an açık olan ekranı da temizle
            if (_currentImage != null)
            {
                _annotations.Clear();
                _selectedAnnot = -1;
                RefreshAnnotationList();
                picCanvas.Invalidate();
                UpdateStatus();
            }

            SaveDataset();

            MessageBox.Show($"Toplam {clearedCount} adet etiket silindi.", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImageLabeling_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                DeleteSelected();
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left: Navigate(-1); e.Handled = true; break;
                case Keys.Right: Navigate(+1); e.Handled = true; break;

                case Keys.Add:
                case Keys.Oemplus: ApplyZoom(_zoom + ZoomStep); e.Handled = true; break;

                case Keys.Subtract:
                case Keys.OemMinus: ApplyZoom(_zoom - ZoomStep); e.Handled = true; break;

                case Keys.D0 when e.Control: FitZoom(); e.Handled = true; break;

                case Keys.S when e.Control: SaveCurrentAnnotations(); e.Handled = true; break;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Form closing
        // ────────────────────────────────────────────────────────────────────

        private void ImageLabeling_FormClosing(object sender, FormClosingEventArgs e)
        {
            _thumbCts?.Cancel();
            SaveCurrentAnnotations();
            _currentImage?.Dispose();
            _autoDetector?.Dispose();
        }

        // ────────────────────────────────────────────────────────────────────
        // Utility
        // ────────────────────────────────────────────────────────────────────

        private static string? PromptInput(string message, string title)
        {
            using var dlg = new Form
            {
                Text = title,
                Size = new Size(320, 130),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lbl = new Label { Text = message, Left = 10, Top = 12, AutoSize = true };
            var txt = new TextBox { Left = 10, Top = 34, Width = 280 };
            var ok = new Button { Text = "Tamam", Left = 120, Top = 64, Width = 80, DialogResult = DialogResult.OK };
            var ca = new Button { Text = "İptal", Left = 210, Top = 64, Width = 80, DialogResult = DialogResult.Cancel };
            dlg.AcceptButton = ok;
            dlg.CancelButton = ca;
            dlg.Controls.AddRange(new Control[] { lbl, txt, ok, ca });
            return dlg.ShowDialog() == DialogResult.OK ? txt.Text : null;
        }
    } // End of Form

    // ─────────────────────────────────────────────────────────────────────────
    // Data models
    // ─────────────────────────────────────────────────────────────────────────

    public class BBoxAnnotation
    {
        public int ClassId { get; set; }
        public float Cx { get; set; }   // YOLO normalize 0-1
        public float Cy { get; set; }
        public float W { get; set; }
        public float H { get; set; }
    }

    public class ImageEntry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "image";

        [JsonPropertyName("file")]
        public string File { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("split")]
        public string Split { get; set; } = "train";

        [JsonPropertyName("annotations")]
        public AnnotationBlock Annotations { get; set; } = new();
    }

    public class AnnotationBlock
    {
        [JsonPropertyName("boxes")]
        public List<float[]> Boxes { get; set; } = new();
    }

    public class DatasetRoot
    {
        [JsonPropertyName("classes")]
        public List<string> Classes { get; set; } = new();

        [JsonPropertyName("images")]
        public List<ImageEntry> Images { get; set; } = new();
    }
}