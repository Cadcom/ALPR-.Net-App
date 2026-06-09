using System.Drawing.Drawing2D;
using System.Text.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ALPR
{
    public partial class FullPlateList : Form
    {
        private string _folderPath;
        private DatasetRoot _dataset;
        private string _datasetPath;
        private List<string> _classes;
        private int _activeClassId = -1;

        private CancellationTokenSource _cts;

        private List<PlateItem> _allPlates = new List<PlateItem>();
        private List<PlateItem> _filteredPlates = new List<PlateItem>();

        private int _currentPage = 0;
        private int _totalPages = 0;
        private const int BatchSize = 25;
        private bool _isLoadingBatch = false;

        public FullPlateList()
        {
            InitializeComponent();
        }

        public FullPlateList(string folderPath, DatasetRoot dataset, string datasetPath, List<string> classes, int activeClassId = -1)
        {
            InitializeComponent();
            _folderPath = folderPath;
            _dataset = dataset;
            _datasetPath = datasetPath;
            _classes = classes;
            _activeClassId = activeClassId;
        }



        private async void FullPlateList_Load(object sender, EventArgs e)
        {
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
            btnAddClassGlobal.Click += BtnAddClassGlobal_Click;
            btnPrevPage.Click += BtnPrevPage_Click;
            btnNextPage.Click += BtnNextPage_Click;
            btnTighten.Click += BtnTighten_Click;

            flpPlates.Resize += FlpPlates_Resize;

            _cts = new CancellationTokenSource();
            try
            {
                await LoadPlatesAsync(_cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        private void FlpPlates_Resize(object sender, EventArgs e)
        {
            ResizeCards();
        }

        private void ResizeCards()
        {
            if (flpPlates.Controls.Count == 0) return;

            int cols = 5;
            int margin = 5;
            int scrollbarWidth = 25;
            int w = (flpPlates.ClientSize.Width - scrollbarWidth) / cols - (margin * 2);

            int rows = (int)Math.Ceiling(flpPlates.Controls.Count / (double)cols);
            if (rows <= 0) rows = 1;
            int h = flpPlates.ClientSize.Height / rows - (margin * 2);

            w = Math.Max(220, w);
            h = Math.Max(180, h);

            flpPlates.SuspendLayout();
            foreach (Control c in flpPlates.Controls)
            {
                c.Size = new System.Drawing.Size(w, h);
            }
            flpPlates.ResumeLayout();
        }

        private async void BtnTighten_Click(object sender, EventArgs e)
        {
            if (_isLoadingBatch) return;

            foreach (Control ctrl in flpPlates.Controls)
            {
                if (ctrl is PlateCardControl card)
                {
                    card.ApplyTighten();
                }
            }
        }

        private async void BtnPrevPage_Click(object sender, EventArgs e)
        {
            if (_isLoadingBatch || _currentPage <= 0) return;
            _currentPage--;
            await LoadCurrentPageAsync();
            txtPageInfo.SelectAll();
        }

        private async void BtnNextPage_Click(object sender, EventArgs e)
        {
            if (_isLoadingBatch || _currentPage >= _totalPages - 1) return;
            _currentPage++;
            await LoadCurrentPageAsync();
            txtPageInfo.SelectAll();
        }

        private async void ChkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();

            ClearAndReload();

            try { await LoadPlatesAsync(_cts.Token); }
            catch (OperationCanceledException) { }
        }

        public class PlateItem
        {
            public ImageEntry Entry;
            public float[] Box;
            public string FullPath;
        }

        private async Task LoadPlatesAsync(CancellationToken token)
        {
            lblStatus.Text = "Plaka listesi hazırlanıyor...";
            _allPlates.Clear();
            _currentPage = 0;

            bool isAllChecked = chkAll.Checked;

            await Task.Run(() =>
            {
                string dirName = Path.GetDirectoryName(_datasetPath);
                if (string.IsNullOrEmpty(dirName)) return;

                foreach (var entry in _dataset.Images)
                {
                    if (token.IsCancellationRequested) break;

                    string fullPath = Path.Combine(dirName, entry.File);

                    if (!isAllChecked)
                    {
                        string entryDir = Path.GetDirectoryName(fullPath);
                        if (!string.Equals(entryDir, _folderPath, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!File.Exists(fullPath)) continue;

                    if (entry.Annotations == null || entry.Annotations.Boxes == null || entry.Annotations.Boxes.Count == 0)
                        continue;

                    foreach (var box in entry.Annotations.Boxes)
                    {
                        var pi = new PlateItem
                        {
                            Entry = entry,
                            Box = box,
                            FullPath = fullPath
                        };
                        _allPlates.Add(pi);
                    }
                }
            }, token);

            if (token.IsCancellationRequested) return;

            int prevFilter = cmbFilterClass.SelectedIndex;
            InitFilterCombo();
            if (prevFilter >= 0 && prevFilter < cmbFilterClass.Items.Count)
                cmbFilterClass.SelectedIndex = prevFilter;

            ApplyFilter();

            await LoadCurrentPageAsync();
        }

        private bool _isUpdatingFilter = false;

        private void InitFilterCombo()
        {
            _isUpdatingFilter = true;
            cmbFilterClass.SelectedIndexChanged -= CmbFilterClass_SelectedIndexChanged;
            cmbFilterClass.Items.Clear();
            cmbFilterClass.Items.Add("* Tüm Sınıflar *");
            foreach (var cls in _classes) cmbFilterClass.Items.Add(cls);
            cmbFilterClass.SelectedIndex = 0;
            cmbFilterClass.SelectedIndexChanged += CmbFilterClass_SelectedIndexChanged;
            _isUpdatingFilter = false;
        }

        private async void CmbFilterClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFilter) return;
            ApplyFilter();
            _currentPage = 0;
            ClearAndReload();
            await LoadCurrentPageAsync();
        }

        private void ApplyFilter()
        {
            if (cmbFilterClass.SelectedIndex <= 0)
                _filteredPlates = _allPlates.ToList();
            else
            {
                int targetClassId = cmbFilterClass.SelectedIndex - 1;
                _filteredPlates = _allPlates.Where(p => (int)p.Box[0] == targetClassId).ToList();
            }

            _totalPages = (int)Math.Ceiling((double)_filteredPlates.Count / BatchSize);
            if (_totalPages == 0) _totalPages = 1;
        }

        private void ClearAndReload()
        {
            flpPlates.SuspendLayout();

            for (int i = flpPlates.Controls.Count - 1; i >= 0; i--)
            {
                var child = flpPlates.Controls[i];
                flpPlates.Controls.RemoveAt(i);
                child.Dispose();
            }

            flpPlates.ResumeLayout();
            flpPlates.AutoScrollPosition = new System.Drawing.Point(0, 0);
        }

        private void BtnAddClassGlobal_Click(object sender, EventArgs e)
        {
            string name = PromptInput("Yeni class adı:", "Class Ekle");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (!_classes.Contains(name))
            {
                _classes.Add(name);
                SaveDataset();

                int prevGlobalFilter = cmbFilterClass.SelectedIndex;
                InitFilterCombo();
                cmbFilterClass.SelectedIndex = prevGlobalFilter;

                foreach (Control ctrl in flpPlates.Controls)
                {
                    if (ctrl is PlateCardControl card)
                    {
                        card.UpdateClassList();
                    }
                }
            }
        }

        public class CardData
        {
            public PlateItem Item;
            public Bitmap DisplayBmp;
            public RectangleF NormCropBox;
            public int Sx, Sy;
            public int CropW, CropH;
            public int FullW, FullH;
        }

        private async Task LoadCurrentPageAsync()
        {
            if (_isLoadingBatch) return;
            _isLoadingBatch = true; // prevent multiple entry

            ClearAndReload();

            if (_filteredPlates.Count == 0)
            {
                lblStatus.Text = "Bu sınıfa ait plaka bulunamadı.";
                txtPageInfo.Text = $"Sayfa 1 / 1";
                btnPrevPage.Enabled = false;
                btnNextPage.Enabled = false;
                _isLoadingBatch = false;
                return;
            }

            txtPageInfo.Text = $"Sayfa {_currentPage + 1} / {_totalPages}";
            btnPrevPage.Enabled = _currentPage > 0;
            btnNextPage.Enabled = _currentPage < _totalPages - 1;

            int startIndex = _currentPage * BatchSize;
            var batch = _filteredPlates.Skip(startIndex).Take(BatchSize).ToList();

            lblStatus.Text = $"Resimler yükleniyor...";

            await Task.Run(() =>
            {
                var groupedBatch = batch.GroupBy(x => x.FullPath);
                foreach (var group in groupedBatch)
                {
                    if (_cts.IsCancellationRequested) break;

                    try
                    {
                        using var bmp = new Bitmap(group.Key);
                        int fullW = bmp.Width;
                        int fullH = bmp.Height;

                        foreach (var item in group)
                        {
                            if (_cts.IsCancellationRequested) break;

                            var data = PrepareCardData(bmp, item);
                            data.FullW = fullW;
                            data.FullH = fullH;

                            this.Invoke((MethodInvoker)delegate
                            {
                                if (this.IsDisposed) return;
                                var card = new PlateCardControl(this, data);
                                flpPlates.Controls.Add(card);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Resim yüklenemedi: {group.Key} - {ex.Message}");
                    }
                }
            }, _cts.Token);

            if (!this.IsDisposed)
            {
                ResizeCards();
                lblStatus.Text = $"Seçilen Sayfa Yüklendi ({batch.Count} öğe).";
                _isLoadingBatch = false;
            }
        }

        private CardData PrepareCardData(Bitmap fullBmp, PlateItem item)
        {
            float cx = item.Box[1];
            float cy = item.Box[2];
            float w = item.Box[3];
            float h = item.Box[4];

            float boxPxW = w * fullBmp.Width;
            float boxPxH = h * fullBmp.Height;

            // Enlarge by 50% for context (which allows shrinking or minor expanding)
            int cw = (int)Math.Max(5, boxPxW * 1.5f);
            int ch = (int)Math.Max(5, boxPxH * 1.5f);

            int cxImage = (int)(cx * fullBmp.Width);
            int cyImage = (int)(cy * fullBmp.Height);

            int sx = cxImage - cw / 2;
            int sy = cyImage - ch / 2;

            // Crop in native aspect ratio and resolution
            Bitmap cropBmp = new Bitmap(cw, ch);
            using (Graphics g = Graphics.FromImage(cropBmp))
            {
                g.Clear(Color.Black);
                Rectangle srcRect = new Rectangle(sx, sy, cw, ch);
                g.DrawImage(fullBmp, new Rectangle(0, 0, cw, ch), srcRect, GraphicsUnit.Pixel);
            }

            float nx = ((cxImage - boxPxW / 2f) - sx) / cw;
            float ny = ((cyImage - boxPxH / 2f) - sy) / ch;
            float nw = boxPxW / cw;
            float nh = boxPxH / ch;

            return new CardData
            {
                Item = item,
                DisplayBmp = cropBmp,
                NormCropBox = new RectangleF(nx, ny, nw, nh),
                Sx = sx,
                Sy = sy,
                CropW = cw,
                CropH = ch
            };
        }

        public void RemoveItem(PlateItem item, PlateCardControl card)
        {
            item.Entry.Annotations.Boxes.Remove(item.Box);
            _allPlates.Remove(item);
            _filteredPlates.Remove(item);

            _totalPages = (int)Math.Ceiling((double)_filteredPlates.Count / BatchSize);
            if (_totalPages == 0) _totalPages = 1;

            if (_currentPage >= _totalPages) _currentPage = _totalPages - 1;
            txtPageInfo.Text = $"Sayfa {_currentPage + 1} / {_totalPages}";

            SaveDataset();
            flpPlates.Controls.Remove(card);
            card.Dispose();
        }

        public void SaveDataset()
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_dataset, opts);
            File.WriteAllText(_datasetPath, json);
        }

        public List<string> GetClasses() => _classes;
        public int GetActiveClassId() => _activeClassId;

        private void FullPlateList_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cts != null) _cts.Cancel();
            this.SuspendLayout();
            flpPlates.Visible = false;
            flpPlates.SuspendLayout();
            ClearAndReload();
        }

        private static string PromptInput(string message, string title)
        {
            using var dlg = new Form
            {
                Text = title,
                Size = new System.Drawing.Size(320, 130),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label lbl = new Label { Left = 10, Top = 10, Text = message, AutoSize = true };
            TextBox txt = new TextBox { Left = 10, Top = 30, Width = 280 };
            Button btnOk = new Button { Text = "Tamam", Left = 130, Top = 60, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "İptal", Left = 210, Top = 60, DialogResult = DialogResult.Cancel };
            dlg.Controls.Add(lbl); dlg.Controls.Add(txt); dlg.Controls.Add(btnOk); dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
            return dlg.ShowDialog() == DialogResult.OK ? txt.Text : null;
        }

        private void FullPlateList_KeyDown(object sender, KeyEventArgs e)
        {
            Keys keyCode = e.KeyData & Keys.KeyCode;
            if (keyCode == Keys.PageUp || keyCode == Keys.Left)
            {
                if (btnPrevPage.Enabled) BtnPrevPage_Click(this, EventArgs.Empty);

            }
            if (keyCode == Keys.PageDown || keyCode == Keys.Right)
            {
                if (btnNextPage.Enabled) BtnNextPage_Click(this, EventArgs.Empty);

            }

        }

        private async void txtPageInfo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string input = txtPageInfo.Text;
                // Parse number from strings like "Sayfa 5 / 10" or just "5"
                var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int pageNum))
                {
                    int targetPage = pageNum - 1;
                    if (targetPage >= 0 && targetPage < _totalPages)
                    {
                        _currentPage = targetPage;
                        await LoadCurrentPageAsync();
                    }
                    else
                    {
                        // Reset to current correct info
                        txtPageInfo.Text = $"Sayfa {_currentPage + 1} / {_totalPages}";
                    }
                }
                txtPageInfo.SelectAll();
            }
        }
    }

    public class PlateCardControl : Panel
    {
        private FullPlateList _parentForm;
        private FullPlateList.PlateItem _item;
        private Bitmap _displayBmp;
        private RectangleF _normBbox;
        private int _sx, _sy, _cropW, _cropH, _fullW, _fullH;

        private PictureBox _pic;
        private ComboBox _cmb;

        private enum DrawMode { None, Moving, Resizing }
        private DrawMode _drawMode = DrawMode.None;
        private PointF _dragStart;
        private RectangleF _originalNormRect;
        private int _activeHandle = -1;

        public PlateCardControl(FullPlateList parent, dynamic data)
        {
            _parentForm = parent;
            _item = data.Item;
            _displayBmp = data.DisplayBmp;
            _normBbox = data.NormCropBox;
            _sx = data.Sx; _sy = data.Sy;
            _cropW = data.CropW; _cropH = data.CropH;
            _fullW = data.FullW; _fullH = data.FullH;

            Width = 220;
            Height = 180;
            Margin = new Padding(5);
            BorderStyle = BorderStyle.FixedSingle;
            UpdateBackColor();

            _pic = new PictureBox
            {
                Top = 5,
                Left = 5,
                Width = 208,
                Height = 95,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Cursor = Cursors.Cross
            };
            _pic.Paint += Pic_Paint;
            _pic.MouseDown += Pic_MouseDown;
            _pic.MouseMove += Pic_MouseMove;
            _pic.MouseUp += Pic_MouseUp;

            _cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 5,
                Height = 28,
                Width = 208,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            // Keep combobox vertically tied to bottom
            _cmb.Top = Height - 75;
            UpdateClassList();

            Button btnTightenCard = new Button { Text = "Sıkılaştır", Height = 28, Left = 5, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Color.LightSkyBlue, FlatStyle = FlatStyle.Flat };
            btnTightenCard.Top = Height - 40;
            btnTightenCard.FlatAppearance.BorderColor = Color.SteelBlue;
            btnTightenCard.Click += (s, e) => ApplyTighten();

            Button btnCopy = new Button { Text = "Kopya", Height = 28, Left = 5, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Color.FromArgb(240, 240, 240), FlatStyle = FlatStyle.Flat };
            btnCopy.Top = Height - 40;
            btnCopy.FlatAppearance.BorderColor = Color.Gray;
            btnCopy.Click += (s, e) => {
                try
                {
                    Rectangle roiRect = new Rectangle(
                        (int)(_normBbox.X * _cropW),
                        (int)(_normBbox.Y * _cropH),
                        (int)(_normBbox.Width * _cropW),
                        (int)(_normBbox.Height * _cropH));

                    roiRect.X = Math.Max(0, roiRect.X);
                    roiRect.Y = Math.Max(0, roiRect.Y);
                    roiRect.Width = Math.Min(_displayBmp.Width - roiRect.X, roiRect.Width);
                    roiRect.Height = Math.Min(_displayBmp.Height - roiRect.Y, roiRect.Height);

                    if (roiRect.Width > 0 && roiRect.Height > 0)
                    {
                        Bitmap cropped = new Bitmap(roiRect.Width, roiRect.Height);
                        using (Graphics g = Graphics.FromImage(cropped))
                        {
                            g.DrawImage(_displayBmp, new Rectangle(0, 0, roiRect.Width, roiRect.Height), roiRect, GraphicsUnit.Pixel);
                        }
                        Clipboard.SetImage(cropped);
                    }
                }
                catch { }
            };

            Button btnReset = new Button { Text = "Sıfırla", Height = 28, Left = 70, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Color.LightYellow, FlatStyle = FlatStyle.Flat };
            btnReset.Top = Height - 40;
            btnReset.FlatAppearance.BorderColor = Color.Orange;
            btnReset.Click += (s, e) => { if (_cmb.Items.Count > 0) _cmb.SelectedIndex = 0; };

            Button btnDelete = new Button { Text = "Sil", Height = 28, Left = 140, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Color.FromArgb(255, 100, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelete.Top = Height - 40;
            btnDelete.FlatAppearance.BorderColor = Color.DarkRed;
            btnDelete.Click += (s, e) =>
            {
                var dlg = MessageBox.Show("Bu plakayı silmek istediğinize emin misiniz?", "Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlg == DialogResult.Yes) _parentForm.RemoveItem(_item, this);
            };

            // Adjust buttons on resize to stay spread
            this.Resize += (s, e) => {
                int pad = 5;
                _cmb.Top = this.ClientSize.Height - 75;
                btnTightenCard.Top = this.ClientSize.Height - 40;
                btnCopy.Top = this.ClientSize.Height - 40;
                btnReset.Top = this.ClientSize.Height - 40;
                btnDelete.Top = this.ClientSize.Height - 40;

                int btnW = (this.ClientSize.Width - (pad * 5)) / 4;
                btnTightenCard.Width = btnW; btnTightenCard.Left = pad;
                btnCopy.Width = btnW; btnCopy.Left = pad * 2 + btnW;
                btnReset.Width = btnW; btnReset.Left = pad * 3 + btnW * 2;
                btnDelete.Width = btnW; btnDelete.Left = pad * 4 + btnW * 3;
            };

            Controls.Add(_pic);
            Controls.Add(_cmb);
            Controls.Add(btnTightenCard);
            Controls.Add(btnCopy);
            Controls.Add(btnReset);
            Controls.Add(btnDelete);

            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(_pic, Path.GetFileName(_item.FullPath));
        }

        public void UpdateClassList()
        {
            _cmb.SelectedIndexChanged -= Cmb_SelectedIndexChanged;
            _cmb.Items.Clear();
            _cmb.Items.AddRange(_parentForm.GetClasses().ToArray());
            int currentClassId = (int)_item.Box[0];
            if (currentClassId >= 0 && currentClassId < _cmb.Items.Count)
                _cmb.SelectedIndex = currentClassId;
            _cmb.SelectedIndexChanged += Cmb_SelectedIndexChanged;
            UpdateBackColor();
        }

        private void Cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmb.SelectedIndex != -1 && (int)_item.Box[0] != _cmb.SelectedIndex)
            {
                _item.Box[0] = _cmb.SelectedIndex;
                _parentForm.SaveDataset();
                UpdateBackColor();
                _pic.Invalidate();
            }
        }

        private void UpdateBackColor()
        {
            int currentClassId = (int)_item.Box[0];
            if (currentClassId == _parentForm.GetActiveClassId()) BackColor = SystemColors.Control;
            else BackColor = GetClassColor(currentClassId);
        }

        private static readonly Color[] _palette = {
            Color.FromArgb(55, 120, 120), Color.FromArgb(80, 255, 110), Color.FromArgb(130, 200, 255), Color.FromArgb(255, 230,  80),
            Color.FromArgb(255, 250, 255), Color.FromArgb(100, 255, 255), Color.FromArgb(55, 18,  180), Color.FromArgb(180, 12, 255)
        };
        private Color GetClassColor(int id)
        {
            if (id >= 0 && id < _palette.Length) return _palette[id];
            var rnd = new Random(id * 73);
            return Color.FromArgb(255, rnd.Next(60, 240), rnd.Next(60, 240), rnd.Next(60, 240));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_displayBmp != null) { _displayBmp.Dispose(); _displayBmp = null; }
            }
            base.Dispose(disposing);
        }

        private void Pic_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(_displayBmp, new Rectangle(0, 0, _pic.Width, _pic.Height));

            RectangleF pxBbox = new RectangleF(
                _normBbox.X * _pic.Width, _normBbox.Y * _pic.Height,
                _normBbox.Width * _pic.Width, _normBbox.Height * _pic.Height);

            var color = GetClassColor((int)_item.Box[0]);
            using var pen = new Pen(color, 2f);
            using var fill = new SolidBrush(Color.FromArgb(40, color));

            e.Graphics.FillRectangle(fill, pxBbox);
            e.Graphics.DrawRectangle(pen, pxBbox.X, pxBbox.Y, pxBbox.Width, pxBbox.Height);

            var handles = GetHandles(pxBbox);
            foreach (var h in handles)
            {
                e.Graphics.FillRectangle(Brushes.White, h);
                e.Graphics.DrawRectangle(Pens.Black, h.X, h.Y, h.Width, h.Height);
            }
        }

        private RectangleF[] GetHandles(RectangleF r)
        {
            float s = 6; float h = s / 2f;
            float cx = r.Left + r.Width / 2f; float cy = r.Top + r.Height / 2f;
            return new RectangleF[] {
                new RectangleF(r.Left - h, r.Top - h, s, s), new RectangleF(r.Right - h, r.Top - h, s, s),
                new RectangleF(r.Left - h, r.Bottom - h, s, s), new RectangleF(r.Right - h, r.Bottom - h, s, s),
                new RectangleF(cx - h, r.Top - h, s, s), new RectangleF(cx - h, r.Bottom - h, s, s),
                new RectangleF(r.Left - h, cy - h, s, s), new RectangleF(r.Right - h, cy - h, s, s)
            };
        }

        private void Pic_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var handles = GetHandles(new RectangleF(_normBbox.X * _pic.Width, _normBbox.Y * _pic.Height, _normBbox.Width * _pic.Width, _normBbox.Height * _pic.Height));
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].Contains(e.Location))
                {
                    _drawMode = DrawMode.Resizing;
                    _activeHandle = i;
                    _originalNormRect = _normBbox;
                    _dragStart = e.Location;
                    return;
                }
            }

            RectangleF pxBbox = new RectangleF(_normBbox.X * _pic.Width, _normBbox.Y * _pic.Height, _normBbox.Width * _pic.Width, _normBbox.Height * _pic.Height);
            if (pxBbox.Contains(e.Location))
            {
                _drawMode = DrawMode.Moving;
                _originalNormRect = _normBbox;
                _dragStart = e.Location;
            }
        }

        private void Pic_MouseMove(object sender, MouseEventArgs e)
        {
            float dx = (e.X - _dragStart.X) / (float)_pic.Width;
            float dy = (e.Y - _dragStart.Y) / (float)_pic.Height;

            if (_drawMode == DrawMode.Moving)
            {
                float nx = Math.Clamp(_originalNormRect.X + dx, 0, 1f - _originalNormRect.Width);
                float ny = Math.Clamp(_originalNormRect.Y + dy, 0, 1f - _originalNormRect.Height);
                _normBbox = new RectangleF(nx, ny, _originalNormRect.Width, _originalNormRect.Height);
                _pic.Invalidate();
            }
            else if (_drawMode == DrawMode.Resizing)
            {
                float left = _originalNormRect.Left, top = _originalNormRect.Top, right = _originalNormRect.Right, bottom = _originalNormRect.Bottom;
                switch (_activeHandle)
                {
                    case 0: left += dx; top += dy; break;
                    case 1: right += dx; top += dy; break;
                    case 2: left += dx; bottom += dy; break;
                    case 3: right += dx; bottom += dy; break;
                    case 4: top += dy; break;
                    case 5: bottom += dy; break;
                    case 6: left += dx; break;
                    case 7: right += dx; break;
                }

                if (right - left < 0.05f) right = left + 0.05f;
                if (bottom - top < 0.05f) bottom = top + 0.05f;
                left = Math.Max(0, left); top = Math.Max(0, top);
                right = Math.Min(1f, right); bottom = Math.Min(1f, bottom);

                _normBbox = RectangleF.FromLTRB(left, top, right, bottom);
                _pic.Invalidate();
            }
            else
            {
                RectangleF pxBbox = new RectangleF(_normBbox.X * _pic.Width, _normBbox.Y * _pic.Height, _normBbox.Width * _pic.Width, _normBbox.Height * _pic.Height);
                var handles = GetHandles(pxBbox);
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].Contains(e.Location))
                    {
                        _pic.Cursor = i switch { 0 or 3 => Cursors.SizeNWSE, 1 or 2 => Cursors.SizeNESW, 4 or 5 => Cursors.SizeNS, _ => Cursors.SizeWE };
                        return;
                    }
                }
                _pic.Cursor = pxBbox.Contains(e.Location) ? Cursors.SizeAll : Cursors.Cross;
            }
        }

        private void Pic_MouseUp(object sender, MouseEventArgs e)
        {
            if (_drawMode != DrawMode.None)
            {
                UpdateYoloBox();
                _pic.Invalidate();
            }
            _drawMode = DrawMode.None;
        }

        private void UpdateYoloBox()
        {
            // Map norm local back to absolute px
            float pxLeft = _sx + _normBbox.X * _cropW;
            float pxTop = _sy + _normBbox.Y * _cropH;
            float pxW = _normBbox.Width * _cropW;
            float pxH = _normBbox.Height * _cropH;

            float newCx = (pxLeft + pxW / 2f) / _fullW;
            float newCy = (pxTop + pxH / 2f) / _fullH;
            float newW = pxW / _fullW;
            float newH = pxH / _fullH;

            _item.Box[1] = Math.Clamp(newCx, 0f, 1f);
            _item.Box[2] = Math.Clamp(newCy, 0f, 1f);
            _item.Box[3] = Math.Clamp(newW, 0f, 1f);
            _item.Box[4] = Math.Clamp(newH, 0f, 1f);

            _parentForm.SaveDataset();
        }

        public void ApplyTighten()
        {
            System.Diagnostics.Debug.WriteLine("[Tighten] CALLED");
            try
            {
                Rectangle roiRect = new Rectangle(
                    (int)(_normBbox.X * _cropW),
                    (int)(_normBbox.Y * _cropH),
                    (int)(_normBbox.Width * _cropW),
                    (int)(_normBbox.Height * _cropH));

                int rx = Math.Max(0, roiRect.X);
                int ry = Math.Max(0, roiRect.Y);
                int rw = Math.Min(_displayBmp.Width - rx, roiRect.Width);
                int rh = Math.Min(_displayBmp.Height - ry, roiRect.Height);

                System.Diagnostics.Debug.WriteLine($"[Tighten] rx={rx} ry={ry} rw={rw} rh={rh}  bmpW={_displayBmp.Width} bmpH={_displayBmp.Height}");

                if (rw <= 5 || rh <= 5)
                {
                    System.Diagnostics.Debug.WriteLine($"[Tighten] EARLY RETURN: rw or rh too small");
                    return;
                }

                // 1. Convert Bitmap to Mat safely
                // Previously: using var mat = BitmapConverter.ToMat(_displayBmp).Clone();
                // This leaked the intermediate Mat from ToMat.
                using var matInput = BitmapConverter.ToMat(_displayBmp);
                using var mat = matInput.Clone();

                // 2. Extract ROI safely
                // Previously: using var roiMat = new Mat(mat, new Rect(rx, ry, rw, rh)).Clone();
                // This leaked the intermediate Mat wrapper.
                using var roiWrap = new Mat(mat, new Rect(rx, ry, rw, rh));
                using var roiMat = roiWrap.Clone();

                using var gray = new Mat();
                Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

                if (gray.Empty()) return; // Safety check

                using var blurred = new Mat();

                if (gray.Empty())
                    throw new Exception("gray is empty");

                if (gray.Type() != MatType.CV_8UC1)
                    throw new Exception("gray type invalid");
                try
                {
                    Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message );
                }
                
                

                // ─────────────────────────────────────────────────────────────────
                // STEP 1 — Otsu thresholding.
                //
                // Otsu, histogramın iki baskın grubu arasındaki sınıf-içi varyansı
                // minimize eden eşiği otomatik bulur.
                //
                // Neden median*0.45 yerine Otsu?
                //   median*0.45 parlak sahnelerde çöküyor:
                //     median=184 → brightThr=82 → kırmızı araba gövdesi (~80-120 gray)
                //     "bright" sayılıyor → trim imkânsız.
                //
                //   Otsu ise şu iki senaryoda da doğru çalışır:
                //     • Beyaz plaka + kırmızı araba: eşik ~150-170 → araba dışarıda ✓
                //     • Gri/siyah arka plan + beyaz plaka: eşik ~100  → plaka içinde ✓
                //
                // Clamp(50..220): tamamen düz (tek renkli) ROI'lerde Otsu ~0 veya
                // ~255 dönebilir; bu dejenere case'leri makul sınıra çekiyoruz.
                // ─────────────────────────────────────────────────────────────────
                using var brightMask = new Mat();
                double otsuVal = Cv2.Threshold(blurred, brightMask, 0, 255,
                                               ThresholdTypes.Binary | ThresholdTypes.Otsu);
                int brightThr = Math.Clamp((int)otsuVal, 50, 220);

                // ─────────────────────────────────────────────────────────────────
                // STEP 2 — Per-column / per-row bright-pixel-count via Reduce.
                //
                // We threshold the blurred image to get a binary mask (0 or 255),
                // then use Cv2.Reduce (OpenCV-native → stride-safe) to sum each
                // column and each row.
                //
                //   colSums[c] ∈ [0, rh*255]   →   colDensity = colSums[c]/(rh*255)
                //   rowSums[r] ∈ [0, rw*255]   →   rowDensity = rowSums[r]/(rw*255)
                //
                // contentDensity = 0.10: a stripe must have ≥10% bright pixels to
                // be considered plate content.  Dark car shadow → ~0% → trimmed.
                // ─────────────────────────────────────────────────────────────────
                const double contentDensity = 0.09;

                // ReduceDimension.Row  → collapses rows → output is 1×rw (one value per column)
                using var colSumMat = new Mat();
                Cv2.Reduce(brightMask, colSumMat, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);
                int[] colSums = new int[rw];
                System.Runtime.InteropServices.Marshal.Copy(colSumMat.Data, colSums, 0, rw);

                // ReduceDimension.Column → collapses cols → output is rh×1 (one value per row)
                using var rowSumMat = new Mat();
                Cv2.Reduce(brightMask, rowSumMat, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32S);
                int[] rowSums = new int[rh];
                System.Runtime.InteropServices.Marshal.Copy(rowSumMat.Data, rowSums, 0, rh);

                int colThreshI = (int)(rh * 255 * contentDensity);
                int rowThreshI = (int)(rw * 255 * contentDensity);

                // ─────────────────────────────────────────────────────────────────
                // STEP 3 — (Dilation kaldırıldı)
                //
                // Dilation kenar taramasında kontraproduktif:
                //   colSums[28..31]=0 (gerçek siyah kenarlık) iken dilW=4 olunca
                //   col-27'deki yüksek değer bu sütunlara yayılır ve kenarlık
                //   "görünmez" hale gelir → right/left yanlış bulunur.
                //
                // Kenar taraması zaten dışarıdan içeriye gittiği için iç gürültü
                // diplerine toleranslıdır; dilation'a gerek yoktur.
                // ─────────────────────────────────────────────────────────────────

                // ─────────────────────────────────────────────────────────────────
                // STEP 4 — Scan from each edge to find the first content stripe.
                //          Ham colSums / rowSums kullanılıyor (dilated değil).
                // ─────────────────────────────────────────────────────────────────
                int left = 0, right = rw - 1, top = 0, bottom = rh - 1;

                for (int c = 0; c < rw; c++) if (colSums[c] >= colThreshI) { left = c; break; }
                for (int c = rw - 1; c >= 0; c--) if (colSums[c] >= colThreshI) { right = c; break; }
                for (int r = 0; r < rh; r++) if (rowSums[r] >= rowThreshI) { top = r; break; }
                for (int r = rh - 1; r >= 0; r--) if (rowSums[r] >= rowThreshI) { bottom = r; break; }

                using var hsv = new Mat();
                Cv2.CvtColor(roiMat, hsv, ColorConversionCodes.BGR2HSV);

                using var blueMask = new Mat();
                Cv2.InRange(hsv, new Scalar(100, 80, 40), new Scalar(135, 255, 255), blueMask);

                using var blueColProj = new Mat();
                Cv2.Reduce(blueMask, blueColProj, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);

                int[] blueColSums = new int[rw];

                // Native data pointer to managed int[] copy
                if (blueColProj.Total() > 0 && blueColProj.Data != IntPtr.Zero)
                {
                    int toCopy = Math.Min(rw, (int)blueColProj.Total());
                    System.Runtime.InteropServices.Marshal.Copy(blueColProj.Data, blueColSums, 0, toCopy);
                }

                int blueMax = blueColSums.Length > 0 ? blueColSums.Max() : 0;
                if (blueMax > 255 * (rh * 0.20f))
                {
                    for (int c = 0; c < rw; c++)
                        if (blueColSums[c] > blueMax * 0.15f) { left = Math.Min(left, c); break; }
                }

                // ── DEBUG ─────────────────────────────────────────────────────────
                var _dbg = new System.Text.StringBuilder();
                _dbg.Append($"[Tighten] ROI: rw={rw} rh={rh}  otsuVal={otsuVal:F1}  brightThr={brightThr}  colThreshI={colThreshI}\n");
                _dbg.Append("[Tighten] colSums L→R first 8: ");
                for (int d = 0; d < Math.Min(8, rw); d++) _dbg.Append(colSums[d] + " ");
                _dbg.Append("\n[Tighten] colSums R→L first 8: ");
                for (int d = rw - 1; d >= Math.Max(0, rw - 8); d--) _dbg.Append(colSums[d] + " ");

                _dbg.Append($"\n[Tighten] FOUND: left={left}  right={right}  top={top}  bottom={bottom}");
                _dbg.Append($"\n[Tighten] newW={right - left + 1}  newH={bottom - top + 1}  oldArea={roiRect.Width * roiRect.Height}  newArea={(right - left + 1) * (bottom - top + 1)}");
                System.Diagnostics.Debug.WriteLine(_dbg.ToString());
                // ── END DEBUG ─────────────────────────────────────────────────────


                float newX = rx + left;
                float newY = ry + top;
                float newW = (right - left) + 1;
                float newH = (bottom - top) + 1;

                float oldArea = roiRect.Width * roiRect.Height;
                float newArea = newW * newH;

                if (newArea >= oldArea * 0.25f && newW > 10 && newH > 5)
                {
                    _normBbox = new RectangleF(newX / _cropW, newY / _cropH, newW / _cropW, newH / _cropH);
                    UpdateYoloBox();
                    _pic.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Tighten] CRASH: {ex.GetType().Name}: {ex.Message}");
            }
        }



    }
}