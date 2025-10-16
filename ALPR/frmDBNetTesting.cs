using ALPR.Detection;
using ALPR.Visualization;
using System.Diagnostics;
using System.Text.Json;

namespace ALPR
{
    public partial class frmDBNetTesting : Form
    {
        private readonly List<string> _modelPaths = new() { "", "", "", "" };
        private readonly List<DBNetTestResult> _testResults = new();
        private string? _testImagePath;
        private bool _isTestRunning;

        public frmDBNetTesting()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            SetupDataGrid();
            SetupGpuSettings();
            LoadDefaultModels();
            UpdateStatus("DBNet test platformu hazýr. Varsayýlan modeller bulunduysa otomatik seçildi.");
        }

        private void LoadDefaultModels()
        {
            try
            {
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "models");
                var defaults = new[]
                {
                    "dbnet_improved_cpu.onnx",
                    "dbnet_improved_gpu.onnx",
                    "dbnet_improved_quantized.onnx",
                    "dbnet_improved_universal.onnx"
                };
                var labels = new[] { lblDBNet1, lblDBNet2, lblDBNet3, lblDBNet4 };

                for (int i = 0; i < defaults.Length; i++)
                {
                    var path = Path.Combine(baseDir, defaults[i]);
                    if (File.Exists(path))
                    {
                        _modelPaths[i] = path;
                        labels[i].Text = $"Model {i + 1}: {Path.GetFileName(path)}";
                        labels[i].ForeColor = Color.DarkGreen;
                    }
                    else
                    {
                        labels[i].Text = $"Model {i + 1}: Seçilmemiþ";
                        labels[i].ForeColor = Color.DarkRed;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Varsayýlan modeller yüklenemedi: {ex.Message}");
            }
        }

        private void SetupDataGrid()
        {
            dataGridViewResults.AutoGenerateColumns = false;
            dataGridViewResults.Columns.Clear();

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ModelName",
                HeaderText = "Model",
                DataPropertyName = "ModelName",
                Width = 150
            });

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "InferenceTime",
                HeaderText = "Çýkarým Süresi (ms)",
                DataPropertyName = "InferenceTimeMs",
                Width = 130
            });

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TextCoverage",
                HeaderText = "Metin Kapsamý (%)",
                DataPropertyName = "TextCoveragePercent",
                Width = 120
            });

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NumContours",
                HeaderText = "Kontur Sayýsý",
                DataPropertyName = "NumContours",
                Width = 100
            });

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MeanConfidence",
                HeaderText = "Ortalama Güven",
                DataPropertyName = "MeanConfidence",
                Width = 120
            });

            dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaxConfidence",
                HeaderText = "Max Güven",
                DataPropertyName = "MaxConfidence",
                Width = 100
            });
        }

        private void SetupGpuSettings()
        {
            bool gpuAvailable = ExecutionProviderHelper.IsGpuAvailable();
            chkUseGpu.Enabled = gpuAvailable;
            chkUseGpu.Checked = gpuAvailable;

            if (!gpuAvailable)
            {
                chkUseGpu.Text = "??? GPU Yok (CPU)";
            }
        }

        private void btnSelectDBNet1_Click(object sender, EventArgs e)
        {
            SelectModel(0, lblDBNet1, "DBNet Model 1");
        }

        private void btnSelectDBNet2_Click(object sender, EventArgs e)
        {
            SelectModel(1, lblDBNet2, "DBNet Model 2");
        }

        private void btnSelectDBNet3_Click(object sender, EventArgs e)
        {
            SelectModel(2, lblDBNet3, "DBNet Model 3");
        }

        private void btnSelectDBNet4_Click(object sender, EventArgs e)
        {
            SelectModel(3, lblDBNet4, "DBNet Model 4");
        }

        private void SelectModel(int index, Label label, string title)
        {
            using var dialog = new OpenFileDialog
            {
                Title = title + " Seçin",
                Filter = "ONNX Model Dosyalarý|*.onnx|Tüm Dosyalar|*.*",
                RestoreDirectory = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models")
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _modelPaths[index] = dialog.FileName;
                label.Text = $"Model {index + 1}: {Path.GetFileName(dialog.FileName)}";
                label.ForeColor = Color.DarkGreen;
                
                UpdateStatus($"? Model {index + 1} seçildi: {Path.GetFileName(dialog.FileName)}");
                
                try
                {
                    var fileInfo = new FileInfo(dialog.FileName);
                    UpdateStatus($"?? Dosya boyutu: {fileInfo.Length / (1024 * 1024):F1} MB");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"?? Dosya bilgisi alýnamadý: {ex.Message}");
                }

                AnalyzeSelectedModel(dialog.FileName, index + 1);
            }
        }

        private void AnalyzeSelectedModel(string modelPath, int modelNumber)
        {
            try
            {
                UpdateStatus($"?? Model {modelNumber} analiz ediliyor...");
                
                var analysis = OnnxModelAnalyzer.AnalyzeModel(modelPath);
                
                if (analysis.IsValid)
                {
                    UpdateStatus($"? Model {modelNumber} analiz baþarýlý!");
                    UpdateStatus($"?? Input: {string.Join(", ", analysis.InputNames)}");
                    UpdateStatus($"?? Output: {string.Join(", ", analysis.OutputNames)}");
                }
                else
                {
                    UpdateStatus($"? Model {modelNumber} analiz hatasý: {analysis.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"?? Model {modelNumber} analiz hatasý: {ex.Message}");
            }
        }

        private void btnSelectTestImage_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Test Resmi Seçin",
                Filter = "Resim Dosyalarý|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _testImagePath = dialog.FileName;
                
                try
                {
                    using var bitmap = new Bitmap(_testImagePath);
                    pictureBoxTestImage.Image?.Dispose();
                    pictureBoxTestImage.Image = new Bitmap(bitmap);
                    
                    UpdateStatus($"??? Test resmi seçildi: {Path.GetFileName(_testImagePath)} ({bitmap.Width}x{bitmap.Height})");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"? Resim yükleme hatasý: {ex.Message}");
                    MessageBox.Show($"Resim yüklenirken hata oluþtu:\n{ex.Message}", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnRunTests_Click(object sender, EventArgs e)
        {
            if (_isTestRunning)
                return;

            // Confirm kaldýrýldý: sadece analiz logla
            ShowModelAnalysisDialog();

            if (!ValidateInputs())
                return;

            await RunDBNetTests();
        }

        private void ShowModelAnalysisDialog()
        {
            var selectedModels = _modelPaths.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
            if (selectedModels.Count == 0)
                return;

            for (int i = 0; i < _modelPaths.Count; i++)
            {
                var path = _modelPaths[i];
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    AnalyzeSelectedModel(path, i + 1);
                }
            }
        }

        private bool ValidateInputs()
        {
            UpdateStatus("?? Girdi doðrulamasý yapýlýyor...");
            
            var selectedModels = _modelPaths.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
            
            UpdateStatus($"?? Seçili model sayýsý: {selectedModels.Count}");
            
            for (int i = 0; i < _modelPaths.Count; i++)
            {
                var path = _modelPaths[i];
                if (!string.IsNullOrEmpty(path))
                {
                    var exists = File.Exists(path);
                    UpdateStatus($"?? Model {i + 1}: {Path.GetFileName(path)} - Var: {exists}");
                }
                else
                {
                    UpdateStatus($"? Model {i + 1}: Seçilmedi");
                }
            }
            
            if (selectedModels.Count == 0)
            {
                MessageBox.Show("En az bir DBNet modeli seçmelisiniz!", "Uyarý", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_testImagePath))
            {
                MessageBox.Show("Test resmi seçmelisiniz!", "Uyarý", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!File.Exists(_testImagePath))
            {
                MessageBox.Show($"Seçilen test resmi dosyasý bulunamadý:\n{_testImagePath}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            UpdateStatus($"? Test resmi: {Path.GetFileName(_testImagePath)} - Var: {File.Exists(_testImagePath)}");
            UpdateStatus($"?? Doðrulama baþarýlý! {selectedModels.Count} model test edilecek.");
            return true;
        }

        private async Task RunDBNetTests()
        {
            _isTestRunning = true;
            _testResults.Clear();
            
            try
            {
                btnRunTests.Enabled = false;
                btnRunTests.Text = "? Test Çalýþýyor...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                
                var selectedModels = _modelPaths
                    .Select((path, index) => new { Path = path, Index = index })
                    .Where(m => !string.IsNullOrEmpty(m.Path) && File.Exists(m.Path))
                    .ToList();

                UpdateStatus($"?? Test edilecek model sayýsý: {selectedModels.Count}");

                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Maximum = selectedModels.Count;
                progressBar.Value = 0;

                for (int i = 0; i < selectedModels.Count; i++)
                {
                    var model = selectedModels[i];
                    UpdateStatus($"?? Test ediliyor: Model {model.Index + 1} ({i + 1}/{selectedModels.Count})");
                    
                    var result = await TestSingleModel(model.Path, model.Index + 1);
                    if (result != null)
                    {
                        _testResults.Add(result);
                        UpdateStatus($"? Model {model.Index + 1} baþarýyla test edildi! Süre: {result.InferenceTimeMs}ms");
                    }
                    else
                    {
                        UpdateStatus($"? Model {model.Index + 1} test baþarýsýz!");
                    }
                    
                    progressBar.Value = i + 1;
                    await Task.Delay(100);
                }

                DisplayResults();
                UpdateVisualizationOptions();
                UpdateJsonOptions();
                
                if (_testResults.Count > 0)
                {
                    UpdateStatus($"?? Test tamamlandý! {_testResults.Count} model baþarýyla test edildi.");
                    ShowMultiModelVisualization();
                }
                else
                {
                    UpdateStatus($"?? Hiçbir model baþarýyla test edilemedi! Model formatlarý uyumlu deðil.");
                    MessageBox.Show("Hiçbir model test edilemedi!", "Test Baþarýsýz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"?? Test genel hatasý: {ex.Message}");
                MessageBox.Show($"Test sýrasýnda genel hata oluþtu:\n\n{ex.Message}", "Kritik Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isTestRunning = false;
                btnRunTests.Enabled = true;
                btnRunTests.Text = "?? Testleri Baþlat";
                progressBar.Visible = false;
            }
        }

        private async Task<DBNetTestResult?> TestSingleModel(string modelPath, int modelNumber)
        {
            try
            {
                UpdateStatus($"?? Model {modelNumber} yükleniyor: {Path.GetFileName(modelPath)}");
                
                return await Task.Run(() =>
                {
                    try
                    {
                        UpdateStatus($"?? Model {modelNumber} detector oluþturuluyor...");
                        using var detector = new DBNetTextDetector(modelPath, chkUseGpu.Checked);
                        
                        UpdateStatus($"??? Model {modelNumber} resim yükleniyor...");
                        using var bitmap = new Bitmap(_testImagePath!);
                        
                        UpdateStatus($"? Model {modelNumber} çýkarým baþlýyor...");
                        var sw = Stopwatch.StartNew();
                        var result = detector.Detect(bitmap, (float)nudConfidenceThreshold.Value);
                        sw.Stop();

                        UpdateStatus($"? Model {modelNumber} test tamamlandý: {sw.ElapsedMilliseconds}ms");

                        var testResult = new DBNetTestResult
                        {
                            ModelName = $"Model {modelNumber} ({Path.GetFileNameWithoutExtension(modelPath)})",
                            ModelPath = modelPath,
                            InferenceTimeMs = sw.ElapsedMilliseconds,
                            TextCoveragePercent = result.Prediction.TextCoveragePercent,
                            NumContours = result.Prediction.NumContours,
                            MeanConfidence = result.Prediction.ConfidenceStats.MeanProbability,
                            MaxConfidence = result.Prediction.ConfidenceStats.MaxProbability,
                            MinConfidence = result.Prediction.ConfidenceStats.MinProbability,
                            DBNetResult = result
                        };

                        UpdateStatus($"?? Model {modelNumber} sonuçlar: Kapsam %{testResult.TextCoveragePercent:F2}, Kontur: {testResult.NumContours}");
                        
                        return testResult;
                    }
                    catch (Exception innerEx)
                    {
                        var errorMessage = $"?? Model {modelNumber} iç hata: {innerEx.Message}";
                        if (innerEx.InnerException != null)
                        {
                            errorMessage += $" | Ýç hata: {innerEx.InnerException.Message}";
                        }
                        UpdateStatus(errorMessage);
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"?? Model {modelNumber} test hatasý: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Ýç hata: {ex.InnerException.Message}";
                }
                UpdateStatus(errorMessage);
                
                return null;
            }
        }

        private void ShowMultiModelVisualization()
        {
            if (_testResults.Count == 0 || string.IsNullOrEmpty(_testImagePath))
                return;

            try
            {
                UpdateStatus("?? Çoklu model görselleþtirmesi oluþturuluyor...");

                using var originalBitmap = new Bitmap(_testImagePath);
                
                var visualizationData = new List<ModelVisualizationData>();
                
                for (int i = 0; i < _testResults.Count; i++)
                {
                    var testResult = _testResults[i];
                    if (testResult.DBNetResult?.BinaryMap != null && testResult.DBNetResult.MapDimensions.Length >= 2)
                    {
                        var data = new ModelVisualizationData
                        {
                            ModelName = testResult.ModelName,
                            BinaryMap = testResult.DBNetResult.BinaryMap,
                            MapWidth = testResult.DBNetResult.MapDimensions[1],
                            MapHeight = testResult.DBNetResult.MapDimensions[0],
                            OriginalWidth = originalBitmap.Width,
                            OriginalHeight = originalBitmap.Height,
                            Threshold = (float)nudConfidenceThreshold.Value,
                            TextCoverage = testResult.TextCoveragePercent,
                            ContourCount = testResult.NumContours,
                            InferenceTime = testResult.InferenceTimeMs
                        };
                        visualizationData.Add(data);
                    }
                }

                if (visualizationData.Count > 0)
                {
                    var visualizedImage = DBNetVisualizer.VisualizeMultipleModels(originalBitmap, visualizationData);
                    pictureBoxVisualization.Image?.Dispose();
                    pictureBoxVisualization.Image = visualizedImage;
                    
                    UpdateStatus($"?? Çoklu model görselleþtirmesi hazýr! {visualizationData.Count} model gösteriliyor.");
                }
                else
                {
                    UpdateStatus("?? Görselleþtirme için yeterli veri yok.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"? Görselleþtirme hatasý: {ex.Message}");
            }
        }

        private void DisplayResults()
        {
            if (_testResults.Count == 0)
            {
                UpdateStatus("?? Gösterilecek sonuç yok");
                return;
            }

            var displayData = _testResults.Select(r => new
            {
                ModelName = r.ModelName,
                InferenceTimeMs = r.InferenceTimeMs,
                TextCoveragePercent = Math.Round(r.TextCoveragePercent, 2),
                NumContours = r.NumContours,
                MeanConfidence = Math.Round(r.MeanConfidence, 4),
                MaxConfidence = Math.Round(r.MaxConfidence, 4)
            }).ToList();

            dataGridViewResults.DataSource = displayData;
            dataGridViewResults.Refresh();
            
            UpdateStatus($"?? {_testResults.Count} model sonucu tabloda gösteriliyor");
        }

        private void UpdateVisualizationOptions()
        {
            listBoxModels.Items.Clear();
            listBoxModels.Items.Add("?? Tüm Modeller (Çoklu Görselleþtirme)");
            foreach (var result in _testResults)
            {
                listBoxModels.Items.Add(result.ModelName);
            }
        }

        private void UpdateJsonOptions()
        {
            comboBoxJsonModel.Items.Clear();
            foreach (var result in _testResults)
            {
                comboBoxJsonModel.Items.Add(result.ModelName);
            }
        }

        private void listBoxModels_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxModels.SelectedIndex == 0)
            {
                ShowMultiModelVisualization();
            }
            else if (listBoxModels.SelectedIndex > 0 && listBoxModels.SelectedIndex <= _testResults.Count)
            {
                var selectedResult = _testResults[listBoxModels.SelectedIndex - 1];
                ShowSingleModelVisualization(selectedResult);
            }
        }

        private void ShowSingleModelVisualization(DBNetTestResult result)
        {
            try
            {
                if (string.IsNullOrEmpty(_testImagePath))
                    return;

                using var originalBitmap = new Bitmap(_testImagePath);
                
                if (result.DBNetResult?.BinaryMap != null && result.DBNetResult.MapDimensions.Length >= 2)
                {
                    var visualizationData = new List<ModelVisualizationData>
                    {
                        new ModelVisualizationData
                        {
                            ModelName = result.ModelName,
                            BinaryMap = result.DBNetResult.BinaryMap,
                            MapWidth = result.DBNetResult.MapDimensions[1],
                            MapHeight = result.DBNetResult.MapDimensions[0],
                            OriginalWidth = originalBitmap.Width,
                            OriginalHeight = originalBitmap.Height,
                            Threshold = (float)nudConfidenceThreshold.Value,
                            TextCoverage = result.TextCoveragePercent,
                            ContourCount = result.NumContours,
                            InferenceTime = result.InferenceTimeMs
                        }
                    };

                    var visualizedImage = DBNetVisualizer.VisualizeMultipleModels(originalBitmap, visualizationData);
                    pictureBoxVisualization.Image?.Dispose();
                    pictureBoxVisualization.Image = visualizedImage;
                    
                    UpdateStatus($"?? Tek model görselleþtirmesi: {result.ModelName}");
                }
                else
                {
                    pictureBoxVisualization.Image?.Dispose();
                    pictureBoxVisualization.Image = new Bitmap(originalBitmap);
                    
                    UpdateStatus($"??? Orijinal resim: {result.ModelName} - Binary map veri yok");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"? Görselleþtirme hatasý: {ex.Message}");
            }
        }

        private void comboBoxJsonModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxJsonModel.SelectedIndex >= 0 && comboBoxJsonModel.SelectedIndex < _testResults.Count)
            {
                var selectedResult = _testResults[comboBoxJsonModel.SelectedIndex];
                ShowJsonOutput(selectedResult);
            }
        }

        private void ShowJsonOutput(DBNetTestResult result)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(result.DBNetResult, options);
                txtJsonOutput.Text = json;
                
                UpdateStatus($"?? JSON çýktýsý: {result.ModelName}");
            }
            catch (Exception ex)
            {
                txtJsonOutput.Text = $"JSON serileþtirme hatasý: {ex.Message}";
                UpdateStatus($"? JSON hatasý: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatus(message));
                return;
            }

            lblStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            
            Application.DoEvents();
        }

        private void frmDBNetTesting_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureBoxTestImage.Image?.Dispose();
            pictureBoxVisualization.Image?.Dispose();
        }

        #region Data Classes

        public class DBNetTestResult
        {
            public string ModelName { get; set; } = string.Empty;
            public string ModelPath { get; set; } = string.Empty;
            public long InferenceTimeMs { get; set; }
            public double TextCoveragePercent { get; set; }
            public int NumContours { get; set; }
            public double MeanConfidence { get; set; }
            public double MaxConfidence { get; set; }
            public double MinConfidence { get; set; }
            public DBNetResult? DBNetResult { get; set; }
        }

        #endregion
    }
}
