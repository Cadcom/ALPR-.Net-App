using ALPR.Detection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace ALPR
{
    public partial class frmModelComparison : Form
    {
        private string? _model1Path;
        private string? _model2Path;
        private string? _model3Path;
        private string? _testDataPath;
        
        private LicensePlateDetector? _detector1;
        private LicensePlateDetector? _detector2;
        private LicensePlateDetector? _detector3;
        
        private readonly List<TestResult> _testResults = new();
        private bool _isTestRunning;
        private CancellationTokenSource? _cancellationTokenSource;

        public frmModelComparison()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            lblStatus.Text = "Model performans testi hazýr.";
            progressBar.Value = 0;
            dataGridResults.AutoGenerateColumns = false;
            SetupDataGridColumns();
            SetupGpuSettings();
        }

        private void SetupGpuSettings()
        {
            bool gpuAvailable = ExecutionProviderHelper.IsGpuAvailable();
            chkUseGpu.Enabled = gpuAvailable;
            chkUseGpu.Checked = gpuAvailable;
        }

        private void SetupDataGridColumns()
        {
            dataGridResults.Columns.Clear();
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "Dosya Adý",
                DataPropertyName = "FileName",
                Width = 150
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model1Time",
                HeaderText = "Model 1 (ms)",
                DataPropertyName = "Model1InferenceTime",
                Width = 100
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model1Detections",
                HeaderText = "Model 1 Tespit",
                DataPropertyName = "Model1DetectionCount",
                Width = 100
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model2Time",
                HeaderText = "Model 2 (ms)",
                DataPropertyName = "Model2InferenceTime",
                Width = 100
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model2Detections",
                HeaderText = "Model 2 Tespit",
                DataPropertyName = "Model2DetectionCount",
                Width = 100
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model3Time",
                HeaderText = "Model 3 (ms)",
                DataPropertyName = "Model3InferenceTime",
                Width = 100
            });
            
            dataGridResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model3Detections",
                HeaderText = "Model 3 Tespit",
                DataPropertyName = "Model3DetectionCount",
                Width = 100
            });
        }

        private void btnSelectModel1_Click(object sender, EventArgs e)
        {
            _model1Path = SelectModelFile("Model 1 Seç");
            if (!string.IsNullOrEmpty(_model1Path))
                lblModel1.Text = $"Model 1: {Path.GetFileName(_model1Path)}";
        }

        private void btnSelectModel2_Click(object sender, EventArgs e)
        {
            _model2Path = SelectModelFile("Model 2 Seç");
            if (!string.IsNullOrEmpty(_model2Path))
                lblModel2.Text = $"Model 2: {Path.GetFileName(_model2Path)}";
        }

        private void btnSelectModel3_Click(object sender, EventArgs e)
        {
            _model3Path = SelectModelFile("Model 3 Seç");
            if (!string.IsNullOrEmpty(_model3Path))
                lblModel3.Text = $"Model 3: {Path.GetFileName(_model3Path)}";
        }

        private void btnSelectTestData_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Test verisi klasörünü seçin",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _testDataPath = dialog.SelectedPath;
                lblTestData.Text = $"Test Verisi: {Path.GetFileName(_testDataPath)}";
                
                var imageFiles = GetImageFiles(_testDataPath);
                lblImageCount.Text = $"Resim Sayýsý: {imageFiles.Count}";
            }
        }

        private string? SelectModelFile(string title)
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = "ONNX Model Dosyalarý|*.onnx|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        private List<string> GetImageFiles(string folderPath)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
            return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                           .Where(file => extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                           .ToList();
        }

        private async void btnStartTest_Click(object sender, EventArgs e)
        {
            if (_isTestRunning)
            {
                _cancellationTokenSource?.Cancel();
                return;
            }

            if (!ValidateInputs())
                return;

            await RunPerformanceTest();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(_model1Path) || !File.Exists(_model1Path))
            {
                MessageBox.Show("Model 1 dosyasý seçilmedi veya bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_model2Path) || !File.Exists(_model2Path))
            {
                MessageBox.Show("Model 2 dosyasý seçilmedi veya bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_model3Path) || !File.Exists(_model3Path))
            {
                MessageBox.Show("Model 3 dosyasý seçilmedi veya bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_testDataPath) || !Directory.Exists(_testDataPath))
            {
                MessageBox.Show("Test verisi klasörü seçilmedi veya bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var imageFiles = GetImageFiles(_testDataPath);
            if (imageFiles.Count == 0)
            {
                MessageBox.Show("Test klasöründe geçerli resim dosyasý bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private async Task RunPerformanceTest()
        {
            _isTestRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                btnStartTest.Text = "? Testi Durdur";
                btnStartTest.BackColor = Color.LightCoral;
                
                _testResults.Clear();
                dataGridResults.DataSource = null;
                
                lblStatus.Text = "Modeller yükleniyor...";
                await LoadModels();
                
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                var imageFiles = GetImageFiles(_testDataPath!);
                progressBar.Maximum = imageFiles.Count;
                progressBar.Value = 0;

                lblStatus.Text = "Test baþlýyor...";
                
                for (int i = 0; i < imageFiles.Count; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var imageFile = imageFiles[i];
                    lblStatus.Text = $"Ýþleniyor: {Path.GetFileName(imageFile)} ({i + 1}/{imageFiles.Count})";
                    
                    var result = await ProcessImageFile(imageFile);
                    if (result != null)
                    {
                        _testResults.Add(result);
                    }
                    
                    progressBar.Value = i + 1;
                    
                    // UI'nin donmamasý için kýsa bir bekleme
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    DisplayResults();
                    ShowSummaryStatistics();
                    lblStatus.Text = $"Test tamamlandý! {_testResults.Count} resim iþlendi.";
                }
                else
                {
                    lblStatus.Text = "Test iptal edildi.";
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Test iptal edildi.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Test hatasý: {ex.Message}";
                MessageBox.Show($"Test sýrasýnda hata oluþtu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await UnloadModels();
                _isTestRunning = false;
                btnStartTest.Text = "?? Testi Baþlat";
                btnStartTest.BackColor = SystemColors.Control;
                progressBar.Value = 0;
            }
        }

        private async Task LoadModels()
        {
            await Task.Run(() =>
            {
                bool useGpu = chkUseGpu.Checked && chkUseGpu.Enabled;
                
                _detector1 = new LicensePlateDetector(_model1Path!, useGpu);
                _detector2 = new LicensePlateDetector(_model2Path!, useGpu);
                _detector3 = new LicensePlateDetector(_model3Path!, useGpu);
            });
        }

        private async Task UnloadModels()
        {
            await Task.Run(() =>
            {
                _detector1?.Dispose();
                _detector2?.Dispose();
                _detector3?.Dispose();
                
                _detector1 = null;
                _detector2 = null;
                _detector3 = null;
            });
        }

        private async Task<TestResult?> ProcessImageFile(string imagePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var bitmap = new Bitmap(imagePath);
                    
                    var result = new TestResult
                    {
                        FileName = Path.GetFileName(imagePath),
                        FilePath = imagePath
                    };

                    float confidence = (float)nudConfidence.Value;
                    bool enableNms = chkEnableNMS.Checked;
                    float nmsThreshold = (float)nudNMSThreshold.Value;

                    // Model 1 Test
                    if (_detector1 != null)
                    {
                        var sw = Stopwatch.StartNew();
                        var detection1 = _detector1.Detect(bitmap, confidence, enableNms, nmsThreshold);
                        sw.Stop();
                        
                        result.Model1InferenceTime = sw.ElapsedMilliseconds;
                        result.Model1DetectionCount = detection1.Detections.Count;
                        result.Model1Detections = detection1.Detections;
                    }

                    // Model 2 Test
                    if (_detector2 != null)
                    {
                        var sw = Stopwatch.StartNew();
                        var detection2 = _detector2.Detect(bitmap, confidence, enableNms, nmsThreshold);
                        sw.Stop();
                        
                        result.Model2InferenceTime = sw.ElapsedMilliseconds;
                        result.Model2DetectionCount = detection2.Detections.Count;
                        result.Model2Detections = detection2.Detections;
                    }

                    // Model 3 Test
                    if (_detector3 != null)
                    {
                        var sw = Stopwatch.StartNew();
                        var detection3 = _detector3.Detect(bitmap, confidence, enableNms, nmsThreshold);
                        sw.Stop();
                        
                        result.Model3InferenceTime = sw.ElapsedMilliseconds;
                        result.Model3DetectionCount = detection3.Detections.Count;
                        result.Model3Detections = detection3.Detections;
                    }

                    return result;
                });
            }
            catch (Exception ex)
            {
                Invoke(() => lblStatus.Text = $"Hata: {Path.GetFileName(imagePath)} - {ex.Message}");
                return null;
            }
        }

        private void DisplayResults()
        {
            dataGridResults.DataSource = _testResults.ToList();
            dataGridResults.Refresh();
        }

        private void ShowSummaryStatistics()
        {
            if (_testResults.Count == 0)
                return;

            var stats = CalculateStatistics();
            
            var summary = $"""
                          ?? PERFORMANS ÖZETÝ
                          
                          ?? MODEL 1 ({Path.GetFileName(_model1Path)})
                          Ortalama Süre: {stats.Model1AvgTime:F2} ms
                          Toplam Tespit: {stats.Model1TotalDetections}
                          Ortalama Tespit/Resim: {stats.Model1AvgDetections:F2}
                          
                          ?? MODEL 2 ({Path.GetFileName(_model2Path)})
                          Ortalama Süre: {stats.Model2AvgTime:F2} ms
                          Toplam Tespit: {stats.Model2TotalDetections}
                          Ortalama Tespit/Resim: {stats.Model2AvgDetections:F2}
                          
                          ?? MODEL 3 ({Path.GetFileName(_model3Path)})
                          Ortalama Süre: {stats.Model3AvgTime:F2} ms
                          Toplam Tespit: {stats.Model3TotalDetections}
                          Ortalama Tespit/Resim: {stats.Model3AvgDetections:F2}
                          
                          ?? EN HIZLI: {stats.FastestModel}
                          ?? EN BAÞARILI: {stats.BestDetectionModel}
                          """;

            txtSummary.Text = summary;
        }

        private StatsSummary CalculateStatistics()
        {
            var stats = new StatsSummary();
            
            if (_testResults.Count == 0)
                return stats;

            stats.Model1AvgTime = _testResults.Average(r => r.Model1InferenceTime);
            stats.Model1TotalDetections = _testResults.Sum(r => r.Model1DetectionCount);
            stats.Model1AvgDetections = _testResults.Average(r => r.Model1DetectionCount);

            stats.Model2AvgTime = _testResults.Average(r => r.Model2InferenceTime);
            stats.Model2TotalDetections = _testResults.Sum(r => r.Model2DetectionCount);
            stats.Model2AvgDetections = _testResults.Average(r => r.Model2DetectionCount);

            stats.Model3AvgTime = _testResults.Average(r => r.Model3InferenceTime);
            stats.Model3TotalDetections = _testResults.Sum(r => r.Model3DetectionCount);
            stats.Model3AvgDetections = _testResults.Average(r => r.Model3DetectionCount);

            // En hýzlý model
            var avgTimes = new[] { stats.Model1AvgTime, stats.Model2AvgTime, stats.Model3AvgTime };
            var fastestIndex = Array.IndexOf(avgTimes, avgTimes.Min());
            stats.FastestModel = $"Model {fastestIndex + 1}";

            // En baþarýlý model (en çok tespit)
            var totalDetections = new[] { stats.Model1TotalDetections, stats.Model2TotalDetections, stats.Model3TotalDetections };
            var bestIndex = Array.IndexOf(totalDetections, totalDetections.Max());
            stats.BestDetectionModel = $"Model {bestIndex + 1}";

            return stats;
        }

        private void btnExportResults_Click(object sender, EventArgs e)
        {
            if (_testResults.Count == 0)
            {
                MessageBox.Show("Dýþa aktarýlacak test sonucu bulunamadý!", "Uyarý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Test Sonuçlarýný Kaydet",
                Filter = "CSV Dosyasý|*.csv|Tüm Dosyalar|*.*",
                DefaultExt = "csv",
                FileName = $"model_comparison_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExportToCsv(dialog.FileName);
                MessageBox.Show($"Test sonuçlarý baþarýyla kaydedildi:\n{dialog.FileName}", "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            
            // Header
            writer.WriteLine("Dosya Adý,Model1 Süre(ms),Model1 Tespit,Model2 Süre(ms),Model2 Tespit,Model3 Süre(ms),Model3 Tespit");
            
            // Data
            foreach (var result in _testResults)
            {
                writer.WriteLine($"{result.FileName},{result.Model1InferenceTime},{result.Model1DetectionCount},{result.Model2InferenceTime},{result.Model2DetectionCount},{result.Model3InferenceTime},{result.Model3DetectionCount}");
            }

            // Summary
            writer.WriteLine();
            writer.WriteLine("ÖZETÝ:");
            var stats = CalculateStatistics();
            writer.WriteLine($"Model1 Ortalama Süre,{stats.Model1AvgTime:F2}");
            writer.WriteLine($"Model2 Ortalama Süre,{stats.Model2AvgTime:F2}");
            writer.WriteLine($"Model3 Ortalama Süre,{stats.Model3AvgTime:F2}");
            writer.WriteLine($"Model1 Toplam Tespit,{stats.Model1TotalDetections}");
            writer.WriteLine($"Model2 Toplam Tespit,{stats.Model2TotalDetections}");
            writer.WriteLine($"Model3 Toplam Tespit,{stats.Model3TotalDetections}");
        }

        private void dataGridResults_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridResults.SelectedRows.Count > 0)
            {
                var selectedResult = dataGridResults.SelectedRows[0].DataBoundItem as TestResult;
                if (selectedResult != null)
                {
                    ShowImagePreview(selectedResult);
                }
            }
        }

        private void ShowImagePreview(TestResult result)
        {
            try
            {
                if (File.Exists(result.FilePath))
                {
                    using var bitmap = new Bitmap(result.FilePath);
                    pictureBoxPreview.Image?.Dispose();
                    pictureBoxPreview.Image = new Bitmap(bitmap);
                    
                    lblPreviewInfo.Text = $"{result.FileName} - M1:{result.Model1DetectionCount} M2:{result.Model2DetectionCount} M3:{result.Model3DetectionCount}";
                }
            }
            catch (Exception ex)
            {
                lblPreviewInfo.Text = $"Önizleme hatasý: {ex.Message}";
            }
        }

        private void frmModelComparison_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            
            _detector1?.Dispose();
            _detector2?.Dispose();
            _detector3?.Dispose();
            
            pictureBoxPreview.Image?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _detector1?.Dispose();
                _detector2?.Dispose();
                _detector3?.Dispose();
                pictureBoxPreview.Image?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Data Classes
        
        public class TestResult
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            
            public long Model1InferenceTime { get; set; }
            public int Model1DetectionCount { get; set; }
            public List<LicensePlateDetection> Model1Detections { get; set; } = new();
            
            public long Model2InferenceTime { get; set; }
            public int Model2DetectionCount { get; set; }
            public List<LicensePlateDetection> Model2Detections { get; set; } = new();
            
            public long Model3InferenceTime { get; set; }
            public int Model3DetectionCount { get; set; }
            public List<LicensePlateDetection> Model3Detections { get; set; } = new();
        }

        public class StatsSummary
        {
            public double Model1AvgTime { get; set; }
            public int Model1TotalDetections { get; set; }
            public double Model1AvgDetections { get; set; }
            
            public double Model2AvgTime { get; set; }
            public int Model2TotalDetections { get; set; }
            public double Model2AvgDetections { get; set; }
            
            public double Model3AvgTime { get; set; }
            public int Model3TotalDetections { get; set; }
            public double Model3AvgDetections { get; set; }
            
            public string FastestModel { get; set; } = string.Empty;
            public string BestDetectionModel { get; set; } = string.Empty;
        }
        
        #endregion
    }
}