using ALPR.Detection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace ALPR
{
    public partial class frmALPR : Form
    {
        private const string DefaultModelPathPlate = "models/LicencePlateDetection_Gpu.onnx";
        private const string ModelPathChar = "models/PlateLetterExtractionS.onnx";
        private const int MaxLogLines = 100;
        private const int LogTrimLines = 50;

        private string _currentPlateModelPath = DefaultModelPathPlate;
        private LicensePlateDetector? _plateDetector;
        private PlateCharDetector? _charDetector;
        private VideoCapture? _capture;
        private bool _isVideoPlaying;
        private string? _selectedVideoPath;
        private Thread? _videoThread;
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private readonly string _outputFolder = "plates";
        private bool _isDisposed;

        public frmALPR()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Directory.CreateDirectory(_outputFolder);
            UpdateCurrentModelLabel();
            SetupGpuAndModels();
        }

        private void UpdateCurrentModelLabel()
        {
            var modelName = Path.GetFileNameWithoutExtension(_currentPlateModelPath);
            lblCurrentModel.Text = $"Model: {modelName}";

            if (!File.Exists(_currentPlateModelPath))
            {
                lblCurrentModel.Text += " (Bulunamadý)";
                lblCurrentModel.ForeColor = Color.Red;
            }
            else
            {
                lblCurrentModel.ForeColor = Color.DarkGreen;
            }
        }

        private void SetupGpuAndModels()
        {
            bool gpuAvailable = ExecutionProviderHelper.IsGpuAvailable();
            chkUseGpu.Enabled = gpuAvailable;
            chkUseGpu.Checked = gpuAvailable;

            Log(gpuAvailable
                ? $"?? GPU Desteði: {ExecutionProviderHelper.GetAvailableProviders()}"
                : "??? GPU desteði bulunamadý. CPU kullanýlacak.");

            LoadDetectors();
        }

        private void LoadDetectors()
        {
            try
            {
                Log($"?? Model dosyalarý kontrol ediliyor...");
                Log($"   Çalýþma dizini: {Directory.GetCurrentDirectory()}");
                Log($"   Plaka modeli: {_currentPlateModelPath} - Var mý: {File.Exists(_currentPlateModelPath)}");
                Log($"   Karakter modeli: {ModelPathChar} - Var mý: {File.Exists(ModelPathChar)}");

                if (!File.Exists(_currentPlateModelPath))
                {
                    Log($"? Plaka modeli bulunamadý: {_currentPlateModelPath}");
                    Log($"?? Model dosyalarýný seçin veya '{Path.Combine(Directory.GetCurrentDirectory(), "models")}' klasörüne koyun");
                    UpdateCurrentModelLabel();
                    return;
                }

                if (!File.Exists(ModelPathChar))
                {
                    Log($"? Karakter modeli bulunamadý: {ModelPathChar}");
                    Log($"?? Model dosyalarýný '{Path.Combine(Directory.GetCurrentDirectory(), "models")}' klasörüne koyun");
                    return;
                }

                _plateDetector?.Dispose();
                _charDetector?.Dispose();

                bool useGpu = chkUseGpu.Checked && chkUseGpu.Enabled;

                _plateDetector = new LicensePlateDetector(_currentPlateModelPath, useGpu);
                _charDetector = new PlateCharDetector(ModelPathChar, swapRB: false, useGpu);

                Log($"? Modeller yüklendi. GPU: {(useGpu ? "Aktif" : "Pasif")}");
                Log($"?? Aktif plaka modeli: {Path.GetFileName(_currentPlateModelPath)}");
                UpdateCurrentModelLabel();
            }
            catch (Exception ex)
            {
                Log($"? Model yükleme hatasý: {ex.Message}");
                UpdateCurrentModelLabel();
            }
        }

        private void btnSelectPlateModel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Plaka Tespit Modeli Seçin",
                Filter = "ONNX Model Dosyalarý|*.onnx|Tüm Dosyalar|*.*",
                RestoreDirectory = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models")
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var oldModelPath = _currentPlateModelPath;
                _currentPlateModelPath = dialog.FileName;

                Log($"?? Yeni plaka modeli seçildi: {Path.GetFileName(_currentPlateModelPath)}");

                // Video iþleme durumu kontrolü
                if (_isVideoPlaying)
                {
                    var result = MessageBox.Show(
                        "Video iþleme devam ediyor. Modeli deðiþtirmek için video iþlemeyi durdurmak gerekiyor. Devam edilsin mi?",
                        "Video Ýþleme Aktif",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        StopVideoProcessing();
                        LoadDetectors();
                    }
                    else
                    {
                        // Ýptal edildi, eski modeli geri yükle
                        _currentPlateModelPath = oldModelPath;
                        Log("? Model deðiþikliði iptal edildi.");
                        UpdateCurrentModelLabel();
                        return;
                    }
                }
                else
                {
                    LoadDetectors();
                }
            }
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Resim Seçin",
                Filter = "Resim Dosyalarý|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ProcessImage(dialog.FileName);
            }
        }

        private void btnSelectVideo_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Video Seçin",
                Filter = "Video Dosyalarý|*.mp4;*.avi;*.mov;*.mkv;*.wmv|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _selectedVideoPath = dialog.FileName;
                btnStartVideo.Enabled = true;
                Log($"?? Video seçildi: {Path.GetFileName(_selectedVideoPath)}");
            }
        }

        private void btnStartVideo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedVideoPath) || _isVideoPlaying)
                return;

            StartVideoProcessing();
        }

        private void btnStopVideo_Click(object sender, EventArgs e)
        {
            StopVideoProcessing();
        }

        private void chkUseGpu_CheckedChanged(object sender, EventArgs e)
        {
            if (_plateDetector != null || _charDetector != null)
            {
                Log("?? GPU ayarý deðiþti. Modeller yeniden yükleniyor...");
                LoadDetectors();
            }
        }

        private void btnModelComparison_Click(object sender, EventArgs e)
        {
            try
            {
                var modelComparisonForm = new frmModelComparison();
                modelComparisonForm.Show();
                Log("?? Model karþýlaþtýrma ekraný açýldý.");
            }
            catch (Exception ex)
            {
                Log($"? Model karþýlaþtýrma ekraný açýlýrken hata: {ex.Message}");
                MessageBox.Show($"Model karþýlaþtýrma ekraný açýlýrken hata oluþtu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // PaddleOCR butonu
        private void btnPaddleOCR_Click(object sender, EventArgs e)
        {
            try
            {
                var paddleOCRForm = new frmPaddleOCR();
                paddleOCRForm.Show();
                Log("?? PaddleOCR platformu açýldý.");
            }
            catch (Exception ex)
            {
                Log($"? PaddleOCR platformu açýlýrken hata: {ex.Message}");
                MessageBox.Show($"PaddleOCR platformu açýlýrken hata oluþtu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TesseractOCR butonu
        private void btnTesseractOCR_Click(object sender, EventArgs e)
        {
            try
            {
                var tesseractOCRForm = new frmTesseractOCR();
                tesseractOCRForm.Show();
                Log("?? Tesseract OCR platformu açýldý.");
            }
            catch (Exception ex)
            {
                Log($"? Tesseract OCR platformu açýlýrken hata: {ex.Message}");
                MessageBox.Show($"Tesseract OCR platformu açýlýrken hata oluþtu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatchProcess_Click(object sender, EventArgs e)
        {
            if (!AreDetectorsReady())
            {
                Log("?? Detektorlar hazýr deðil! Model dosyalarýný kontrol edin.");
                MessageBox.Show("Model dosyalarý yüklenmemiþ! Önce plaka modelini yükleyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Plaka tespiti yapýlacak resimlerin bulunduðu klasörü seçin",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                ProcessBatchImages(folderDialog.SelectedPath);
            }
        }

        private void ProcessBatchImages(string folderPath)
        {
            try
            {
                Log($"?? Toplu iþleme baþlýyor: {folderPath}");

                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
                var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();

                if (imageFiles.Length == 0)
                {
                    Log("?? Klasörde desteklenen resim dosyasý bulunamadý!");
                    MessageBox.Show("Seçilen klasörde resim dosyasý bulunamadý!\nDesteklenen formatlar: JPG, JPEG, PNG, BMP, TIFF", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Log($"?? {imageFiles.Length} resim dosyasý bulundu.");

                // Progress için deðiþkenler
                int processedCount = 0;
                int totalPlatesFound = 0;
                var stopwatch = Stopwatch.StartNew();

                // Her resim için iþlem yap
                foreach (var imagePath in imageFiles)
                {
                    try
                    {
                        processedCount++;
                        var fileName = Path.GetFileName(imagePath);

                        Log($"?? [{processedCount}/{imageFiles.Length}] Ýþleniyor: {fileName}");

                        using var bitmap = new Bitmap(imagePath);
                        var plateResult = _plateDetector!.Detect(
                            bitmap,
                            (float)nudConfidenceThreshold.Value,
                            chkEnableNMS.Checked,
                            (float)nudNMSThreshold.Value);

                        if (plateResult.Detections.Count == 0)
                        {
                            Log($"  ?? Plaka bulunamadý: {fileName}");
                            continue;
                        }

                        var detectedPlates = new List<string>();
                        foreach (var plate in plateResult.Detections)
                        {
                            totalPlatesFound++;

                            // Plaka resmini kaydet
                            SavePlateImageBatch(bitmap, plate, fileName, totalPlatesFound);

                            detectedPlates.Add($"{plate.Confidence:P1}");
                        }

                        Log($"  ? {plateResult.Detections.Count} plaka bulundu: {string.Join(", ", detectedPlates)}");

                        // Son iþlenen resmi göster (isteðe baðlý)
                        if (processedCount == imageFiles.Length)
                        {
                            using var resultBitmap = DrawPlatesOnImage(bitmap, plateResult.Detections);
                            pictureBoxImage.Image?.Dispose();
                            pictureBoxImage.Image = new Bitmap(resultBitmap);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"? {Path.GetFileName(imagePath)} iþlenirken hata: {ex.Message}");
                    }
                }

                stopwatch.Stop();

                Log($"?? Toplu iþleme tamamlandý!");
                Log($"?? Özet: {processedCount} resim iþlendi, {totalPlatesFound} plaka bulundu");
                Log($"?? Toplam süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye");
                Log($"?? Ortalama hýz: {(processedCount / stopwatch.Elapsed.TotalSeconds):F2} resim/saniye");

                MessageBox.Show(
                    $"Toplu iþleme tamamlandý!\n\n" +
                    $"Ýþlenen resim: {processedCount}\n" +
                    $"Bulunan plaka: {totalPlatesFound}\n" +
                    $"Süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye\n\n" +
                    $"Plaka resimleri '{_outputFolder}' klasörüne kaydedildi.",
                    "Toplu Ýþleme Tamamlandý",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"? Toplu iþleme hatasý: {ex.Message}");
                MessageBox.Show($"Toplu iþleme sýrasýnda hata oluþtu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SavePlateImageBatch(Bitmap originalBitmap, LicensePlateDetection plate, string originalFileName, int plateIndex)
        {
            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                var filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                var filepath = Path.Combine(_outputFolder, filename);

                plateBitmap.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log($"    ?? Plaka kaydedildi: {filename}");
            }
            catch (Exception ex)
            {
                Log($"?? Plaka kaydetme hatasý: {ex.Message}");
            }
        }

        private Bitmap DrawPlatesOnImage(Bitmap originalBitmap, IReadOnlyList<LicensePlateDetection> plates)
        {
            var resultBitmap = new Bitmap(originalBitmap);
            using var g = Graphics.FromImage(resultBitmap);

            foreach (var plate in plates)
            {
                var rect = plate.GetRectangle();

                using var platePen = new Pen(Color.Lime, 3);
                g.DrawRectangle(platePen, rect);

                var confidence = $"{plate.Confidence:P1}";

                using var font = new Font("Arial", 12, FontStyle.Bold);
                using var bgBrush = new SolidBrush(Color.FromArgb(180, Color.Black));
                using var textBrush = new SolidBrush(Color.White);

                var textSize = g.MeasureString(confidence, font);
                var textRect = new RectangleF(rect.X, rect.Y - textSize.Height - 5, textSize.Width + 10, textSize.Height + 5);

                g.FillRectangle(bgBrush, textRect);
                g.DrawString(confidence, font, textBrush, textRect.X + 5, textRect.Y + 2);
            }

            return resultBitmap;
        }

        private void ProcessImage(string imagePath)
        {
            // Debug: Detector durumunu kontrol et
            if (!AreDetectorsReady())
            {
                Log("?? Detektorlar hazýr deðil! Model dosyalarýný kontrol edin.");

                // Model olmadan da resmi göster
                try
                {
                    using var bitmap = new Bitmap(imagePath);
                    pictureBoxImage.Image?.Dispose();
                    pictureBoxImage.Image = new Bitmap(bitmap);
                    Log($"??? Resim yüklendi (model olmadan): {Path.GetFileName(imagePath)}");
                }
                catch (Exception ex)
                {
                    Log($"? Resim yükleme hatasý: {ex.Message}");
                }
                return;
            }

            try
            {
                Log($"?? Resim iþleniyor: {Path.GetFileName(imagePath)}");

                using var bitmap = new Bitmap(imagePath);
                using var result = ProcessFrame(bitmap);

                if (result != null)
                {
                    pictureBoxImage.Image?.Dispose();
                    pictureBoxImage.Image = new Bitmap(result);
                }
            }
            catch (Exception ex)
            {
                Log($"? Resim iþleme hatasý: {ex.Message}");
            }
        }

        private void StartVideoProcessing()
        {
            if (!AreDetectorsReady())
                return;

            try
            {
                _capture?.Release();
                _capture = new VideoCapture(_selectedVideoPath);

                if (!_capture.IsOpened())
                {
                    Log("? Video açýlamadý!");
                    return;
                }

                _isVideoPlaying = true;
                ResetFrameCounter();
                SetVideoButtons(playing: true);

                Log($"?? Video iþleme baþladý: {Path.GetFileName(_selectedVideoPath)}");

                _videoThread = new Thread(ProcessVideoFrames)
                {
                    IsBackground = true,
                    Name = "VideoProcessing"
                };
                _videoThread.Start();
            }
            catch (Exception ex)
            {
                Log($"? Video baþlatma hatasý: {ex.Message}");
                StopVideoProcessing();
            }
        }

        private void StopVideoProcessing()
        {
            _isVideoPlaying = false;

            try
            {
                _videoThread?.Join(2000);
                _capture?.Release();
                _capture = null;
            }
            catch (Exception ex)
            {
                SafeLog($"?? Video durdurma hatasý: {ex.Message}");
            }

            SetVideoButtons(playing: false);
            SafeLog("?? Video iþleme durduruldu");
            SafeUpdateFpsDisplay(0);
        }

        private void ProcessVideoFrames()
        {
            int frameSkip = 0;
            int targetFps = 30;
            int frameDelay = 1000 / targetFps;

            try
            {
                using var frame = new Mat();

                while (_isVideoPlaying && _capture != null && !_isDisposed)
                {
                    var frameStart = DateTime.Now;

                    if (!_capture.Read(frame) || frame.Empty())
                        break;

                    int skipValue = GetFrameSkipValue();
                    if (frameSkip < skipValue)
                    {
                        frameSkip++;
                        continue;
                    }
                    frameSkip = 0;

                    using var bitmap = BitmapConverter.ToBitmap(frame);
                    using var result = ProcessFrame(bitmap);

                    if (result != null)
                    {
                        SafeUpdateImage(result);
                    }

                    UpdateFrameCounter();

                    var elapsed = (int)(DateTime.Now - frameStart).TotalMilliseconds;
                    var sleepTime = Math.Max(1, frameDelay - elapsed);
                    Thread.Sleep(sleepTime);
                }
            }
            catch (Exception ex)
            {
                SafeLog($"? Video iþleme hatasý: {ex.Message}");
            }
            finally
            {
                if (_isVideoPlaying)
                {
                    Invoke(StopVideoProcessing);
                }
            }
        }

        private Bitmap? ProcessFrame(Bitmap originalBitmap)
        {
            if (!AreDetectorsReady())
                return null;

            try
            {
                var sw = Stopwatch.StartNew();

                var plateResult = _plateDetector!.Detect(
                    originalBitmap,
                    (float)nudConfidenceThreshold.Value,
                    chkEnableNMS.Checked,
                    (float)nudNMSThreshold.Value);

                sw.Stop();

                if (plateResult.Detections.Count == 0)
                {
                    SafeLog($"?? Plaka bulunamadý ({sw.ElapsedMilliseconds}ms)");
                    return new Bitmap(originalBitmap);
                }

                var resultBitmap = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(resultBitmap);

                var detectedPlates = new List<string>();
                bool saveImages = GetSaveImagesValue();

                foreach (var plate in plateResult.Detections)
                {
                    var plateText = ProcessPlateCharacters(originalBitmap, plate);
                    DrawPlateDetection(g, plate, plateText);

                    if (!string.IsNullOrEmpty(plateText))
                    {
                        detectedPlates.Add($"{plateText} ({plate.Confidence:P1})");
                    }
                    else
                    {
                        detectedPlates.Add($"[Okunamadý] ({plate.Confidence:P1})");
                    }

                    if (saveImages && !string.IsNullOrEmpty(plateText))
                    {
                        SavePlateImage(originalBitmap, plate, plateText);
                    }
                }

                // Tespit edilen plakalar listesiyle log mesajý
                var platesList = string.Join(", ", detectedPlates);
                SafeLog($"? {plateResult.Detections.Count} plaka tespit edildi: {platesList} ({sw.ElapsedMilliseconds}ms)");

                return resultBitmap;
            }
            catch (Exception ex)
            {
                SafeLog($"? Frame iþleme hatasý: {ex.Message}");
                return new Bitmap(originalBitmap);
            }
        }

        private string ProcessPlateCharacters(Bitmap originalBitmap, LicensePlateDetection plate)
        {
            if (_charDetector == null)
                return string.Empty;

            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return string.Empty;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);

                var charResult = _charDetector.Detect(
                    plateBitmap,
                    GetCharConfidenceValue(),
                    GetEnableNmsValue(),
                    GetNmsThresholdValue());

                if (charResult.Detections.Count == 0)
                    return string.Empty;

                // Debug modu kontrolü
                bool debugMode = GetDebugModeValue();

                if (debugMode)
                {
                    // Debug modunu aktifleþtir
                    OcrStitcher.EnableDebug = true;
                    OcrStitcher.DebugLogger = (msg) => SafeLog($"  ?? {msg}");

                    SafeLog($"?? DEBUG: {charResult.Detections.Count} karakter tespit edildi:");
                    for (int i = 0; i < charResult.Detections.Count; i++)
                    {
                        var c = charResult.Detections[i];
                        SafeLog($"  [{i}] '{c.Class}' @ ({c.X},{c.Y}) size:{c.Width}x{c.Height} conf:{c.Confidence:P1}");
                    }
                }
                else
                {
                    OcrStitcher.EnableDebug = false;
                }

                var result = OcrStitcher.Stitch(charResult.Detections, "left_to_right");

                if (debugMode)
                {
                    SafeLog($"?? DEBUG: Stitch sonucu: '{result}'");
                    OcrStitcher.EnableDebug = false; // Debug'ý kapat
                }

                return result;
            }
            catch (Exception ex)
            {
                SafeLog($"?? Karakter iþleme hatasý: {ex.Message}");
                return string.Empty;
            }
        }

        private static void DrawPlateDetection(Graphics g, LicensePlateDetection plate, string plateText)
        {
            var rect = plate.GetRectangle();

            using var platePen = new Pen(Color.Lime, 3);
            g.DrawRectangle(platePen, rect);

            var confidence = $"{plate.Confidence:P1}";
            var displayText = string.IsNullOrEmpty(plateText) ? confidence : $"{plateText} ({confidence})";

            using var font = new Font("Arial", 12, FontStyle.Bold);
            using var bgBrush = new SolidBrush(Color.FromArgb(180, Color.Black));
            using var textBrush = new SolidBrush(Color.White);

            var textSize = g.MeasureString(displayText, font);
            var textRect = new RectangleF(rect.X, rect.Y - textSize.Height - 5, textSize.Width + 10, textSize.Height + 5);

            g.FillRectangle(bgBrush, textRect);
            g.DrawString(displayText, font, textBrush, textRect.X + 5, textRect.Y + 2);
        }

        private void SavePlateImage(Bitmap originalBitmap, LicensePlateDetection plate, string plateText)
        {
            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var safeFileName = string.Concat(plateText.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                var filename = $"{timestamp}_{safeFileName}.jpg";
                var filepath = Path.Combine(_outputFolder, filename);

                plateBitmap.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);
                SafeLog($"?? Plaka kaydedildi: {filename}");
            }
            catch (Exception ex)
            {
                SafeLog($"?? Plaka kaydetme hatasý: {ex.Message}");
            }
        }

        // Helper methods
        private bool AreDetectorsReady() => _plateDetector != null && _charDetector != null;

        private void ResetFrameCounter()
        {
            _frameCount = 0;
            _lastFpsUpdate = DateTime.Now;
        }

        private void SetVideoButtons(bool playing)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetVideoButtons(playing));
                return;
            }

            btnStartVideo.Enabled = !playing;
            btnStopVideo.Enabled = playing;
            btnSelectVideo.Enabled = !playing;
            btnSelectPlateModel.Enabled = !playing; // Model deðiþtirme video sýrasýnda devre dýþý
        }

        private int GetFrameSkipValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => (int)nudFrameSkip.Value);
            }
            return (int)nudFrameSkip.Value;
        }

        private float GetCharConfidenceValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => (float)nudCharConfidence.Value);
            }
            return (float)nudCharConfidence.Value;
        }

        private bool GetEnableNmsValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => chkEnableNMS.Checked);
            }
            return chkEnableNMS.Checked;
        }

        private float GetNmsThresholdValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => (float)nudNMSThreshold.Value);
            }
            return (float)nudNMSThreshold.Value;
        }

        private bool GetSaveImagesValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => chkSavePlates.Checked);
            }
            return chkSavePlates.Checked;
        }

        private bool GetDebugModeValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => chkDebugMode.Checked);
            }
            return chkDebugMode.Checked;
        }

        private void SafeUpdateImage(Bitmap result)
        {
            if (InvokeRequired)
            {
                Invoke(() => SafeUpdateImage(result));
                return;
            }

            pictureBoxImage.Image?.Dispose();
            pictureBoxImage.Image = new Bitmap(result);
        }

        private void SafeUpdateFpsDisplay(double fps)
        {
            if (InvokeRequired)
            {
                Invoke(() => SafeUpdateFpsDisplay(fps));
                return;
            }

            lblFps.Text = $"FPS: {fps:F2}";
        }

        private void SafeLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(message));
                return;
            }
            Log(message);
        }

        private void UpdateFrameCounter()
        {
            _frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - _lastFpsUpdate).TotalSeconds;

            if (elapsed >= 1.0)
            {
                var fps = _frameCount / elapsed;
                SafeUpdateFpsDisplay(fps);
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
        }

        private void Log(string message)
        {
            if (_isDisposed) return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";

            if (txtLog.Lines.Length > MaxLogLines)
            {
                var lines = txtLog.Lines.Skip(LogTrimLines).ToArray();
                txtLog.Lines = lines;
            }

            txtLog.AppendText(logMessage + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void frmALPR_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isDisposed = true;
            StopVideoProcessing();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                _plateDetector?.Dispose();
                _charDetector?.Dispose();
                _capture?.Dispose();
                pictureBoxImage.Image?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void btnFastOCR_Click(object sender, EventArgs e)
        {
            var frmFastOCR = new frmFastOCR();
            frmFastOCR.Show();
        }
    }
}