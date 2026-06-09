using ALPR.Detection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace ALPR
{
    public partial class frmALPR : Form
    {
        private const string DefaultModelPathPlateV1 = "models/LicencePlateDetection_Gpu.onnx";
        private const string DefaultModelPathPlateV2 = "models/plateRecognitionV2.onnx";
        private const string ModelPathChar = "models/cct_s_v1_global.onnx"; // Varsayılan S
        private const string ModelPathTitanV8 = "models/titan_armor_v8.onnx";
        private const string ModelPathParseq = "models/parseq_fp16_fp32_sim.onnx";
        private const int MaxLogLines = 100;
        private const int LogTrimLines = 50;

        private enum PlateDetectionModelType { V1, V2 }
        private PlateDetectionModelType _currentPlateModelType = PlateDetectionModelType.V1;
        private string _currentPlateModelPath = DefaultModelPathPlateV1;
        private LicensePlateDetector? _plateDetectorV1;
        private YoloOnnxRunner.PlateRecognitionModel? _plateDetectorV2;
        private PlateCharDetector? _charDetector; // Varsayılan (S)
        private TitanArmorV8Detector? _titanV8Detector;
        private ParseqDetector? _parseqDetector;
        private VideoCapture? _capture;
        private bool _isVideoPlaying;
        private bool _isVideoPaused;          // ← ekle
        private ManualResetEventSlim _pauseEvent = new(true); // ← ekle
        private string? _selectedVideoPath;
        private Thread? _videoThread;
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private readonly string _outputFolder = "plates";
        private bool _isDisposed;
        // Tracks whether detectors were loaded with GPU enabled
        private bool _detectorsLoadedWithGpu;

        public frmALPR()
        {
            InitializeComponent();
            this.Load += frmALPR_Load;
        }

        private bool _isInitializing;

        private void frmALPR_Load(object? sender, EventArgs e)
        {
            InitializeAppLogic();
        }

        // Helper methods
        private bool AreDetectorsReady() => IsPlateDetectorReady() && (_charDetector != null || _titanV8Detector != null || _parseqDetector != null);

        private bool IsPlateDetectorReady() => _currentPlateModelType == PlateDetectionModelType.V2 ? _plateDetectorV2 != null : _plateDetectorV1 != null;

        private void InitializeAppLogic()
        {
            _isInitializing = true;
            Directory.CreateDirectory(_outputFolder);
            cbOcrModel.SelectedIndex = 0;
            cmbPlateModelType.SelectedIndex = 0;
            ApplyPlateModelTypeSelection(reloadDetectors: false);
            UpdateCurrentModelLabel();

            bool gpuAvailable = ExecutionProviderHelper.IsGpuAvailable();
            chkUseGpu.Enabled = true;
            chkUseGpu.Checked = gpuAvailable;

            if (gpuAvailable)
            {
                Log($"✅ GPU Desteği: {ExecutionProviderHelper.GetAvailableProviders()}");
            }
            else
            {
                Log("⚠️ GPU algılanamadı. Checkbox'ı işaretlerseniz yine de GPU kullanımı denenecek.");
                Log("   Eğer GPU kullanılamıyorsa otomatik olarak CPU'ya düşülecek.");
                Log($"   Sistem Bilgisi: {Environment.OSVersion}");
                Log($"   CUDA_PATH: {Environment.GetEnvironmentVariable("CUDA_PATH") ?? "Ayarlanmamış"}");
            }

            _isInitializing = false;
            LoadDetectors();
        }

        private void UpdateCurrentModelLabel()
        {
            var modelName = Path.GetFileNameWithoutExtension(_currentPlateModelPath);
            var modelTypeText = _currentPlateModelType == PlateDetectionModelType.V2 ? "V2" : "V1";
            lblCurrentModel.Text = $"Model ({modelTypeText}): {modelName}";

            if (!File.Exists(_currentPlateModelPath))
            {
                lblCurrentModel.Text += " (Bulunamadı)";
                lblCurrentModel.ForeColor = Color.Red;
            }
            else
            {
                lblCurrentModel.ForeColor = Color.DarkGreen;
            }
        }

        // SetupGpuAndModels is removed as it's merged into InitializeAppLogic

        private void LoadDetectors()
        {
            try
            {
                ExecutionProviderHelper.Logger = (msg) => Log(msg);
                Log($"🔧 Model dosyaları kontrol ediliyor...");
                Log($"   Çalışma dizini: {Directory.GetCurrentDirectory()}");
                Log($"   Plaka modeli: {_currentPlateModelPath} - Var mı: {File.Exists(_currentPlateModelPath)}");
                Log($"   Karakter modeli: {ModelPathChar} - Var mı: {File.Exists(ModelPathChar)}");

                if (!File.Exists(_currentPlateModelPath))
                {
                    Log($"❌ Plaka modeli bulunamadı: {_currentPlateModelPath}");
                    UpdateCurrentModelLabel();
                    return;
                }

                if (!File.Exists(ModelPathChar))
                {
                    Log($"❌ Karakter S modeli bulunamadı: {ModelPathChar}");
                    return;
                }

                _plateDetectorV1?.Dispose();
                _plateDetectorV2?.Dispose();
                _charDetector?.Dispose();
                _titanV8Detector?.Dispose();
                _parseqDetector?.Dispose();

                bool useGpu = chkUseGpu.Checked;
                _detectorsLoadedWithGpu = useGpu;

                Log($"🔧 Modeller yükleniyor (GPU: {(useGpu ? "Aktif" : "Pasif")})...");

                // 1. Plaka Dedektörü (V1 veya V2)
                try
                {
                    if (_currentPlateModelType == PlateDetectionModelType.V2)
                    {
                        _plateDetectorV2 = new YoloOnnxRunner.PlateRecognitionModel(_currentPlateModelPath, useGpu);
                        Log($"✅ Plaka dedektörü (V2) yüklendi");
                    }
                    else
                    {
                        _plateDetectorV1 = new LicensePlateDetector(_currentPlateModelPath, useGpu);
                        Log($"✅ Plaka dedektörü (V1) yüklendi");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Plaka dedektörü yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        Log($"⚠️ GPU ile yükleme başarısız. CPU ile deneniyor...");
                        try
                        {
                            if (_currentPlateModelType == PlateDetectionModelType.V2)
                            {
                                _plateDetectorV2 = new YoloOnnxRunner.PlateRecognitionModel(_currentPlateModelPath, false);
                                Log($"✅ Plaka dedektörü (V2) CPU ile yüklendi");
                            }
                            else
                            {
                                _plateDetectorV1 = new LicensePlateDetector(_currentPlateModelPath, false);
                                Log($"✅ Plaka dedektörü (V1) CPU ile yüklendi");
                            }
                        }
                        catch (Exception inner)
                        {
                            Log($"❌ Plaka dedektörü CPU ile de yüklenemedi: {inner.Message}");
                        }
                    }
                }

                // 2. Karakter S Dedektörü
                try
                {
                    _charDetector = new PlateCharDetector(ModelPathChar, swapRB: false, useGpu);
                }
                catch (Exception ex)
                {
                    Log($"❌ Karakter S yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        _charDetector = new PlateCharDetector(ModelPathChar, swapRB: false, false);
                    }
                }

                // 3. Titan V8 Dedektörü
                try
                {
                    if (File.Exists(ModelPathTitanV8))
                    {
                        _titanV8Detector = new TitanArmorV8Detector(ModelPathTitanV8, useGpu: useGpu);
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Titan V8 yükleme hatası: {ex.Message}");
                }

                // 4. Parseq Dedektörü
                try
                {
                    if (false && File.Exists(ModelPathParseq))
                    {
                        _parseqDetector = new ParseqDetector(ModelPathParseq, useGpu: useGpu, logCallback: null);
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Parseq yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        try
                        {
                            _parseqDetector = new ParseqDetector(ModelPathParseq, useGpu: false, logCallback: null);
                        }
                        catch (Exception cpuEx)
                        {
                            Log($"❌ Parseq CPU yükleme de başarısız: {cpuEx.Message}");
                        }
                    }
                }

                Log($"✅ Tüm modeller yüklendi (GPU: {(useGpu ? "Aktif" : "Pasif")})");

                btnSelectImage.Enabled = false;
                btnSelectVideo.Enabled = false;
                btnBatchProcess.Enabled = false;

                // Warm-up (Isınma Turu)
                Task.Run(() =>
                {
                    try
                    {
                        Log("🔥 Modeller ısıtılıyor (Warm-up)...");
                        var swWarm = Stopwatch.StartNew();

                        using var dummyBmp = new Bitmap(200, 60, PixelFormat.Format24bppRgb);
                        using var gr = Graphics.FromImage(dummyBmp);
                        gr.Clear(Color.White);
                        using var dummyMat = BitmapConverter.ToMat(dummyBmp);

                        //_plateDetectorV1?.Detect(dummyBmp, 0.5f, false, 0.45f);
                        // Warm-up içinde TitanV8'i 3 kez çalıştır:
                        using var titanDummy = new Mat(60, 200, MatType.CV_8UC3, Scalar.White);
                        _titanV8Detector?.Predict(titanDummy);


                        // PlateDetector da 3 kez:
                        _plateDetectorV2?.Predict(dummyBmp);
                        _charDetector?.RunOnnxPlateRecognition(dummyBmp);


                        swWarm.Stop();
                        Log($"✅ Warm-up tamamlandı ({swWarm.ElapsedMilliseconds}ms).");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Warm-up hatası (önemsiz): {ex.Message}");
                    }
                    finally
                    {
                        // Warm-up bitti, butonları aç
                        this.Invoke(() =>
                        {
                            btnSelectImage.Enabled = true;
                            btnSelectVideo.Enabled = true;
                            btnBatchProcess.Enabled = true;
                        });
                    }
                });

                UpdateCurrentModelLabel();
            }
            catch (Exception ex)
            {
                Log($"❌ Model yükleme hatası: {ex.Message}");
                Log($"   Stack trace: {ex.StackTrace}");
                UpdateCurrentModelLabel();
            }
        }

        private void btnSelectPlateModel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Plaka Tespit Modeli Seçin",
                Filter = "ONNX Model Dosyaları|*.onnx|Tüm Dosyalar|*.*",
                RestoreDirectory = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "models")
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var oldModelPath = _currentPlateModelPath;
                _currentPlateModelPath = dialog.FileName;

                Log($"?? Yeni plaka modeli seçildi: {Path.GetFileName(_currentPlateModelPath)}");

                // Video işleme durumu kontrolü
                if (_isVideoPlaying)
                {
                    var result = MessageBox.Show(
                        "Video işleme devam ediyor. Modeli değiştirmek için video işlemeyi durdurmak gerekiyor. Devam edilsin mi?",
                        "Video İşleme Aktif",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        StopVideoProcessing();
                        LoadDetectors();
                    }
                    else
                    {
                        // İptal edildi, eski modeli geri yükle
                        _currentPlateModelPath = oldModelPath;
                        Log("? Model değişikliği iptal edildi.");
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
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Ensure detectors reflect current GPU setting before running prediction
                if (!IsPlateDetectorReady() || _detectorsLoadedWithGpu != chkUseGpu.Checked)
                {
                    Log("?? GPU ayarı değişikliği algılandı veya dedektörler yok. Modeller yeniden yükleniyor...");
                    LoadDetectors();
                }

                ProcessImage(dialog.FileName);
            }
        }

        private void btnSelectVideo_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Video Seçin",
                Filter = "Video Dosyaları|*.mp4;*.avi;*.mov;*.mkv;*.wmv|Tüm Dosyalar|*.*",
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
            if (_isInitializing) return;

            if (_plateDetectorV1 != null || _plateDetectorV2 != null || _charDetector != null)
            {
                Log("?? GPU ayarı değişti. Modeller yeniden yükleniyor...");
                LoadDetectors();
            }
        }

        private void cmbPlateModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;

            ApplyPlateModelTypeSelection(reloadDetectors: true);
        }

        private void ApplyPlateModelTypeSelection(bool reloadDetectors)
        {
            _currentPlateModelType = cmbPlateModelType.SelectedIndex == 1 ? PlateDetectionModelType.V2 : PlateDetectionModelType.V1;

            var defaultPath = _currentPlateModelType == PlateDetectionModelType.V2 ? DefaultModelPathPlateV2 : DefaultModelPathPlateV1;

            if (string.IsNullOrWhiteSpace(_currentPlateModelPath) || _currentPlateModelPath == DefaultModelPathPlateV1 || _currentPlateModelPath == DefaultModelPathPlateV2)
            {
                _currentPlateModelPath = defaultPath;
            }

            UpdateCurrentModelLabel();

            if (reloadDetectors && _plateDetectorV1 == null && _plateDetectorV2 == null)
            {
                Log($"🔄 Plaka model tipi değiştirildi: {(_currentPlateModelType == PlateDetectionModelType.V2 ? "V2" : "V1")}");
                LoadDetectors();
            }
            else if (reloadDetectors)
            {
                Log($"🔄 Plaka model tipi değiştirildi: {(_currentPlateModelType == PlateDetectionModelType.V2 ? "V2" : "V1")}");
                LoadDetectors();
            }
        }

        // Yeni DetectPlates: V2 için YoloOnnxRunner dönüşünü DetectionResult'a çevirir, V1 için mevcut LicensePlateDetector kullanır
        private DetectionResult DetectPlates(Bitmap bitmap)
        {
            if (_currentPlateModelType == PlateDetectionModelType.V2)
            {
                using var input = Ensure24Bpp(bitmap);
                var detections = _plateDetectorV2?.Predict(input) ?? new List<YoloOnnxRunner.DetectionResult>();

                float confidenceThreshold = (float)nudConfidenceThreshold.Value;
                bool enableNms = chkEnableNMS.Checked;
                float nmsThreshold = (float)nudNMSThreshold.Value;

                var thresholded = detections.Where(d => d.Confidence >= confidenceThreshold).ToList();

                if (thresholded.Count == 0 && detections.Count > 0 && confidenceThreshold > 0.11f)
                {
                    thresholded = detections.Where(d => d.Confidence >= 0.11f).ToList();
                    if (GetDebugModeValue())
                        SafeLog($"🔁 V2 fallback eşiği devreye girdi: {confidenceThreshold:F2} -> 0.11", Color.DarkGoldenrod);
                }

                var plateCandidates = thresholded.Where(d => d.ClassId == 0).ToList();
                var effective = plateCandidates.Count > 0 ? plateCandidates : thresholded;

                var mapped = effective.Select(d => new LicensePlateDetection
                {
                    X = (int)Math.Round(d.Box.X),
                    Y = (int)Math.Round(d.Box.Y),
                    Width = (int)Math.Round(d.Box.Width),
                    Height = (int)Math.Round(d.Box.Height),
                    Confidence = d.Confidence,
                    Class = string.IsNullOrWhiteSpace(d.CountryCode) ? "Licence_Plate" : d.CountryCode,
                    ClassId = d.ClassId,
                    CountryName = d.CountryName
                }).Where(d => d.Width > 0 && d.Height > 0).ToList();

                if (enableNms && mapped.Count > 1)
                {
                    mapped = ApplyNmsForMappedDetections(mapped, nmsThreshold);
                }

                return new DetectionResult(mapped, 0);
            }

            return _plateDetectorV1!.Detect(bitmap, (float)nudConfidenceThreshold.Value, chkEnableNMS.Checked, (float)nudNMSThreshold.Value);
        }

        private static Bitmap Ensure24Bpp(Bitmap source)
        {
            if (source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                return new Bitmap(source);

            var converted = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(converted);
            g.DrawImage(source, 0, 0, source.Width, source.Height);
            return converted;
        }

        private static List<LicensePlateDetection> ApplyNmsForMappedDetections(List<LicensePlateDetection> detections, float iouThreshold)
        {
            var ordered = detections.OrderByDescending(d => d.Confidence).ToList();
            var kept = new List<LicensePlateDetection>(ordered.Count);

            while (ordered.Count > 0)
            {
                var current = ordered[0];
                kept.Add(current);
                ordered.RemoveAt(0);

                for (int i = ordered.Count - 1; i >= 0; i--)
                {
                    if (CalculateIoU(current, ordered[i]) > iouThreshold)
                    {
                        ordered.RemoveAt(i);
                    }
                }
            }

            return kept;
        }

        private static float CalculateIoU(LicensePlateDetection a, LicensePlateDetection b)
        {
            int x1 = Math.Max(a.X, b.X);
            int y1 = Math.Max(a.Y, b.Y);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            int intersectionWidth = Math.Max(0, x2 - x1);
            int intersectionHeight = Math.Max(0, y2 - y1);
            int intersection = intersectionWidth * intersectionHeight;

            int areaA = a.Width * a.Height;
            int areaB = b.Width * b.Height;
            int union = areaA + areaB - intersection;

            return union > 0 ? (float)intersection / union : 0f;
        }

        private void btnImageLabeling_Click(object sender, EventArgs e)
        {
            try
            {
                var frmLabeling = new ImageLabeling();
                frmLabeling.Show();
                Log("🔧 Resim Etiketleme ekranı açıldı.");
            }
            catch (Exception ex)
            {
                Log($"❌ Resim Etiketleme ekranı açılırken hata: {ex.Message}");
            }
        }

        private void btnBatchProcess_Click(object sender, EventArgs e)
        {
            // If detectors were loaded with a different GPU setting, reload now so batch uses current choice
            if (!IsPlateDetectorReady() || _detectorsLoadedWithGpu != chkUseGpu.Checked)
            {
                Log("?? GPU ayarı değişikliği algılandı veya dedektörler yok. Modeller yeniden yükleniyor...");
                LoadDetectors();
            }

            if (!AreDetectorsReady())
            {
                Log("?? Detektorlar hazır değil! Model dosyalarını kontrol edin.");
                MessageBox.Show("Model dosyaları yüklenmemiş! Önce plaka modelini yükleyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Plaka tespiti yapılacak resimlerin bulunduğu klasörü seçin",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                ProcessBatchImages(folderDialog.SelectedPath, true);
            }
        }

        private void ProcessBatchImages(string folderPath, bool doOcr = false)
        {
            try
            {
                Log($"?? Toplu işleme başlıyor: {folderPath}");

                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
                var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();

                if (imageFiles.Length == 0)
                {
                    Log("?? Klasörde desteklenen resim dosyası bulunamadı!");
                    MessageBox.Show("Seçilen klasörde resim dosyası bulunamadı!\nDesteklenen formatlar: JPG, JPEG, PNG, BMP, TIFF", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Log($"?? {imageFiles.Length} resim dosyası bulundu.");

                // Progress için değişkenler
                int processedCount = 0;
                int totalPlatesFound = 0;
                int v8CorrectCount = 0;
                int cctCorrectCount = 0;
                var stopwatch = Stopwatch.StartNew();

                // Her resim için işlem yap
                foreach (var imagePath in imageFiles)
                {
                    try
                    {
                        processedCount++;
                        var fileName = Path.GetFileName(imagePath);

                        Log($"?? [{processedCount}/{imageFiles.Length}] İşleniyor: {fileName}");

                        using var bitmap = new Bitmap(imagePath);
                        var plateResult = DetectPlates(bitmap);

                        if (plateResult.Detections.Count == 0)
                        {
                            Log($"  ?? Plaka bulunamadı: {fileName}");
                            continue;
                        }

                        var detectedPlates = new List<string>();
                        foreach (var plate in plateResult.Detections)
                        {
                            totalPlatesFound++;

                            // Plaka resmini kaydet
                            var (v8C, cctC) = SavePlateImageBatch(bitmap, plate, fileName, totalPlatesFound, doOcr);
                            if (v8C) v8CorrectCount++;
                            if (cctC) cctCorrectCount++;

                            detectedPlates.Add($"{plate.Confidence:P1}");
                        }

                        Log($"  ? {plateResult.Detections.Count} plaka bulundu: {string.Join(", ", detectedPlates)}");

                        // Son işlenen resmi göster (isteğe bağlı)
                        if (processedCount == imageFiles.Length)
                        {
                            using var resultBitmap = DrawPlatesOnImage(bitmap, plateResult.Detections);
                            pictureBoxImage.Image?.Dispose();
                            pictureBoxImage.Image = new Bitmap(resultBitmap);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"? {Path.GetFileName(imagePath)} işlenirken hata: {ex.Message}");
                    }
                }

                stopwatch.Stop();

                Log($"?? Toplu işleme tamamlandı!");
                Log($"?? Özet: {processedCount} resim işlendi, {totalPlatesFound} plaka bulundu");
                if (doOcr)
                {
                    Log($"?? Doğruluk Özeti:");
                    Log($"   - Titan V8: {v8CorrectCount} / {totalPlatesFound} doğru bilindi.");
                    if (chkMultiModel.Checked)
                    {
                        Log($"   - Model S (CCT): {cctCorrectCount} / {totalPlatesFound} doğru bilindi.");
                    }
                }
                Log($"?? Toplam süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye");
                Log($"?? Ortalama hız: {(processedCount / stopwatch.Elapsed.TotalSeconds):F2} resim/saniye");

                string reportMsg = $"Toplu işleme tamamlandı!\n\n" +
                                   $"İşlenen resim: {processedCount}\n" +
                                   $"Bulunan plaka: {totalPlatesFound}\n" +
                                   $"Süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye\n\n";

                if (doOcr)
                {
                    reportMsg += $"Titan V8 Doğru: {v8CorrectCount}\n";
                    if (chkMultiModel.Checked) reportMsg += $"Model S Doğru: {cctCorrectCount}\n\n";
                }

                reportMsg += $"Plaka resimleri '{_outputFolder}' klasörüne kaydedildi.";

                MessageBox.Show(
                    reportMsg,
                    "Toplu İşleme Tamamlandı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"? Toplu işleme hatası: {ex.Message}");
                MessageBox.Show($"Toplu işleme sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private (bool v8Correct, bool cctCorrect) SavePlateImageBatch(Bitmap originalBitmap, LicensePlateDetection plate, string originalFileName, int plateIndex, bool doOcr)
        {
            bool v8ResultCorrect = false;
            bool cctResultCorrect = false;

            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return (false, false);

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);

                string filename;
                string targetFolder = _outputFolder;

                if (doOcr)
                {
                    if (chkMultiModel.Checked)
                    {
                        var plateTextV8 = ProcessPlateCharactersTitanV8Only(originalBitmap, plate);
                        string plateTextCCT = string.Empty;
                        if (_charDetector != null)
                        {
                            plateTextCCT = _charDetector.RunOnnxPlateRecognition(plateBitmap).Detection ?? string.Empty;
                        }

                        string baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
                        v8ResultCorrect = !string.IsNullOrWhiteSpace(plateTextV8) && baseFileName.Contains(plateTextV8, StringComparison.OrdinalIgnoreCase);
                        cctResultCorrect = !string.IsNullOrWhiteSpace(plateTextCCT) && baseFileName.Contains(plateTextCCT, StringComparison.OrdinalIgnoreCase);

                        if (string.Equals(plateTextV8, plateTextCCT, StringComparison.OrdinalIgnoreCase))
                        {
                            var plateText = plateTextV8;
                            if (string.IsNullOrWhiteSpace(plateText))
                            {
                                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                                filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                            }
                            else
                            {
                                var safePlateText = string.Concat(plateText.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                                filename = $"{safePlateText}.jpg";
                            }
                        }
                        else
                        {
                            if (v8ResultCorrect || cctResultCorrect)
                            {
                                string correctText = v8ResultCorrect ? plateTextV8 : plateTextCCT;
                                string safePlateText = string.Concat(correctText.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                                filename = $"{safePlateText}.jpg";
                                Log($"✅ MultiModel Farklı Ama Dosya Adıyla Eşleşti: '{correctText}' Doğru Kabul Edildi.", Color.DarkGreen);
                            }
                            else
                            {
                                targetFolder = Path.Combine(_outputFolder, "NotSure");
                                if (!Directory.Exists(targetFolder))
                                {
                                    Directory.CreateDirectory(targetFolder);
                                }

                                string safeV8 = string.IsNullOrWhiteSpace(plateTextV8) ? "BOS" : string.Concat(plateTextV8.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                                string safeCCT = string.IsNullOrWhiteSpace(plateTextCCT) ? "BOS" : string.Concat(plateTextCCT.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));

                                filename = $"{safeV8}_V8_{safeCCT}_CCT.jpg";
                                Log($"⚠️ MultiModel Farklı: V8='{plateTextV8}', S(CCT)='{plateTextCCT}' - NotSure klasörüne kaydediliyor.", Color.DarkGoldenrod);
                            }
                        }
                    }
                    else
                    {
                        // Sadece Titan V8
                        var plateText = ProcessPlateCharactersTitanV8Only(originalBitmap, plate);
                        string baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
                        v8ResultCorrect = !string.IsNullOrWhiteSpace(plateText) && baseFileName.Contains(plateText, StringComparison.OrdinalIgnoreCase);

                        if (string.IsNullOrWhiteSpace(plateText))
                        {
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                            filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                        }
                        else
                        {
                            var safePlateText = string.Concat(plateText.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                            filename = $"{safePlateText}.jpg";
                        }
                    }
                }
                else
                {
                    // Eski yöntem
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                    filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                }

                var fullPath = Path.Combine(targetFolder, filename);

                // Çakışma kontrolü ve yönetimi
                if (File.Exists(fullPath))
                {
                    //bool overwrite = false;
                    //try
                    //{
                    //    // Dosya boyutlarını karşılaştır (basit içerik kontrolü varsayımı)
                    //    // Not: Bitmap kaydetmeden önce boyutunu tam bilemeyiz, bu yüzden mevcut dosyayla karşılaştırmak zor.
                    //    // Ancak kullanıcı kuralı: "Aynı boyuttaysa üzerine yazsın. Boyutları farklı ise indexlesin"
                    //    // Burada mantıksal bir sorun var: Henüz kaydetmediğimiz resmin boyutunu (byte olarak) bilmiyoruz.
                    //    // Kaydedip sonra kontrol etmek gerekebilir veya geçici bir dosyaya kaydedip karşılaştırabiliriz.
                    //    // Veya sadece isim çakışmasına odaklanıp, eğer dosya varsa indexleyelim (farklı resimse).
                    //    // Fakat kullanıcı "aynı boyuttaysa üzerine yazsın" dedi. 

                    //    // Strateji: Geçici belleğe kaydet, boyutunu al.
                    //    using (var ms = new MemoryStream())
                    //    {
                    //        plateBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //        long newSize = ms.Length;

                    //        var existingInfo = new FileInfo(fullPath);
                    //        if (existingInfo.Length == newSize)
                    //        {
                    //            overwrite = true;
                    //        }
                    //    }
                    //}
                    //catch
                    //{
                    //    // Hata durumunda güvenli yol: indexle
                    //    overwrite = false;
                    //}

                    //if (!overwrite)
                    //{
                    // İndeksle: xxxxxxx_02.jpg
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                    string ext = Path.GetExtension(filename);
                    int counter = 2;

                    do
                    {
                        var newName = $"{nameWithoutExt}_{counter:D2}{ext}";
                        fullPath = Path.Combine(targetFolder, newName);

                        // Yeni isim de var mı? Varsa ve boyutu farklıysa counter artır, boyutu aynıysa üzerine yaz
                        if (File.Exists(fullPath))
                        {
                            // Tekrar boyut kontrolü
                            try
                            {
                                using (var ms = new MemoryStream())
                                {
                                    plateBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); // Tekrar save maliyetli ama güvenli
                                    long newSize = ms.Length;
                                    if (new FileInfo(fullPath).Length == newSize)
                                    {
                                        // Aynı boyut, bu dosyanın üzerine yaz
                                        break;
                                    }
                                }
                            }
                            catch { }
                            counter++;
                        }
                        else
                        {
                            // Dosya yok, buraya yaz
                            break;
                        }

                    } while (true);
                    //}
                }

                plateBitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log($"    ?? Plaka kaydedildi: {Path.GetFileName(fullPath)}");
            }
            catch (Exception ex)
            {
                Log($"?? Plaka kaydetme hatası: {ex.Message}");
            }

            return (v8ResultCorrect, cctResultCorrect);
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
                Log("?? Detektorlar hazır değil! Model dosyalarını kontrol edin.");

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
                    Log($"? Resim yükleme hatası: {ex.Message}");
                }
                return;
            }

            try
            {
                Log($"?? Resim işleniyor: {Path.GetFileName(imagePath)}");

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
                Log($"? Resim işleme hatası: {ex.Message}");
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
                    Log("? Video açılamadı!");
                    return;
                } 

                _isVideoPlaying = true;
                ResetFrameCounter();
                SetVideoButtons(playing: true);

                Log($"?? Video işleme başladı: {Path.GetFileName(_selectedVideoPath)}");

                _videoThread = new Thread(ProcessVideoFrames)
                {
                    IsBackground = true,
                    Name = "VideoProcessing"
                };
                _videoThread.Start();
            }
            catch (Exception ex)
            {
                Log($"? Video başlatma hatası: {ex.Message}");
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
                SafeLog($"?? Video durdurma hatası: {ex.Message}");
            }

            SetVideoButtons(playing: false);
            SafeLog("?? Video işleme durduruldu");
            SafeUpdateFpsDisplay(0);
        }

        private void ProcessVideoFrames()
        {
            int frameSkip = 0;

            double videoFps = _capture?.Fps ?? 30.0;
            if (videoFps <= 0 || videoFps > 120) videoFps = 30.0;
            int frameDelay = (int)(1000.0 / videoFps);

            try
            {
                using var frame = new Mat();

                while (_isVideoPlaying && _capture != null && !_isDisposed)
                {
                    _pauseEvent.Wait(); // pause'da burada bekler, resume'da devam eder

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
                    var result = ProcessFrame(bitmap);

                    if (result != null)
                    {
                        SafeUpdateImage(result);
                    }

                    UpdateFrameCounter();

                    var elapsed = (int)(DateTime.Now - frameStart).TotalMilliseconds;
                    int effectiveDelay = frameDelay * (skipValue + 1);
                    var sleepTime = Math.Max(1, effectiveDelay - elapsed);
                    Thread.Sleep(sleepTime);
                }
            }
            catch (Exception ex)
            {
                SafeLog($"❌ Video işleme hatası: {ex.Message}");
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

                // Doğrudan OCR modu kontrolü
                if (GetDirectOcrValue())
                {
                    var fullImagePlate = new LicensePlateDetection
                    {
                        X = 0,
                        Y = 0,
                        Width = originalBitmap.Width,
                        Height = originalBitmap.Height,
                        Confidence = 1.0f
                    };

                    var plateText = ProcessPlateCharacters(originalBitmap, fullImagePlate);
                    sw.Stop();

                    var directResult = new Bitmap(originalBitmap);
                    using var gDirect = Graphics.FromImage(directResult);
                    DrawPlateDetection(gDirect, fullImagePlate, plateText);

                    SafeLog($"?? Doğrudan OCR yapıldı: '{plateText}' ({sw.ElapsedMilliseconds}ms)");
                    return directResult;
                }

                var plateResult = DetectPlates(originalBitmap);

                sw.Stop();

                if (plateResult.Detections.Count == 0)
                {
                    SafeLog($"?? Plaka bulunamadı ({sw.ElapsedMilliseconds}ms)");
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
                        detectedPlates.Add($"[Okunamadı] ({plate.Confidence:P1})");
                    }

                    if (saveImages && !string.IsNullOrEmpty(plateText))
                    {
                        SavePlateImage(originalBitmap, plate, plateText);
                    }
                }

                // Tespit edilen plakalar listesiyle log mesajı
                var platesList = string.Join(", ", detectedPlates);
                SafeLog($"? {plateResult.Detections.Count} plaka tespit edildi: {platesList} ({sw.ElapsedMilliseconds}ms)");

                return resultBitmap;
            }
            catch (Exception ex)
            {
                SafeLog($"? Frame işleme hatası: {ex.Message}");
                return new Bitmap(originalBitmap);
            }
        }

        private int GetOcrModelSelection()
        {
            if (InvokeRequired)
            {
                return (int)Invoke(new Func<int>(GetOcrModelSelection));
            }
            return cbOcrModel.SelectedIndex >= 0 ? cbOcrModel.SelectedIndex : 0;
        }

        private string ProcessPlateCharacters(Bitmap originalBitmap, LicensePlateDetection plate)
        {
            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return string.Empty;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);
                using var plateMat = BitmapConverter.ToMat(plateBitmap);

                int selectedOcrModel = GetOcrModelSelection();
                var swModel = new Stopwatch();

                // Video modunda: Sadece seçilen model çalışsın (Maksimum FPS için)
                if (_isVideoPlaying)
                {
                    if (selectedOcrModel == 2 && _parseqDetector != null)
                    {
                        swModel.Restart();
                        var parseqRes = _parseqDetector.RunParseqOcr(plateBitmap);
                        swModel.Stop();
                        SafeLog($"?? OCR (Parseq): '{parseqRes.Text}' ({swModel.ElapsedMilliseconds}ms)");
                        return parseqRes.Text;
                    }
                    else if (selectedOcrModel == 1 && _titanV8Detector != null)
                    {
                        swModel.Restart();
                        var v8Res = _titanV8Detector.Predict(plateMat);
                        swModel.Stop();
                        string v8Text = v8Res?.Text ?? string.Empty;
                        SafeLog($"?? OCR (Titan V8): '{v8Text}' ({swModel.ElapsedMilliseconds}ms)");
                        return v8Text;
                    }
                    else
                    {
                        swModel.Restart();
                        var sRes = _charDetector?.RunOnnxPlateRecognition(plateBitmap).Detection ?? string.Empty;
                        swModel.Stop();
                        SafeLog($"?? OCR (Model S): '{sRes}' ({swModel.ElapsedMilliseconds}ms)");
                        return sRes;
                    }
                }

                // Tekil resim modunda: Karşılaştırma için tüm modeller çalıştırılır ve detaylı loglanır
                swModel.Restart();
                var sResult = _charDetector?.RunOnnxPlateRecognition(plateBitmap).Detection ?? string.Empty;
                swModel.Stop();
                long sTime = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var v8Result = _titanV8Detector?.Predict(plateMat);
                swModel.Stop();
                long v8Time = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var parseqResult = _parseqDetector?.RunParseqOcr(plateBitmap);
                swModel.Stop();
                long parseqTime = swModel.ElapsedMilliseconds;

                SafeLog($"?? OCR SONUÇLARI:");
                SafeLog($"   - Model S".PadRight(30) + $": '{(string.IsNullOrEmpty(sResult) ? "[Boş]" : sResult)}' ({sTime}ms)");

                if (v8Result != null)
                {
                    LogV8Result(v8Result, v8Time);
                }
                else
                {
                    SafeLog($"   - Titan v8".PadRight(30) + $": [Yüklenmedi]", Color.Gray);
                }

                //if (parseqResult != null)
                //{
                //    LogParseqResult(parseqResult, parseqTime);
                //}
                //else
                //{
                //    SafeLog($"   - Parseq".PadRight(30) + $": [Yüklenmedi]", Color.Gray);
                //}

                // Öncelik Sırası: Parseq > Titan V8 > Model S
                if (parseqResult != null && !string.IsNullOrEmpty(parseqResult.Text)) return parseqResult.Text;
                if (v8Result != null && !string.IsNullOrEmpty(v8Result.Text)) return v8Result.Text;
                return sResult;
            }
            catch (Exception ex)
            {
                SafeLog($"?? Karakter işleme hatası: {ex.Message}");
                return string.Empty;
            }
        }

        // Batch modu için: SADECE Titan V8 ile OCR çalıştırır. Diğer modeller devreye girmez.
        private string ProcessPlateCharactersTitanV8Only(Bitmap originalBitmap, LicensePlateDetection plate)
        {
            if (_titanV8Detector == null)
            {
                SafeLog("?? Batch OCR: Titan V8 yüklü değil, OCR atlandı.");
                return string.Empty;
            }

            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return string.Empty;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);
                using var plateMat = BitmapConverter.ToMat(plateBitmap);

                var sw = Stopwatch.StartNew();
                var v8Result = _titanV8Detector.Predict(plateMat);
                sw.Stop();

                string resultText = v8Result?.Text ?? string.Empty;

                SafeLog($"?? Batch OCR (Titan V8): '{resultText}' ({sw.ElapsedMilliseconds}ms)");
                return resultText;
            }
            catch (Exception ex)
            {
                SafeLog($"?? Batch OCR hatası: {ex.Message}");
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
                SafeLog($"?? Plaka kaydetme hatası: {ex.Message}");
            }
        }

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
            cmbPlateModelType.Enabled = !playing; // Model değiştirme video sırasında devre dışı
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

        private bool GetDirectOcrValue()
        {
            if (InvokeRequired)
            {
                return Invoke(() => chkDirectOcr.Checked);
            }
            return chkDirectOcr.Checked;
        }

        private void SafeUpdateImage(Bitmap result)
        {
            if (_isDisposed) return;

            if (pictureBoxImage.InvokeRequired)
            {
                pictureBoxImage.BeginInvoke(new Action(() => SafeUpdateImage(result)));
                return;
            }

            try
            {
                if (result != null && result.Width > 0 && result.Height > 0)
                {
                    pictureBoxImage.Image?.Dispose();
                    pictureBoxImage.Image = new Bitmap(result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PictureBox hata: {ex.Message}");
            }
        }

        private void SafeUpdateFpsDisplay(double fps)
        {
            if (_isDisposed) return;

            if (lblFps.InvokeRequired)
            {
                lblFps.BeginInvoke(new Action(() => SafeUpdateFpsDisplay(fps)));
                return;
            }

            lblFps.Text = $"FPS: {fps:F2}";
        }

        private void SafeLog(string message, Color? color = null)
        {
            Log(message, color);
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

        private void LogParseqResult(ParseqOcrResult result, long durationMs)
        {
            if (_isDisposed) return;

            if (!txtLog.IsHandleCreated)
            {
                Debug.WriteLine($"[HANDLE NOT CREATED] Parseq OCR: {result.Text}");
                return;
            }

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => LogParseqResult(result, durationMs)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = Color.DarkOrchid;
            txtLog.AppendText($"[{timestamp}] " + "   - Parseq".PadRight(30) + ": '");

            foreach (var detail in result.Details)
            {
                int colorVal = (int)(200 * (1.0f - detail.Confidence));
                txtLog.SelectionColor = Color.FromArgb(colorVal, colorVal, colorVal);
                txtLog.AppendText(detail.Character.ToString());
            }

            txtLog.SelectionColor = Color.DarkOrchid;
            txtLog.AppendText($"' (Acc: {result.AverageConfidence:P1}) ({durationMs}ms){Environment.NewLine}");

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void LogV8Result(TitanV8ModelResult result, long durationMs)
        {
            if (_isDisposed) return;

            if (!txtLog.IsHandleCreated)
            {
                Debug.WriteLine($"[HANDLE NOT CREATED] Titan V8 OCR: {result.Text}");
                return;
            }

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => LogV8Result(result, durationMs)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Başlık kısmı
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = Color.Teal; // Farklı bir renk seçildi
            txtLog.AppendText($"[{timestamp}] " + "   - Titan v8".PadRight(30) + ": '");

            // Karakter karakter renklendirme
            foreach (var detail in result.Details)
            {
                int colorVal = (int)(200 * (1.0f - detail.Confidence));
                txtLog.SelectionColor = Color.FromArgb(colorVal, colorVal, colorVal);
                txtLog.AppendText(detail.Character.ToString());
            }

            txtLog.SelectionColor = Color.Teal;
            float avgAcc = result.Details.Any() ? result.Details.Average(d => d.Confidence) : 0f;
            txtLog.AppendText($"' ({result.ModelHead}, Acc: {avgAcc:P1}, Secure: {result.IsSecure}) ({durationMs}ms){Environment.NewLine}");

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void Log(string message, Color? color = null)
        {
            if (_isDisposed) return;

            if (!txtLog.IsHandleCreated)
            {
                Debug.WriteLine($"[HANDLE NOT CREATED] {message}");
                return;
            }

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => Log(message, color)));
                return;
            }

            // Açık tema için renkleri uyarla
            if (color == null)
            {
                if (message.Contains("❌")) color = Color.DarkRed;
                else if (message.Contains("✅") || message.Contains("??")) color = Color.DarkGreen;
                else if (message.Contains("⚠️")) color = Color.DarkGoldenrod;
                else color = Color.Black;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}{Environment.NewLine}";

            // Satır sayısı kontrolü (RichTextBox için performans açısından önemli)
            if (txtLog.Lines.Length > MaxLogLines)
            {
                // En eski satırları sil
                txtLog.Select(0, txtLog.GetFirstCharIndexFromLine(LogTrimLines));
                txtLog.ReadOnly = false;
                txtLog.SelectedText = "";
                txtLog.ReadOnly = true;
            }

            // Renkli metin ekleme
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = (Color)color;
            txtLog.AppendText(logMessage);
            txtLog.SelectionColor = txtLog.ForeColor; // Rengi sıfırla

            txtLog.SelectionStart = txtLog.TextLength;
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
                _plateDetectorV1?.Dispose();
                _plateDetectorV2?.Dispose();
                _charDetector?.Dispose();
                _titanV8Detector?.Dispose();
                _parseqDetector?.Dispose();
                _capture?.Dispose();
                pictureBoxImage.Image?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (!_isVideoPlaying) return;

            if (_isVideoPaused)
            {
                // Resume
                _isVideoPaused = false;
                _pauseEvent.Set();
                btnPause.Text = "⏸ Duraklat";
                SafeLog("▶️ Video devam ediyor");
            }
            else
            {
                // Pause
                _isVideoPaused = true;
                _pauseEvent.Reset();
                btnPause.Text = "▶️ Devam Et";
                SafeLog("⏸ Video duraklatıldı");
            }
        }
    }
}