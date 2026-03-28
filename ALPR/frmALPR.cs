using ALPR.Detection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace ALPR
{
    public partial class frmALPR : Form
    {
        private const string DefaultModelPathPlate = "models/LicencePlateDetection_Gpu.onnx";
        private const string ModelPathChar = "models/cct_s_v1_global.onnx"; // Varsayılan S
        private const string ModelPathCharXS = "models/cct_xs_v1_global_model.onnx"; // 2. sıra XS
        private const string ModelPathTitan = "models/titan_armor_v3.onnx";
        private const string ModelPathSentinel = "models/titan_armor_v6.onnx";
        private const string ModelPathAbsolute = "models/titan_armor_v6_3_absolute.onnx";
        private const string ModelPathTitanV8 = "models/titan_armor_v8.onnx";
        private const int MaxLogLines = 100;
        private const int LogTrimLines = 50;

        private string _currentPlateModelPath = DefaultModelPathPlate;
        private LicensePlateDetector? _plateDetector;
        private PlateCharDetector? _charDetector; // Varsayılan (S)
        private PlateCharDetector? _charDetectorXS;
        private TitanArmorDetector? _titanArmorDetector;
        private TitanArmorSentinel? _sentinelDetector;
        private TitanArmorV6Absolute? _absoluteDetector;
        private TitanArmorV8Detector? _titanV8Detector;
        private VideoCapture? _capture;
        private bool _isVideoPlaying;
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
            InitializeAppLogic();
        }

        private void InitializeAppLogic()
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
                lblCurrentModel.Text += " (Bulunamadı)";
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

            // Checkbox'ı HER ZAMAN aktif bırak - kullanıcı deneme hakkına sahip olsun
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

            LoadDetectors();
        }

        private void LoadDetectors()
        {
            try
            {
                Log($"🔧 Model dosyaları kontrol ediliyor...");
                Log($"   Çalışma dizini: {Directory.GetCurrentDirectory()}");
                Log($"   Plaka modeli: {_currentPlateModelPath} - Var mı: {File.Exists(_currentPlateModelPath)}");
                Log($"   Karakter modeli: {ModelPathChar} - Var mı: {File.Exists(ModelPathChar)}");

                if (!File.Exists(_currentPlateModelPath))
                {
                    Log($"❌ Plaka modeli bulunamadı: {_currentPlateModelPath}");
                    Log($"💡 Model dosyalarını seçin veya '{Path.Combine(Directory.GetCurrentDirectory(), "models")}' klasörüne koyun");
                    UpdateCurrentModelLabel();
                    return;
                }

                if (!File.Exists(ModelPathChar))
                {
                    Log($"❌ Karakter modeli bulunamadı: {ModelPathChar}");
                    Log($"💡 Model dosyalarını '{Path.Combine(Directory.GetCurrentDirectory(), "models")}' klasörüne koyun");
                    return;
                }

                _plateDetector?.Dispose();
                _charDetector?.Dispose();
                _charDetectorXS?.Dispose();
                _titanArmorDetector?.Dispose();
                _sentinelDetector?.Dispose();
                _absoluteDetector?.Dispose();
                _titanV8Detector?.Dispose();
                

                bool useGpu = chkUseGpu.Checked;
                _detectorsLoadedWithGpu = useGpu;

                Log($"🎯 GPU kullanımı: {(useGpu ? "İSTENİYOR" : "İSTENMİYOR")}");

                try
                {
                    _plateDetector = new LicensePlateDetector(_currentPlateModelPath, useGpu);
                    Log($"✅ Plaka dedektörü yüklendi");
                }
                catch (Exception ex)
                {
                    Log($"❌ Plaka dedektörü yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        Log($"⚠️ GPU ile yükleme başarısız. CPU ile deneniyor...");
                        _plateDetector = new LicensePlateDetector(_currentPlateModelPath, false);
                        Log($"✅ Plaka dedektörü CPU ile yüklendi");
                    }
                    else
                    {
                        throw;
                    }
                }

                try
                {
                    _charDetector = new PlateCharDetector(ModelPathChar, swapRB: false, useGpu);
                    Log($"✅ Karakter modeli (S - Varsayılan) yüklendi");
                }
                catch (Exception ex)
                {
                    Log($"❌ Karakter modeli (S) yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        Log($"⚠️ GPU ile Karakter (S) yükleme başarısız. CPU ile deneniyor...");
                        _charDetector = new PlateCharDetector(ModelPathChar, swapRB: false, false);
                        Log($"✅ Karakter modeli (S - Varsayılan) CPU ile yüklendi");
                    }
                }

                try
                {
                    _charDetectorXS = new PlateCharDetector(ModelPathCharXS, swapRB: false, useGpu);
                    Log($"✅ Karakter modeli (XS) yüklendi");
                }
                catch (Exception ex)
                {
                    Log($"❌ Karakter modeli (XS) yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        Log($"⚠️ GPU ile Karakter (XS) yükleme başarısız. CPU ile deneniyor...");
                        _charDetectorXS = new PlateCharDetector(ModelPathCharXS, swapRB: false, false);
                        Log($"✅ Karakter modeli (XS) CPU ile yüklendi");
                    }
                }

                try
                {
                    if (File.Exists(ModelPathTitan))
                    {
                        _titanArmorDetector = new TitanArmorDetector(ModelPathTitan, useGpu);
                        Log($"✅ Titan Armor dedektörü yüklendi");
                    }
                    else
                    {
                        Log($"⚠️ Titan Armor modeli bulunamadı: {ModelPathTitan}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Titan Armor dedektörü yükleme hatası: {ex.Message}");
                    if (useGpu)
                    {
                        Log($"⚠️ GPU ile Titan Armor yükleme başarısız. CPU ile deneniyor...");
                        _titanArmorDetector = new TitanArmorDetector(ModelPathTitan, false);
                        Log($"✅ Titan Armor dedektörü CPU ile yüklendi");
                    }
                }

                try
                {
                    Log($"🔍 Sentinel v6 kontrol ediliyor: {Path.GetFullPath(ModelPathSentinel)}");
                    if (File.Exists(ModelPathSentinel))
                    {
                        _sentinelDetector = new TitanArmorSentinel(ModelPathSentinel, useGpu);
                        Log($"✅ Sentinel v6 dedektörü yüklendi");
                    }
                    else
                    {
                        Log($"⚠️ Sentinel v6 dosyası BULUNAMADI: {ModelPathSentinel}");
                        Log($"   Aranan tam yol: {Path.GetFullPath(ModelPathSentinel)}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Sentinel v6 yükleme hatası: {ex.Message}");
                }

                try
                {
                    Log($"🔍 Sentinel Absolute kontrol ediliyor: {Path.GetFullPath(ModelPathAbsolute)}");
                    if (File.Exists(ModelPathAbsolute))
                    {
                        _absoluteDetector = new TitanArmorV6Absolute(ModelPathAbsolute, useGpu);
                        Log($"✅ Sentinel Absolute dedektörü yüklendi");
                    }
                    else
                    {
                        Log($"⚠️ Sentinel Absolute dosyası BULUNAMADI: {ModelPathAbsolute}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Sentinel Absolute yükleme hatası: {ex.Message}");
                }

                try
                {
                    Log($"🔍 Titan V8 kontrol ediliyor: {Path.GetFullPath(ModelPathTitanV8)}");
                    if (File.Exists(ModelPathTitanV8))
                    {
                        _titanV8Detector = new TitanArmorV8Detector(ModelPathTitanV8, useGpu);
                        Log($"✅ Titan V8 dedektörü yüklendi");
                    }
                    else
                    {
                        Log($"⚠️ Titan V8 dosyası BULUNAMADI: {ModelPathTitanV8}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Titan V8 yükleme hatası: {ex.Message}");
                }


                Log($"✅ Modeller yüklendi. GPU: {(useGpu ? "İSTENDİ" : "Pasif")}");
                Log($"💡 Aktif plaka modeli: {Path.GetFileName(_currentPlateModelPath)}");

                // Warm-up (Isınma Turu)
                Task.Run(() =>
                {
                    try
                    {
                        Log("🔥 Modeller ısıtılıyor (Warm-up)...");
                        var swWarm = Stopwatch.StartNew();
                        
                        // Boş bir bitmap ile ısıtma
                        using var dummyBmp = new Bitmap(200, 60);
                        using var gr = Graphics.FromImage(dummyBmp);
                        gr.Clear(Color.White);
                        using var dummyMat = BitmapConverter.ToMat(dummyBmp);

                        _charDetector?.RunOnnxPlateRecognition(dummyBmp);
                        _charDetectorXS?.RunOnnxPlateRecognition(dummyBmp);
                        _titanArmorDetector?.Predict(dummyMat);
                        _sentinelDetector?.Predict(dummyMat);
                        _absoluteDetector?.PredictDetailed(dummyMat);
                        _titanV8Detector?.Predict(dummyMat);

                        swWarm.Stop();
                        Log($"✅ Warm-up tamamlandı ({swWarm.ElapsedMilliseconds}ms). İlk işlem artık hızlı olacak.");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Warm-up sırasında hata (önemsiz): {ex.Message}");
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
                if (_plateDetector == null || _detectorsLoadedWithGpu != chkUseGpu.Checked)
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
            if (_plateDetector != null || _charDetector != null)
            {
                Log("?? GPU ayarı değişti. Modeller yeniden yükleniyor...");
                LoadDetectors();
            }
        }

        private void btnModelComparison_Click(object sender, EventArgs e)
        {
            try
            {
                var modelComparisonForm = new frmModelComparison();
                modelComparisonForm.Show();
                Log("?? Model karşılaştırma ekranı açıldı.");
            }
            catch (Exception ex)
            {
                Log($"? Model karşılaştırma ekranı açılırken hata: {ex.Message}");
                MessageBox.Show($"Model karşılaştırma ekranı açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // PaddleOCR butonu
        private void btnPaddleOCR_Click(object sender, EventArgs e)
        {
            try
            {
                var paddleOCRForm = new frmPaddleOCR();
                paddleOCRForm.Show();
                Log("?? PaddleOCR platformu açıldı.");
            }
            catch (Exception ex)
            {
                Log($"? PaddleOCR platformu açılırken hata: {ex.Message}");
                MessageBox.Show($"PaddleOCR platformu açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TesseractOCR butonu
        private void btnTesseractOCR_Click(object sender, EventArgs e)
        {
            try
            {
                var ImageToTextForm = new ImageLabeling();
                ImageToTextForm.Show();
                //var tesseractOCRForm = new frmTesseractOCR();
                //tesseractOCRForm.Show();
                //Log("?? Tesseract OCR platformu açıldı.");
            }
            catch (Exception ex)
            {
                Log($"? Tesseract OCR platformu açılırken hata: {ex.Message}");
                MessageBox.Show($"Tesseract OCR platformu açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatchProcess_Click(object sender, EventArgs e)
        {
            // If detectors were loaded with a different GPU setting, reload now so batch uses current choice
            if (_plateDetector == null || _detectorsLoadedWithGpu != chkUseGpu.Checked)
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
                ProcessBatchImages(folderDialog.SelectedPath, chkPlakaOku.Checked);
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
                        var plateResult = _plateDetector!.Detect(
                            bitmap,
                            (float)nudConfidenceThreshold.Value,
                            chkEnableNMS.Checked,
                            (float)nudNMSThreshold.Value);

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
                            SavePlateImageBatch(bitmap, plate, fileName, totalPlatesFound, doOcr);

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
                Log($"?? Toplam süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye");
                Log($"?? Ortalama hız: {(processedCount / stopwatch.Elapsed.TotalSeconds):F2} resim/saniye");

                MessageBox.Show(
                    $"Toplu işleme tamamlandı!\n\n" +
                    $"İşlenen resim: {processedCount}\n" +
                    $"Bulunan plaka: {totalPlatesFound}\n" +
                    $"Süre: {stopwatch.Elapsed.TotalSeconds:F2} saniye\n\n" +
                    $"Plaka resimleri '{_outputFolder}' klasörüne kaydedildi.",
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

        private void SavePlateImageBatch(Bitmap originalBitmap, LicensePlateDetection plate, string originalFileName, int plateIndex, bool doOcr)
        {
            try
            {
                var plateRect = plate.GetRectangle();
                plateRect.Intersect(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));

                if (plateRect.Width <= 0 || plateRect.Height <= 0)
                    return;

                using var plateBitmap = originalBitmap.Clone(plateRect, originalBitmap.PixelFormat);

                string filename;

                if (doOcr)
                {
                    // OCR ile plaka okuma (mevcut aktif model kullanılır)
                    // Batch işleme sırasında Titan V8 ile OCR yapılması istendi -> Titan V8 metodunu kullan
                    var plateText = ProcessPlateCharactersTitanV8Only(originalBitmap, plate); // Sadece Titan V8 modeli çalıştırılır
                    
                    if (string.IsNullOrWhiteSpace(plateText))
                    {
                        // Okunamadıysa fallback, timestamp kullan
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                        filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                    }
                    else
                    {
                        // Plaka metni geçerli
                        var safePlateText = string.Concat(plateText.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                        filename = $"{safePlateText}.jpg";
                    }
                }
                else
                {
                    // Eski yöntem
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var safeOriginalName = string.Concat(Path.GetFileNameWithoutExtension(originalFileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                    filename = $"{timestamp}_plate_{plateIndex:D4}_{safeOriginalName}_{plate.Confidence:F3}.jpg";
                }

                var fullPath = Path.Combine(_outputFolder, filename);

                // Çakışma kontrolü ve yönetimi
                if (File.Exists(fullPath))
                {
                    bool overwrite = false;
                    try 
                    {
                        // Dosya boyutlarını karşılaştır (basit içerik kontrolü varsayımı)
                        // Not: Bitmap kaydetmeden önce boyutunu tam bilemeyiz, bu yüzden mevcut dosyayla karşılaştırmak zor.
                        // Ancak kullanıcı kuralı: "Aynı boyuttaysa üzerine yazsın. Boyutları farklı ise indexlesin"
                        // Burada mantıksal bir sorun var: Henüz kaydetmediğimiz resmin boyutunu (byte olarak) bilmiyoruz.
                        // Kaydedip sonra kontrol etmek gerekebilir veya geçici bir dosyaya kaydedip karşılaştırabiliriz.
                        // Veya sadece isim çakışmasına odaklanıp, eğer dosya varsa indexleyelim (farklı resimse).
                        // Fakat kullanıcı "aynı boyuttaysa üzerine yazsın" dedi. 
                        
                        // Strateji: Geçici belleğe kaydet, boyutunu al.
                        using (var ms = new MemoryStream())
                        {
                            plateBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            long newSize = ms.Length;
                            
                            var existingInfo = new FileInfo(fullPath);
                            if (existingInfo.Length == newSize)
                            {
                                overwrite = true;
                            }
                        }
                    }
                    catch 
                    {
                        // Hata durumunda güvenli yol: indexle
                        overwrite = false;
                    }

                    if (!overwrite)
                    {
                        // İndeksle: xxxxxxx_02.jpg
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                        string ext = Path.GetExtension(filename);
                        int counter = 2;
                        
                        do
                        {
                            var newName = $"{nameWithoutExt}_{counter:D2}{ext}";
                            fullPath = Path.Combine(_outputFolder, newName);
                            
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
                                catch {}
                                counter++;
                            }
                            else
                            {
                                // Dosya yok, buraya yaz
                                break;
                            }

                        } while (true);
                    }
                }

                plateBitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                Log($"    ?? Plaka kaydedildi: {Path.GetFileName(fullPath)}");
            }
            catch (Exception ex)
            {
                Log($"?? Plaka kaydetme hatası: {ex.Message}");
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
                SafeLog($"? Video işleme hatası: {ex.Message}");
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

                var plateResult = _plateDetector!.Detect(
                    originalBitmap,
                    (float)nudConfidenceThreshold.Value,
                    chkEnableNMS.Checked,
                    (float)nudNMSThreshold.Value);

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
                using var plateMat = BitmapConverter.ToMat(plateBitmap);

                // Süre ölçümleri için Stopwatch
                var swModel = new Stopwatch();

                swModel.Restart();
                var sResult = _charDetector?.RunOnnxPlateRecognition(plateBitmap).Detection ?? string.Empty;
                swModel.Stop();
                long sTime = swModel.ElapsedMilliseconds;

                // Eğer video oynatılıyorsa SADECE Model S çalışsın ve dönsün (Performans için)
                if (_isVideoPlaying)
                {
                    SafeLog($"?? OCR: '{sResult}' ({sTime}ms)");
                    return sResult;
                }

                swModel.Restart();
                var xsResult = _charDetectorXS?.RunOnnxPlateRecognition(plateBitmap).Detection ?? string.Empty;
                swModel.Stop();
                long xsTime = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var v3Result = _titanArmorDetector?.Predict(plateMat) ?? string.Empty;
                swModel.Stop();
                long v3Time = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var sentinelResult = _sentinelDetector?.Predict(plateMat);
                swModel.Stop();
                long sentinelTime = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var absoluteResult = _absoluteDetector?.PredictDetailed(plateMat);
                swModel.Stop();
                long absTime = swModel.ElapsedMilliseconds;

                swModel.Restart();
                var v8Result = _titanV8Detector?.Predict(plateMat);
                swModel.Stop();
                long v8Time = swModel.ElapsedMilliseconds;

                SafeLog($"?? OCR SONUÇLARI:");
                SafeLog($"   - Model S".PadRight(30) + $": '{(string.IsNullOrEmpty(sResult) ? "[Yüklenmedi]" : sResult)}' ({sTime}ms)");
                SafeLog($"   - Model XS".PadRight(30) + $": '{(string.IsNullOrEmpty(xsResult) ? "[Yüklenmedi]" : xsResult)}' ({xsTime}ms)");
                SafeLog($"   - Titan v3".PadRight(30) + $": '{(string.IsNullOrEmpty(v3Result) ? "[Boş]" : v3Result)}' ({v3Time}ms)");
                
                if (sentinelResult != null)
                {
                    LogSentinelResult(sentinelResult, sentinelTime);
                }
                else
                {
                    SafeLog($"   - Sentinel v6: [Dedektör Yüklenemedi]", Color.Gray);
                }

                if (absoluteResult != null)
                {
                    LogAbsoluteResult(absoluteResult, absTime);
                }
                else
                {
                    SafeLog($"   - Abs v6.3   : [Dedektör Yüklenemedi]", Color.Gray);
                }

                if (v8Result != null)
                {
                    LogV8Result(v8Result, v8Time);
                }
                else
                {
                    SafeLog($"   - Titan v8   : [Dedektör Yüklenemedi]", Color.Gray);
                }

                // Öncelik Sırası: Model S > Model XS > Titan V8 > Absolute > Sentinel > V3
                // Kullanıcı isteği: İlk modelin (S) tahmini basılmalı
                if (!string.IsNullOrEmpty(sResult)) return sResult;
                if (!string.IsNullOrEmpty(xsResult)) return xsResult;
                if (v8Result != null && !string.IsNullOrEmpty(v8Result.Text)) return v8Result.Text;
                if (absoluteResult != null && !string.IsNullOrEmpty(absoluteResult.Text)) return absoluteResult.Text;
                if (sentinelResult != null && !string.IsNullOrEmpty(sentinelResult.Text)) return sentinelResult.Text;
                return v3Result;
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

        // Helper methods
        private bool AreDetectorsReady() => _plateDetector != null && (_charDetector != null || _charDetectorXS != null || _titanArmorDetector != null || _titanV8Detector != null);

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
            btnSelectPlateModel.Enabled = !playing; // Model değiştirme video sırasında devre dışı
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

        private void SafeLog(string message, Color? color = null)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(message, color));
                return;
            }
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

        private void LogSentinelResult(SentinelResult result, long durationMs)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogSentinelResult(result, durationMs));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            // Başlık kısmı
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = Color.DarkSlateBlue;
            txtLog.AppendText($"[{timestamp}] " + "   - Sentinel v6".PadRight(30) + ": '");

            // Karakter karakter renklendirme
            foreach (var detail in result.Details)
            {
                // Güven puanına göre grilik ayarı (1.0 = Siyah, 0.0 = Açık Gri)
                // Formül: 255 * (1 - conf) -> ama çok açık olmasın diye sınırlıyoruz
                int colorVal = (int)(180 * (1.0f - detail.Confidence));
                txtLog.SelectionColor = Color.FromArgb(colorVal, colorVal, colorVal);
                txtLog.AppendText(detail.Character.ToString());
            }

            txtLog.SelectionColor = Color.DarkSlateBlue;
            float avgAcc = result.Details.Any() ? result.Details.Average(d => d.Confidence) : 0f;
            txtLog.AppendText($"' (Acc: {avgAcc:P1}, Secure: {result.IsSecure}) ({durationMs}ms){Environment.NewLine}");

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void LogAbsoluteResult(OcrResult result, long durationMs)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogAbsoluteResult(result, durationMs));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            // Başlık kısmı
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = Color.SaddleBrown;
            txtLog.AppendText($"[{timestamp}] " + "   - Abs v6.3".PadRight(30) + ": '");

            foreach (var detail in result.Details)
            {
                int colorVal = (int)(200 * (1.0f - detail.Confidence));
                txtLog.SelectionColor = Color.FromArgb(colorVal, colorVal, colorVal);
                txtLog.AppendText(detail.Character.ToString());
            }

            txtLog.SelectionColor = Color.SaddleBrown;
            float avgAcc = result.Details.Any() ? result.Details.Average(d => d.Confidence) : 0f;
            txtLog.AppendText($"' (Acc: {avgAcc:P1}, Secure: {result.IsSecure}) ({durationMs}ms){Environment.NewLine}");

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void LogV8Result(TitanV8ModelResult result, long durationMs)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogV8Result(result, durationMs));
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
            
            if (InvokeRequired)
            {
                Invoke(() => Log(message, color));
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
                _plateDetector?.Dispose();
                _charDetector?.Dispose();
                _titanArmorDetector?.Dispose();
                _sentinelDetector?.Dispose();
                _absoluteDetector?.Dispose();
                _titanV8Detector?.Dispose();
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