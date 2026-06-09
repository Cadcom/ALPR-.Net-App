using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace ALPR
{
    public partial class ImageLabeling : Form
    {
        // ── State ────────────────────────────────────────────────────────────
        private string _folderPath = "";
        private string _datasetPath = "";
        private List<string> _imageFiles = new();
        private List<string> _classes = new() { "plate" };
        private int _selectedClassId = 0;
        private DatasetRoot _dataset = new();

        private int _currentIndex = -1;
        private Bitmap? _currentImage = null;

        private List<BBoxAnnotation> _annotations = new();
        private int _selectedAnnot = -1;

        private string _thumbFilter = string.Empty;
        private System.Threading.CancellationTokenSource? _thumbCts;

        // ── Zoom ─────────────────────────────────────────────────────────────
        private float _zoom = 1.0f;
        private const float ZoomStep = 0.15f;
        private const float ZoomMin = 0.05f;
        private const float ZoomMax = 10.0f;

        // ── Drawing state ─────────────────────────────────────────────────────
        private enum DrawMode { None, Drawing, Moving, Resizing }
        private DrawMode _drawMode = DrawMode.None;

        private PointF _dragStart;
        private RectangleF _drawRect;
        private PointF _moveOffset;
        private RectangleF _originalRect;

        private enum Handle
        {
            None,
            Top, Bottom, Left, Right,
            TopLeft, TopRight, BottomLeft, BottomRight
        }
        private Handle _activeHandle = Handle.None;
        private const int HandleSize = 7;

        // ── Class color palette ───────────────────────────────────────────────
        private static readonly Color[] _palette =
        {
            Color.FromArgb(255,  80,  80),
            Color.FromArgb( 80, 200,  80),
            Color.FromArgb( 80, 140, 255),
            Color.FromArgb(255, 200,  50),
            Color.FromArgb(220,  80, 220),
            Color.FromArgb( 80, 210, 210),
            Color.FromArgb(255, 140,  40),
            Color.FromArgb(160, 255, 100),
        };

        private static Color ClassColor(int id)
        {
            if (id < 0) return Color.White;
            if (id < _palette.Length) return _palette[id];

            var rnd = new Random(id * 73);
            return Color.FromArgb(255, rnd.Next(60, 240), rnd.Next(60, 240), rnd.Next(60, 240));
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public ImageLabeling()
        {
            InitializeComponent();
            cmbClass.SelectedIndexChanged += cmbClass_SelectedIndexChanged;
            RefreshClassCombo();
            UpdateNavButtons();
        }

        // ── Thumbnail filter ──────────────────────────────────────────────────
        private void txtThumbFilter_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                _thumbFilter = txtThumbFilter?.Text?.Trim() ?? string.Empty;
                BuildThumbnailsAsync();
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Folder / JSON
        // ─────────────────────────────────────────────────────────────────────

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
            lblFolderPath.ForeColor = SystemColors.ControlLightLight;

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".tif", ".tiff" };
            _imageFiles = Directory.GetFiles(path)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
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
            if (File.Exists(_datasetPath))
            {
                try
                {
                    _dataset = JsonSerializer.Deserialize<DatasetRoot>(
                                   File.ReadAllText(_datasetPath)) ?? new DatasetRoot();
                }
                catch { _dataset = new DatasetRoot(); }
            }
            else
            {
                _dataset = new DatasetRoot();
            }

            foreach (var cls in _dataset.Classes)
                if (!_classes.Contains(cls)) _classes.Add(cls);

            RefreshClassCombo();
        }

        private void SaveDataset()
        {
            if (_datasetPath == "") return;

            _dataset.Classes = new List<string>(_classes);
            File.WriteAllText(_datasetPath,
                JsonSerializer.Serialize(_dataset, new JsonSerializerOptions { WriteIndented = true }));

            lblSaved.Text = "✓ Kaydedildi";
            lblSaved.ForeColor = Color.Green;
        }

        /// <summary>
        /// Returns the dataset-relative path: "folderName/filename.jpg"
        /// </summary>
        private static string DatasetRelativePath(string fullPath) =>
            Path.Combine(new DirectoryInfo(Path.GetDirectoryName(fullPath)!).Name,
                         Path.GetFileName(fullPath));

        private void SaveCurrentAnnotations()
        {
            if (_currentIndex < 0 || _currentImage == null) return;

            var fileName = DatasetRelativePath(_imageFiles[_currentIndex]);
            var entry = _dataset.Images.FirstOrDefault(e => e.File == fileName)
                           ?? CreateAndRegisterEntry(fileName);

            entry.Width = _currentImage.Width;
            entry.Height = _currentImage.Height;
            entry.Annotations.Boxes = SerializeAnnotations(_annotations);

            SaveDataset();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Annotation serialization helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Converts float[][] boxes from a dataset entry to BBoxAnnotation list.</summary>
        private static List<BBoxAnnotation> ParseAnnotations(ImageEntry? entry)
        {
            if (entry == null) return new List<BBoxAnnotation>();

            return entry.Annotations.Boxes
                .Where(b => b.Length >= 5)
                .Select(b => new BBoxAnnotation
                {
                    ClassId = (int)b[0],
                    Cx = b[1],
                    Cy = b[2],
                    W = b[3],
                    H = b[4]
                })
                .ToList();
        }

        /// <summary>Converts a BBoxAnnotation list to float[][] for JSON storage.</summary>
        private static List<float[]> SerializeAnnotations(List<BBoxAnnotation> annots) =>
            annots.Select(a => new float[] { a.ClassId, a.Cx, a.Cy, a.W, a.H }).ToList();

        private ImageEntry CreateAndRegisterEntry(string fileName)
        {
            var entry = new ImageEntry
            {
                File = fileName,
                Width = _currentImage!.Width,
                Height = _currentImage!.Height
            };
            _dataset.Images.Add(entry);
            return entry;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Thumbnails
        // ─────────────────────────────────────────────────────────────────────

        private async void BuildThumbnailsAsync()
        {
            _thumbCts?.Cancel();
            _thumbCts = new System.Threading.CancellationTokenSource();
            var ct = _thumbCts.Token;

            lvThumbnails.Items.Clear();
            imgListThumb.Images.Clear();

            var filtered = string.IsNullOrWhiteSpace(_thumbFilter)
                ? _imageFiles
                : _imageFiles.Where(p => Path.GetFileName(p)
                      .StartsWith(_thumbFilter, StringComparison.OrdinalIgnoreCase))
                      .ToList();

            // Add placeholder items immediately so the list feels responsive
            foreach (var file in filtered)
            {
                var item = new ListViewItem(Path.GetFileName(file))
                {
                    Tag = _imageFiles.IndexOf(file)
                };
                lvThumbnails.Items.Add(item);
            }

            lblStatus.Text = "Küçük resimler yükleniyor...";
            Application.DoEvents();

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        if (ct.IsCancellationRequested) break;

                        Bitmap thumb;
                        try
                        {
                            using var fs = new FileStream(filtered[i], FileMode.Open, FileAccess.Read);
                            using var bmp = new Bitmap(fs);
                            thumb = new Bitmap(bmp, imgListThumb.ImageSize);
                        }
                        catch
                        {
                            thumb = new Bitmap(imgListThumb.ImageSize.Width, imgListThumb.ImageSize.Height);
                        }

                        if (IsDisposed || Disposing) break;

                        int idx = i;
                        try
                        {
                            Invoke((Action)(() =>
                            {
                                if (IsDisposed || Disposing || ct.IsCancellationRequested) return;
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
            catch (OperationCanceledException) { /* new filter fired, expected */ }

            if (!IsDisposed && !Disposing) UpdateStatus();
        }

        private void lvThumbnails_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvThumbnails.SelectedIndices.Count == 0) return;

            var item = lvThumbnails.Items[lvThumbnails.SelectedIndices[0]];
            int originalIdx = item.Tag is int t ? t : lvThumbnails.SelectedIndices[0];

            if (originalIdx == _currentIndex) return;

            SaveCurrentAnnotations();
            _currentIndex = originalIdx;
            LoadCurrentImage();
            UpdateNavButtons();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Image loading & navigation
        // ─────────────────────────────────────────────────────────────────────

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
                _currentImage = new Bitmap(temp); // unlocks the file handle
            }
            catch (Exception ex)
            {
                MessageBox.Show("Resim yüklenemedi: " + ex.Message);
                return;
            }

            var fileName = DatasetRelativePath(_imageFiles[_currentIndex]);
            _annotations = ParseAnnotations(_dataset.Images.FirstOrDefault(e => e.File == fileName));

            // Sync thumbnail selection (list may be filtered, so match by Tag)
            var thumbItem = lvThumbnails.Items
                .Cast<ListViewItem>()
                .FirstOrDefault(it => it.Tag is int t && t == _currentIndex);
            if (thumbItem != null) { thumbItem.Selected = true; thumbItem.EnsureVisible(); }

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

        // ─────────────────────────────────────────────────────────────────────
        // Class management
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshClassCombo()
        {
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
            if (!_classes.Contains(name)) _classes.Add(name);

            int idx = _classes.IndexOf(name);
            RefreshClassCombo();
            cmbClass.SelectedIndex = idx;
            _selectedClassId = idx;
        }

        private int SelectedClassId => _selectedClassId;

        // ─────────────────────────────────────────────────────────────────────
        // Zoom
        // ─────────────────────────────────────────────────────────────────────

        private void FitZoom()
        {
            if (_currentImage == null) return;
            float zw = (float)pnlCanvas.ClientSize.Width / _currentImage.Width;
            float zh = (float)pnlCanvas.ClientSize.Height / _currentImage.Height;
            _zoom = Math.Max(Math.Min(zw, zh), ZoomMin);
            UpdateCanvas();
        }

        private void btnZoomIn_Click(object sender, EventArgs e) => ApplyZoom(_zoom + ZoomStep);
        private void btnZoomOut_Click(object sender, EventArgs e) => ApplyZoom(_zoom - ZoomStep);
        private void btnZoomFit_Click(object sender, EventArgs e) => FitZoom();

        private void ApplyZoom(float z)
        {
            _zoom = Math.Clamp(z, ZoomMin, ZoomMax);
            UpdateCanvas();
        }

        private void pnlCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != Keys.Control) return;
            if (e is HandledMouseEventArgs he) he.Handled = true;
            ApplyZoom(_zoom + (e.Delta > 0 ? ZoomStep : -ZoomStep));
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

        // ── Coordinate helpers ────────────────────────────────────────────────

        private RectangleF ImageRect() =>
            _currentImage == null
                ? RectangleF.Empty
                : new RectangleF(0, 0,
                      _currentImage.Width * _zoom,
                      _currentImage.Height * _zoom);

        private RectangleF YoloToCanvas(BBoxAnnotation a)
        {
            if (_currentImage == null) return RectangleF.Empty;
            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            return new RectangleF(
                (a.Cx - a.W / 2f) * iw,
                (a.Cy - a.H / 2f) * ih,
                a.W * iw, a.H * ih);
        }

        private BBoxAnnotation RectToAnnotation(RectangleF r, int classId)
        {
            if (_currentImage == null) return new BBoxAnnotation();
            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            return new BBoxAnnotation
            {
                ClassId = classId,
                Cx = (r.Left + r.Width / 2f) / iw,
                Cy = (r.Top + r.Height / 2f) / ih,
                W = r.Width / iw,
                H = r.Height / ih
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Paint
        // ─────────────────────────────────────────────────────────────────────

        private void picCanvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.DimGray);
            if (_currentImage == null) return;

            float iw = _currentImage.Width * _zoom;
            float ih = _currentImage.Height * _zoom;
            e.Graphics.DrawImage(_currentImage, 0, 0, iw, ih);
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < _annotations.Count; i++)
                DrawAnnotation(e.Graphics, _annotations[i], selected: i == _selectedAnnot);

            // In-progress rectangle
            if (_drawMode == DrawMode.Drawing && _drawRect.Width > 2 && _drawRect.Height > 2)
            {
                using var dashPen = new Pen(Color.Yellow, 1.5f) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(dashPen, _drawRect.X, _drawRect.Y, _drawRect.Width, _drawRect.Height);
            }
        }

        private void DrawAnnotation(Graphics g, BBoxAnnotation a, bool selected)
        {
            var rect = YoloToCanvas(a);
            var color = ClassColor(a.ClassId);

            using var pen = new Pen(color, selected ? 2.5f : 1.5f);
            using var fill = new SolidBrush(Color.FromArgb(selected ? 50 : 25, color));

            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            // Label background + text
            string lbl = a.ClassId < _classes.Count ? $"{a.ClassId}:{_classes[a.ClassId]}" : $"{a.ClassId}";
            using var font = new Font("Segoe UI", 8, FontStyle.Bold);
            using var bgBrush = new SolidBrush(Color.FromArgb(180, color));
            var sz = g.MeasureString(lbl, font);
            g.FillRectangle(bgBrush, rect.X, rect.Y - sz.Height, sz.Width, sz.Height);
            g.DrawString(lbl, font, Brushes.Black, rect.X, rect.Y - sz.Height);

            if (selected) DrawHandles(g, rect);
        }

        private static void DrawHandles(Graphics g, RectangleF r)
        {
            foreach (var h in GetHandleRects(r).Values)
            {
                g.FillRectangle(Brushes.White, h);
                g.DrawRectangle(Pens.Black, h.X, h.Y, h.Width, h.Height);
            }
        }

        private static Dictionary<Handle, RectangleF> GetHandleRects(RectangleF r)
        {
            float hs = HandleSize;
            float half = hs / 2f;
            float cx = r.Left + r.Width / 2f;
            float cy = r.Top + r.Height / 2f;

            return new Dictionary<Handle, RectangleF>
            {
                [Handle.TopLeft] = new(r.Left - half, r.Top - half, hs, hs),
                [Handle.TopRight] = new(r.Right - half, r.Top - half, hs, hs),
                [Handle.BottomLeft] = new(r.Left - half, r.Bottom - half, hs, hs),
                [Handle.BottomRight] = new(r.Right - half, r.Bottom - half, hs, hs),
                [Handle.Top] = new(cx - half, r.Top - half, hs, hs),
                [Handle.Bottom] = new(cx - half, r.Bottom - half, hs, hs),
                [Handle.Left] = new(r.Left - half, cy - half, hs, hs),
                [Handle.Right] = new(r.Right - half, cy - half, hs, hs),
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mouse interaction
        // ─────────────────────────────────────────────────────────────────────

        private void picCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_currentImage == null || e.Button != MouseButtons.Left) return;
            pnlCanvas.Focus();

            PointF pt = e.Location;

            // 1. Handle resize (selected bbox handles)
            if (_selectedAnnot >= 0 && _selectedAnnot < _annotations.Count)
            {
                var handles = GetHandleRects(YoloToCanvas(_annotations[_selectedAnnot]));
                foreach (var kv in handles)
                {
                    if (!kv.Value.Contains(pt)) continue;
                    _drawMode = DrawMode.Resizing;
                    _activeHandle = kv.Key;
                    _originalRect = YoloToCanvas(_annotations[_selectedAnnot]);
                    _dragStart = pt;
                    return;
                }

                // 2. Move selected bbox
                var selRect = YoloToCanvas(_annotations[_selectedAnnot]);
                if (selRect.Contains(pt))
                {
                    _drawMode = DrawMode.Moving;
                    _dragStart = pt;
                    _moveOffset = new PointF(pt.X - selRect.X, pt.Y - selRect.Y);
                    return;
                }
            }

            // 3. Click on an unselected bbox → select and start moving
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                var rect = YoloToCanvas(_annotations[i]);
                if (!rect.Contains(pt)) continue;

                _selectedAnnot = i;
                RefreshAnnotationList();
                picCanvas.Invalidate();
                _drawMode = DrawMode.Moving;
                _dragStart = pt;
                _moveOffset = new PointF(pt.X - rect.X, pt.Y - rect.Y);
                return;
            }

            // 4. Empty canvas → start drawing new bbox
            _selectedAnnot = -1;
            RefreshAnnotationList();
            _drawMode = DrawMode.Drawing;
            _dragStart = pt;
            _drawRect = new RectangleF(pt.X, pt.Y, 0, 0);
        }

        private void picCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentImage == null) return;

            var imgRect = ImageRect();
            var pt = new PointF(
                Math.Clamp(e.X, 0, imgRect.Width),
                Math.Clamp(e.Y, 0, imgRect.Height));

            switch (_drawMode)
            {
                case DrawMode.Drawing:
                    _drawRect = RectangleF.FromLTRB(
                        Math.Min(pt.X, _dragStart.X), Math.Min(pt.Y, _dragStart.Y),
                        Math.Max(pt.X, _dragStart.X), Math.Max(pt.Y, _dragStart.Y));
                    picCanvas.Invalidate();
                    break;

                case DrawMode.Moving when _selectedAnnot >= 0:
                    ApplyMove(pt, imgRect);
                    picCanvas.Invalidate();
                    break;

                case DrawMode.Resizing when _selectedAnnot >= 0:
                    ApplyResize(pt, imgRect);
                    picCanvas.Invalidate();
                    return; // skip cursor update — resize cursors are set in MouseDown
            }

            UpdateCursor(pt);
        }

        private void ApplyMove(PointF pt, RectangleF imgRect)
        {
            var a = _annotations[_selectedAnnot];
            var rect = YoloToCanvas(a);
            float x = Math.Clamp(pt.X - _moveOffset.X, 0, imgRect.Width - rect.Width);
            float y = Math.Clamp(pt.Y - _moveOffset.Y, 0, imgRect.Height - rect.Height);
            var moved = RectToAnnotation(new RectangleF(x, y, rect.Width, rect.Height), a.ClassId);
            a.Cx = moved.Cx;
            a.Cy = moved.Cy;
        }

        private void ApplyResize(PointF pt, RectangleF imgRect)
        {
            float dx = pt.X - _dragStart.X;
            float dy = pt.Y - _dragStart.Y;

            float left = _originalRect.Left;
            float top = _originalRect.Top;
            float right = _originalRect.Right;
            float bottom = _originalRect.Bottom;

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

            // Enforce minimum size and clamp to image bounds
            if (right - left < 4) right = left + 4;
            if (bottom - top < 4) bottom = top + 4;
            left = Math.Max(0, left);
            top = Math.Max(0, top);
            right = Math.Min(imgRect.Width, right);
            bottom = Math.Min(imgRect.Height, bottom);

            var a = _annotations[_selectedAnnot];
            var newA = RectToAnnotation(RectangleF.FromLTRB(left, top, right, bottom), a.ClassId);
            a.Cx = newA.Cx; a.Cy = newA.Cy; a.W = newA.W; a.H = newA.H;
        }

        private void UpdateCursor(PointF pt)
        {
            if (_selectedAnnot < 0) { picCanvas.Cursor = Cursors.Cross; return; }

            var handles = GetHandleRects(YoloToCanvas(_annotations[_selectedAnnot]));
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

            picCanvas.Cursor = YoloToCanvas(_annotations[_selectedAnnot]).Contains(pt)
                ? Cursors.SizeAll : Cursors.Cross;
        }

        private void picCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            bool modified = false;

            if (_drawMode == DrawMode.Drawing)
            {
                if (_drawRect.Width > 5 && _drawRect.Height > 5)
                {
                    _annotations.Add(RectToAnnotation(_drawRect, SelectedClassId));
                    _selectedAnnot = _annotations.Count - 1;
                    RefreshAnnotationList();
                    UpdateStatus();
                    modified = true;
                }
                _drawRect = RectangleF.Empty;
            }
            else if (_drawMode is DrawMode.Moving or DrawMode.Resizing)
            {
                RefreshAnnotationList();
                modified = true;
            }

            _drawMode = DrawMode.None;
            _activeHandle = Handle.None;
            picCanvas.Invalidate();

            if (modified) SaveCurrentAnnotations();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Annotation list (right panel)
        // ─────────────────────────────────────────────────────────────────────

        private bool _suppressAnnotSel = false;

        private void RefreshAnnotationList()
        {
            _suppressAnnotSel = true;
            lvAnnotations.BeginUpdate();

            // Add / remove items only as needed (avoids flicker from full Clear)
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
                var cls = a.ClassId < _classes.Count ? _classes[a.ClassId] : a.ClassId.ToString();
                var item = lvAnnotations.Items[i];

                item.Text = (i + 1).ToString();
                item.SubItems[1].Text = cls;
                item.SubItems[2].Text = a.Cx.ToString("F3");
                item.SubItems[3].Text = a.Cy.ToString("F3");
                item.SubItems[4].Text = a.W.ToString("F3");
                item.SubItems[5].Text = a.H.ToString("F3");

                var bg = Color.FromArgb(30, ClassColor(a.ClassId));
                if (item.BackColor != bg) item.BackColor = bg;
            }

            for (int i = 0; i < lvAnnotations.Items.Count; i++)
            {
                bool sel = i == _selectedAnnot;
                if (lvAnnotations.Items[i].Selected != sel)
                    lvAnnotations.Items[i].Selected = sel;
            }

            if (_selectedAnnot >= 0 && _selectedAnnot < lvAnnotations.Items.Count)
                lvAnnotations.Items[_selectedAnnot].EnsureVisible();

            lvAnnotations.EndUpdate();
            _suppressAnnotSel = false;
        }

        private void lvAnnotations_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressAnnotSel || lvAnnotations.SelectedIndices.Count == 0) return;
            _selectedAnnot = lvAnnotations.SelectedIndices[0];
            picCanvas.Invalidate();
        }

        private void btnDeleteAnnotation_Click(object sender, EventArgs e) => DeleteSelected();

        private void DeleteSelected()
        {
            if (_selectedAnnot < 0 || _selectedAnnot >= _annotations.Count) return;
            _annotations.RemoveAt(_selectedAnnot);
            _selectedAnnot = Math.Min(_selectedAnnot, _annotations.Count - 1);
            RefreshAnnotationList();
            picCanvas.Invalidate();
            UpdateStatus();
            SaveCurrentAnnotations();
        }

        private void btnEditAnnotation_Click(object sender, EventArgs e)
        {
            if (_selectedAnnot < 0 || _selectedAnnot >= _annotations.Count)
            {
                MessageBox.Show("Lütfen önce bir annotation seçin.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ShowAnnotationEditor(_annotations[_selectedAnnot]))
            {
                RefreshAnnotationList();
                picCanvas.Invalidate();
                UpdateStatus();
                SaveCurrentAnnotations();
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

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10)
            };
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
            btnOk.Dock = DockStyle.Right; btnOk.Width = 90;
            btnCancel.Dock = DockStyle.Right; btnCancel.Width = 70;
            pnlBtn.Controls.Add(btnCancel);
            pnlBtn.Controls.Add(btnOk);
            tbl.Controls.Add(pnlBtn, 0, 6);
            tbl.SetColumnSpan(pnlBtn, 2);

            dlg.Controls.Add(tbl);
            dlg.KeyPreview = true;

            if (dlg.ShowDialog(this) != DialogResult.OK) return false;

            var nfi = System.Globalization.CultureInfo.InvariantCulture;
            var ns = System.Globalization.NumberStyles.Float;
            if (!float.TryParse(edCx.Text, ns, nfi, out float cx) |
                !float.TryParse(edCy.Text, ns, nfi, out float cy) |
                !float.TryParse(edW.Text, ns, nfi, out float w) |
                !float.TryParse(edH.Text, ns, nfi, out float h))
            {
                MessageBox.Show("Geçersiz sayısal değer.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            a.ClassId = cmbEditClass.SelectedIndex;
            a.Cx = Math.Clamp(cx, 0f, 1f);
            a.Cy = Math.Clamp(cy, 0f, 1f);
            a.W = Math.Clamp(w, 0f, 1f);
            a.H = Math.Clamp(h, 0f, 1f);
            return true;
        }

        private void btnSaveNow_Click(object sender, EventArgs e) => SaveCurrentAnnotations();

        // ─────────────────────────────────────────────────────────────────────
        // Auto-Label
        // ─────────────────────────────────────────────────────────────────────

        private ALPR.Detection.LicensePlateDetector? _detectorV1;
        private string _detectorPathV1 = "";
        private const string DefaultModelV1 = "models/LicencePlateDetection_Gpu.onnx";

        private YoloOnnxRunner.PlateRecognitionModel? _detectorV2;
        private string _detectorPathV2 = "";
        private const string DefaultModelV2 = "models/plateReconitionV2.onnx";

        private bool _useV2Model = true;

        /// <summary>
        /// Model bu eşiğin üzerinde bir güvenle farklı bir sınıf tahmin ediyorsa,
        /// cmbClass seçimini geçersiz kılıp kendi tahminini kullanır.
        /// </summary>
        private const float ClassOverrideThreshold = 0.98f;

        private bool EnsureDetector()
        {
            bool loaded = _useV2Model
                ? EnsureModel(ref _detectorPathV2, DefaultModelV2, "Plaka Tespit Modeli (V2)",
                      path => new YoloOnnxRunner.PlateRecognitionModel(path, useGpu: true),
                      ref _detectorV2)
                : EnsureModel(ref _detectorPathV1, DefaultModelV1, "Plaka Tespit Modeli",
                      path => new ALPR.Detection.LicensePlateDetector(path, useGpu: false),
                      ref _detectorV1);

            if (loaded && _useV2Model && _detectorV2?.ClassLabels != null)
            {
                SyncClassesFromModel(_detectorV2.ClassLabels);
            }

            return loaded;
        }

        private void SyncClassesFromModel(List<string> labels)
        {
            bool changed = false;
            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (string.IsNullOrWhiteSpace(label)) continue;

                if (i < _classes.Count)
                {
                    // Eğer mevcut sınıf ismi "class_N" gibi geçici bir isimse veya boşsa güncelle
                    if (_classes[i].StartsWith("class_") || _classes[i] == "plate" && label != "plate")
                    {
                        _classes[i] = label;
                        changed = true;
                    }
                }
                else
                {
                    _classes.Add(label);
                    changed = true;
                }
            }

            if (changed)
            {
                RefreshClassCombo();
            }
        }

        private bool EnsureModel<T>(
            ref string path, string defaultPath, string dialogTitle,
            Func<string, T> factory, ref T? instance) where T : class
        {
            if (path == "") path = defaultPath;

            if (!File.Exists(path))
            {
                using var ofd = new OpenFileDialog
                {
                    Title = dialogTitle + " Seçin",
                    Filter = "ONNX Model|*.onnx|Tüm Dosyalar|*.*",
                    InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models")
                };
                if (ofd.ShowDialog(this) != DialogResult.OK) return false;
                path = ofd.FileName;
            }

            if (instance != null) return true;

            try
            {
                lblStatus.Text = $"🔧 {dialogTitle} yükleniyor...";
                Application.DoEvents();
                instance = factory(path);
                lblSaved.Text = $"✓ {dialogTitle} yüklendi";
                lblSaved.ForeColor = Color.Green;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{dialogTitle} yüklenemedi:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                path = "";
                instance = null;
                return false;
            }
        }

        private void btnAutoLabel_Click(object sender, EventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Önce bir resim yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!EnsureDetector()) return;

            AutoLabelCurrentImage(confirmOverwrite: true);
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
            if (!EnsureDetector()) return;

            var confirm = MessageBox.Show(
                $"{_imageFiles.Count} resim otomatik etiketlenecek.\n" +
                "Var olan annotation'lar korunacak (tespit edilenler eklenecek).\n\nDevam?",
                "Tümünü Otomatik Etiketle",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

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
                    using var bmp = new Bitmap(temp);

                    string fname = DatasetRelativePath(_imageFiles[i]);
                    var entry = _dataset.Images.FirstOrDefault(e => e.File == fname);
                    var existAnnots = ParseAnnotations(entry);

                    totalAdded += RunDetector(bmp, existAnnots, classId: capturedClassId);

                    if (entry == null)
                    {
                        entry = new ImageEntry { File = fname, Width = bmp.Width, Height = bmp.Height };
                        _dataset.Images.Add(entry);
                    }
                    entry.Annotations.Boxes = SerializeAnnotations(existAnnots);
                }
                catch (Exception ex)
                {
                    lblSaved.Text = $"⚠ {Path.GetFileName(_imageFiles[i])}: {ex.Message}";
                    lblSaved.ForeColor = Color.OrangeRed;
                }
            }

            SaveDataset();

            // Reload current image in case its annotations changed
            if (_currentIndex >= 0)
            {
                string curFile = DatasetRelativePath(_imageFiles[_currentIndex]);
                _annotations = ParseAnnotations(_dataset.Images.FirstOrDefault(e => e.File == curFile));
                RefreshAnnotationList();
                picCanvas.Invalidate();
            }

            pbAutoLabel.Visible = false;
            UpdateStatus();
            MessageBox.Show(
                $"Tamamlandı! {_imageFiles.Count} resim işlendi, toplam {totalAdded} yeni bbox eklendi.",
                "Otomatik Etiketleme Bitti", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AutoLabelCurrentImage(bool confirmOverwrite)
        {
            if (confirmOverwrite && _annotations.Count > 0)
            {
                var res = MessageBox.Show(
                    $"Bu resimde zaten {_annotations.Count} annotation var.\n" +
                    "[Evet] = Ekle (var olanları koru)\n[Hayır] = Hepsini sil, sadece yenileri ekle",
                    "Annotation Mevcut", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (res == DialogResult.Cancel) return;
                if (res == DialogResult.No) _annotations.Clear();
            }
            RunDetector(_currentImage!, _annotations);
        }

        /// <summary>
        /// Runs the active detector on a bitmap and appends non-duplicate detections.
        /// Returns the number of annotations added.
        /// </summary>
        private int RunDetector(Bitmap bmp, List<BBoxAnnotation> target, int classId = -1)
        {
            if (classId < 0) classId = SelectedClassId;

            // OnnxRuntime requires Format24bppRgb
            bool mustDispose = bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            Bitmap input = mustDispose
                ? ConvertTo24bpp(bmp) : bmp;

            try
            {
                return _useV2Model
                    ? AppendDetectionsV2(input, target, classId)
                    : AppendDetectionsV1(input, target, classId);
            }
            finally
            {
                if (mustDispose) input.Dispose();
            }
        }

        private static Bitmap ConvertTo24bpp(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(dst);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dst;
        }

        private int AppendDetectionsV2(Bitmap bmp, List<BBoxAnnotation> target, int classId)
        {
            var detections = _detectorV2!.Predict(bmp);
            int added = 0;

            foreach (var det in detections)
            {
                // V2 modelde ülke sınıfı doğrudan çıktıda geliyor.
                // Geçerli bir class id döndüyse doğrudan onu kullan.
                int finalClass = det.ClassId >= 0 ? det.ClassId : classId;

                var annot = NormalizeDetection(det.Box, bmp.Width, bmp.Height, finalClass);
                if (!IsDuplicate(annot, target)) { target.Add(annot); added++; }
            }

            return added;
        }

        private int AppendDetectionsV1(Bitmap bmp, List<BBoxAnnotation> target, int classId)
        {
            var result = _detectorV1!.Detect(bmp, 0.35f, true, 0.45f);
            int added = 0;

            foreach (var det in result.Detections)
            {
                var annot = NormalizeDetection(
                    new RectangleF(det.X, det.Y, det.Width, det.Height),
                    bmp.Width, bmp.Height, classId);
                if (!IsDuplicate(annot, target)) { target.Add(annot); added++; }
            }

            return added;
        }

        private static BBoxAnnotation NormalizeDetection(RectangleF box, int imgW, int imgH, int classId) =>
            new BBoxAnnotation
            {
                ClassId = classId,
                Cx = Math.Clamp((box.X + box.Width / 2f) / imgW, 0f, 1f),
                Cy = Math.Clamp((box.Y + box.Height / 2f) / imgH, 0f, 1f),
                W = Math.Clamp(box.Width / imgW, 0f, 1f),
                H = Math.Clamp(box.Height / imgH, 0f, 1f)
            };

        private static bool IsDuplicate(BBoxAnnotation candidate, List<BBoxAnnotation> existing) =>
            existing.Any(a => YoloIoU(a, candidate) > 0.5f);

        private static float YoloIoU(BBoxAnnotation a, BBoxAnnotation b)
        {
            float ax1 = a.Cx - a.W / 2f, ax2 = a.Cx + a.W / 2f;
            float ay1 = a.Cy - a.H / 2f, ay2 = a.Cy + a.H / 2f;
            float bx1 = b.Cx - b.W / 2f, bx2 = b.Cx + b.W / 2f;
            float by1 = b.Cy - b.H / 2f, by2 = b.Cy + b.H / 2f;

            float ix = Math.Max(0, Math.Min(ax2, bx2) - Math.Max(ax1, bx1));
            float iy = Math.Max(0, Math.Min(ay2, by2) - Math.Max(ay1, by1));
            float inter = ix * iy;
            float union = a.W * a.H + b.W * b.H - inter;
            return union <= 0 ? 0f : inter / union;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Plate list & label clearing
        // ─────────────────────────────────────────────────────────────────────

        private void btnPlateList_Click(object sender, EventArgs e)
        {
            if (_dataset == null || string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(_datasetPath))
            {
                MessageBox.Show("Lütfen önce veri seti klasörünü seçin.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveCurrentAnnotations();

            // Try to infer the active class from the folder name
            string folderName = new DirectoryInfo(_folderPath).Name;
            int activeClass = _classes.FindIndex(c => folderName.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
            if (activeClass < 0) activeClass = _classes.FindIndex(c => c.IndexOf(folderName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (activeClass < 0) activeClass = cmbClass.SelectedIndex;

            using var frm = new FullPlateList(_folderPath, _dataset, _datasetPath, _classes, activeClass);
            frm.ShowDialog(this);
            LoadCurrentImage(); // Reload in case the list form made changes
        }

        private void btnTemizle_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_folderPath) || _dataset == null || _imageFiles.Count == 0) return;

            string folderName = new DirectoryInfo(_folderPath).Name;
            if (MessageBox.Show(
                    $"Dikkat: '{folderName}' klasöründeki TÜM ETİKETLER kalıcı olarak silinecek.\n\nOnaylıyor musunuz?",
                    "Klasör Etiketlerini Temizle",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            int cleared = 0;
            foreach (var imgFile in _imageFiles)
            {
                var entry = _dataset.Images.FirstOrDefault(x => x.File == DatasetRelativePath(imgFile));
                if (entry?.Annotations?.Boxes?.Count > 0)
                {
                    cleared += entry.Annotations.Boxes.Count;
                    entry.Annotations.Boxes.Clear();
                }
            }

            _annotations.Clear();
            _selectedAnnot = -1;
            RefreshAnnotationList();
            picCanvas.Invalidate();
            UpdateStatus();
            SaveDataset();

            MessageBox.Show($"Toplam {cleared} adet etiket silindi.", "İşlem Tamam",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Keyboard shortcuts
        // ─────────────────────────────────────────────────────────────────────

        private void ImageLabeling_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                case Keys.Back:
                    DeleteSelected(); e.Handled = true; break;

                case Keys.Left: Navigate(-1); e.Handled = true; break;
                case Keys.Right: Navigate(+1); e.Handled = true; break;

                case Keys.Add:
                case Keys.Oemplus:
                    ApplyZoom(_zoom + ZoomStep); e.Handled = true; break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    ApplyZoom(_zoom - ZoomStep); e.Handled = true; break;

                case Keys.D0 when e.Control: FitZoom(); e.Handled = true; break;
                case Keys.S when e.Control: SaveCurrentAnnotations(); e.Handled = true; break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Form closing
        // ─────────────────────────────────────────────────────────────────────

        private void ImageLabeling_FormClosing(object sender, FormClosingEventArgs e)
        {
            _thumbCts?.Cancel();
            SaveCurrentAnnotations();
            _currentImage?.Dispose();
            _detectorV1?.Dispose();
            _detectorV2?.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utility
        // ─────────────────────────────────────────────────────────────────────

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
    }
}