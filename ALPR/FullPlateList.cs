using System.Text.Json;

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

        public FullPlateList()
        {
            // For designer 
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

            flpPlates.Scroll -= FlpPlates_Scroll;
            flpPlates.Scroll += FlpPlates_Scroll;
            flpPlates.MouseWheel -= FlpPlates_MouseWheel;
            flpPlates.MouseWheel += FlpPlates_MouseWheel;
            flpPlates.MouseEnter -= FlpPlates_MouseEnter;
            flpPlates.MouseEnter += FlpPlates_MouseEnter;

            btnAddClassGlobal.Click -= BtnAddClassGlobal_Click;
            btnAddClassGlobal.Click += BtnAddClassGlobal_Click;

            _cts = new CancellationTokenSource();
            try
            {
                await LoadPlatesAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void FlpPlates_MouseEnter(object sender, EventArgs e)
        {
            if (!flpPlates.Focused) flpPlates.Focus();
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
            
            try
            {
                await LoadPlatesAsync(_cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        private class PlateItem
        {
            public ImageEntry Entry;
            public float[] Box;
            public string FullPath;
        }

        private List<PlateItem> _allPlates = new List<PlateItem>();
        private List<PlateItem> _filteredPlates = new List<PlateItem>();
        private int _loadedCount = 0;
        private const int BatchSize = 50;
        private bool _isLoadingBatch = false;

        private async Task LoadPlatesAsync(CancellationToken token)
        {
            lblStatus.Text = "Plaka listesi hazırlanıyor...";
            _allPlates.Clear();
            _loadedCount = 0;

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
                        // Sadece seçili klasördeki resimleri göster
                        string entryDir = Path.GetDirectoryName(fullPath);
                        if (!string.Equals(entryDir, _folderPath, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!File.Exists(fullPath)) continue;

                    if (entry.Annotations == null || entry.Annotations.Boxes == null || entry.Annotations.Boxes.Count == 0)
                        continue;

                    foreach (var box in entry.Annotations.Boxes)
                    {
                        var pi = new PlateItem();
                        pi.Entry = entry;
                        pi.Box = box;
                        pi.FullPath = fullPath;
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

            await LoadNextBatchAsync(token);
        }

        private bool _isUpdatingFilter = false;

        private void InitFilterCombo()
        {
            _isUpdatingFilter = true;
            cmbFilterClass.SelectedIndexChanged -= CmbFilterClass_SelectedIndexChanged;
            cmbFilterClass.Items.Clear();
            cmbFilterClass.Items.Add("* Tüm Sınıflar *");
            foreach (var cls in _classes)
            {
                cmbFilterClass.Items.Add(cls);
            }
            cmbFilterClass.SelectedIndex = 0;
            cmbFilterClass.SelectedIndexChanged += CmbFilterClass_SelectedIndexChanged;
            _isUpdatingFilter = false;
        }

        private async void CmbFilterClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFilter) return;
            ApplyFilter();
            ClearAndReload();
            await LoadNextBatchAsync(_cts.Token);
        }

        private void ApplyFilter()
        {
            if (cmbFilterClass.SelectedIndex <= 0)
            {
                _filteredPlates = _allPlates.ToList();
            }
            else
            {
                int targetClassId = cmbFilterClass.SelectedIndex - 1;
                _filteredPlates = _allPlates.Where(p => (int)p.Box[0] == targetClassId).ToList();
            }
        }

        private void ClearAndReload()
        {
            _loadedCount = 0;
            flpPlates.SuspendLayout();
            
            // Mevcut kontrollerin dispose edilmesi ÇOK ÖNEMLİ (GDI Leak engellemek için)
            for (int i = flpPlates.Controls.Count - 1; i >= 0; i--)
            {
                if (flpPlates.Controls[i] is Panel panel)
                {
                    for (int j = panel.Controls.Count - 1; j >= 0; j--)
                    {
                        var child = panel.Controls[j];
                        if (child is PictureBox pic && pic.Image != null)
                        {
                            var img = pic.Image;
                            pic.Image = null;
                            img.Dispose();
                        }
                        child.Dispose();
                    }
                    panel.Dispose();
                }
                else
                {
                    flpPlates.Controls[i].Dispose();
                }
            }

            flpPlates.Controls.Clear();
            flpPlates.ResumeLayout();
            flpPlates.AutoScrollPosition = new Point(0, 0);
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
                    if (ctrl is Panel pnl)
                    {
                        foreach (Control child in pnl.Controls)
                        {
                            if (child is ComboBox c)
                            {
                                int sel = c.SelectedIndex;
                                c.Items.Clear();
                                c.Items.AddRange(_classes.ToArray());
                                if (sel >= 0 && sel < c.Items.Count)
                                {
                                    c.SelectedIndex = sel;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FlpPlates_MouseWheel(object sender, MouseEventArgs e)
        {
            CheckScrollAndLoad();
        }

        private void FlpPlates_Scroll(object sender, ScrollEventArgs e)
        {
            CheckScrollAndLoad();
        }

        private void CheckScrollAndLoad()
        {
            if (_isLoadingBatch || _loadedCount >= _filteredPlates.Count || _cts.Token.IsCancellationRequested) return;

            int maxScroll = flpPlates.VerticalScroll.Maximum - flpPlates.ClientSize.Height;
            if (maxScroll <= 0 || flpPlates.VerticalScroll.Value >= maxScroll - 300)
            {
                _ = LoadNextBatchAsync(_cts.Token);
            }
        }

        private async Task LoadNextBatchAsync(CancellationToken token)
        {
            if (_isLoadingBatch) return;
            _isLoadingBatch = true;

            int itemsToLoad = Math.Min(BatchSize, _filteredPlates.Count - _loadedCount);
            if (itemsToLoad <= 0) 
            {
                _isLoadingBatch = false;
                if (_filteredPlates.Count == 0)
                    lblStatus.Text = "Bu sınıfa ait plaka bulunamadı.";
                return;
            }

            lblStatus.Text = $"{_loadedCount} / {_filteredPlates.Count} plaka yüklendi. (Yükleniyor...)";

            var batch = _filteredPlates.Skip(_loadedCount).Take(itemsToLoad).ToList();
            
            await Task.Run(() =>
            {
                var groupedBatch = batch.GroupBy(x => x.FullPath);
                foreach (var group in groupedBatch)
                {
                    if (token.IsCancellationRequested) break;

                    using var bmp = new Bitmap(group.Key);
                    foreach (var item in group)
                    {
                        if (token.IsCancellationRequested) break;

                        float cx = item.Box[1];
                        float cy = item.Box[2];
                        float w = item.Box[3];
                        float h = item.Box[4];

                        int pW = (int)(w * bmp.Width);
                        int pH = (int)(h * bmp.Height);
                        int pX = (int)(cx * bmp.Width - pW / 2f);
                        int pY = (int)(cy * bmp.Height - pH / 2f);

                        pX = Math.Max(0, pX);
                        pY = Math.Max(0, pY);
                        pW = Math.Clamp(pW, 1, bmp.Width - pX);
                        pH = Math.Clamp(pH, 1, bmp.Height - pY);

                        Rectangle cropRect = new Rectangle(pX, pY, pW, pH);
                        Bitmap cropBmp = new Bitmap(pW, pH);

                        using (Graphics g = Graphics.FromImage(cropBmp))
                        {
                            g.DrawImage(bmp, new Rectangle(0, 0, pW, pH), cropRect, GraphicsUnit.Pixel);
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            if (this.IsDisposed) return;
                            AddPlateCard(cropBmp, item.Entry, item.Box, item.FullPath);
                            _loadedCount++;
                            lblStatus.Text = $"{_loadedCount} / {_filteredPlates.Count} plaka yüklendi. (Yükleniyor...)";
                        });
                    }
                }
            }, token);

            if (!this.IsDisposed)
            {
                if (_loadedCount < _filteredPlates.Count)
                    lblStatus.Text = $"{_loadedCount} / {_filteredPlates.Count} plaka yüklendi. Aşağı kaydırarak devam edin.";
                else
                    lblStatus.Text = $"{_loadedCount} / {_filteredPlates.Count} plaka yüklendi. Tüm plakalar yüklendi.";
                    
                _isLoadingBatch = false;
                
                if (flpPlates.VerticalScroll.Visible == false && _loadedCount < _filteredPlates.Count)
                {
                    _ = LoadNextBatchAsync(token);
                }
            }
        }

        private static readonly Color[] _palette = {
            Color.FromArgb(55, 120, 120),   // 0 red
            Color.FromArgb(80, 255, 110),   // 1 green
            Color.FromArgb(130, 200, 255),   // 2 blue
            Color.FromArgb(255, 230,  80),   // 3 yellow
            Color.FromArgb(255, 250, 255),   // 4 magenta
            Color.FromArgb(100, 255, 255),   // 5 cyan
            Color.FromArgb(55, 18,  180),   // 6 orange
            Color.FromArgb(180, 12, 255),   // 7 purple
            Color.FromArgb(255, 16, 20),   // 8 pink
            Color.FromArgb(100, 210, 180),   // 9 teal
            Color.FromArgb(220, 200, 140),   // 10 tan
        };

        private Color GetClassColor(int id)
        {
            if (id < 0) return Color.White;
            if (id < _palette.Length) return _palette[id];

            var rnd = new Random(id * 73);
            int r = rnd.Next(60, 240);
            int g = rnd.Next(60, 240);
            int b = rnd.Next(60, 240);
            return Color.FromArgb(255, r, g, b);
        }

        private void AddPlateCard(Bitmap cropBmp, ImageEntry entry, float[] box, string imagePath)
        {
            int currentClassId = (int)box[0];

            Panel card = new Panel
            {
                Width = 160,
                Height = 150,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };

            if (currentClassId != _activeClassId)
            {
                card.BackColor = GetClassColor(currentClassId);
            }

            PictureBox pic = new PictureBox
            {
                Image = cropBmp,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 150,
                Height = 60,
                Top = 5,
                Left = 5
            };
            
            // Show filename on hover if useful
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(pic, Path.GetFileName(imagePath));

            ComboBox cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Top = 70,
                Left = 5
            };
            cmb.Items.AddRange(_classes.ToArray());
            
            if (currentClassId >= 0 && currentClassId < _classes.Count)
            {
                cmb.SelectedIndex = currentClassId;
            }

            cmb.SelectedIndexChanged += (s, e) =>
            {
                if (cmb.SelectedIndex != -1 && (int)box[0] != cmb.SelectedIndex)
                {
                    box[0] = cmb.SelectedIndex;
                    if (cmb.SelectedIndex == _activeClassId)
                    {
                        card.BackColor = SystemColors.Control;
                    }
                    else
                    {
                        card.BackColor = GetClassColor(cmb.SelectedIndex);
                    }
                    SaveDataset();
                }
            };

            cmb.MouseWheel += (s, e) =>
            {
                if (e is HandledMouseEventArgs he)
                {
                    he.Handled = true;
                }
            };

            Button btnCopy = new Button
            {
                Text = "Kopyala",
                Width = 72,
                Height = 30,
                Top = 105,
                Left = 5,
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnCopy.FlatAppearance.BorderColor = Color.Gray;
            btnCopy.Click += (s, e) =>
            {
                try {
                    Clipboard.SetImage(cropBmp);
                } catch { }
            };

            Button btnDelete = new Button
            {
                Text = "Sil",
                Width = 72,
                Height = 30,
                Top = 105,
                Left = 83,
                BackColor = Color.FromArgb(255, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.FlatAppearance.BorderColor = Color.DarkRed;
            btnDelete.Click += (s, e) =>
            {
                var dlg = MessageBox.Show("Bu plakayı silmek istediğinize emin misiniz?", "Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlg == DialogResult.Yes)
                {
                    flpPlates.Focus();
                    int scrollY = Math.Abs(flpPlates.AutoScrollPosition.Y);
                    
                    entry.Annotations.Boxes.Remove(box);
                    
                    flpPlates.SuspendLayout();
                    flpPlates.Controls.Remove(card);
                    card.Dispose();
                    flpPlates.ResumeLayout();
                    
                    flpPlates.AutoScrollPosition = new Point(0, scrollY);
                    SaveDataset();
                }
            };

            card.Controls.Add(pic);
            card.Controls.Add(cmb);
            card.Controls.Add(btnCopy);
            card.Controls.Add(btnDelete);

            flpPlates.Controls.Add(card);
        }

        private void SaveDataset()
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_dataset, opts);
            File.WriteAllText(_datasetPath, json);
        }

        private void FullPlateList_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }

            // Kapanma anındaki Dispose işlemlerinin UI'ı dondurmasını engellemek için
            // FlowLayoutPanel'in layout güncellemelerini durdurup görünmez yapıyoruz.
            this.SuspendLayout();
            flpPlates.Visible = false;
            flpPlates.SuspendLayout();

            for (int i = flpPlates.Controls.Count - 1; i >= 0; i--)
            {
                if (flpPlates.Controls[i] is Panel panel)
                {
                    for (int j = panel.Controls.Count - 1; j >= 0; j--)
                    {
                        var child = panel.Controls[j];
                        if (child is PictureBox pic && pic.Image != null)
                        {
                            var img = pic.Image;
                            pic.Image = null;
                            img.Dispose();
                        }
                        child.Dispose();
                    }
                    panel.Dispose();
                }
                else
                {
                    flpPlates.Controls[i].Dispose();
                }
            }
            flpPlates.Controls.Clear();
        }

        private static string PromptInput(string message, string title)
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
            Label lbl = new Label { Left = 10, Top = 10, Text = message, AutoSize = true };
            TextBox txt = new TextBox { Left = 10, Top = 30, Width = 280 };
            Button btnOk = new Button { Text = "Tamam", Left = 130, Top = 60, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "İptal", Left = 210, Top = 60, DialogResult = DialogResult.Cancel };
            dlg.Controls.Add(lbl);
            dlg.Controls.Add(txt);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            return dlg.ShowDialog() == DialogResult.OK ? txt.Text : null;
        }
    }
}
