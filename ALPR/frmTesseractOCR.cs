using ALPR.Detection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Xml.Linq;
using Tesseract;

namespace ALPR
{
    public partial class frmTesseractOCR : Form
    {
        private string? _currentImagePath;
        private TesseractOCRDetector? _tesseractDetector;
        private bool _isProcessing;
        private bool _isTesseractInitialized = false;

        public frmTesseractOCR()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            SetupGpuSettings();
            DisableDetectionUI();
            InitializeTesseract();
            UpdateStatus("?? Tesseract OCR hazýr. Plaka resmi yükleyin ve iþleme baþlayýn.");
        }

        private async void InitializeTesseract()
        {
            try
            {
                Log("?? Tesseract OCR baþlatýlýyor...");
                
                await Task.Run(async () =>
                {
                    await Task.Delay(1000); // Small delay to let UI load
                    
                    try
                    {
                        _tesseractDetector = new TesseractOCRDetector(null, chkUseGpu.Checked);
                        
                        // Wait a bit for async initialization
                        await Task.Delay(2000);
                        
                        _isTesseractInitialized = true;
                        SafeUpdateStatus("? Tesseract OCR hazýr! Plaka resmi seçin.");
                    }
                    catch (Exception ex)
                    {
                        SafeUpdateStatus($"? Tesseract OCR baþlatma hatasý: {ex.Message}");
                        Log($"? Tesseract OCR baþlatma hatasý: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"? Tesseract OCR initialization exception: {ex.Message}");
                UpdateStatus($"? Tesseract OCR baþlatýlamadý: {ex.Message}");
            }
        }

        private void SetupGpuSettings()
        {
            // Tesseract CPU tabanlý, GPU seçeneðini devre dýþý býrak
            chkUseGpu.Enabled = false;
            chkUseGpu.Checked = false;
            chkUseGpu.Text = "??? CPU (Tesseract)";

            Log("GPU durumu: Tesseract CPU tabanlý çalýþýr");
        }

        private void DisableDetectionUI()
        {
            // Detection bölümünü devre dýþý býrak - sadece recognition modu
            btnSelectDetModel.Enabled = false;
            btnAnalyzeDetModel.Enabled = false;
            lblDetModel.Text = "Detection Model: ? DEVRE DIÞI (Sadece Recognition)";
            lblDetModel.ForeColor = Color.Gray;
            
            // Recognition model UI'ýný güncelleyin
            lblRecModel.Text = "Recognition Model: ? Tesseract Engine (Otomatik)";
            lblRecModel.ForeColor = Color.DarkGreen;
            
            chkUseRecModel.Checked = true;
            chkUseRecModel.Text = "?? Sadece Recognition Modu";
            chkUseRecModel.Enabled = false; // Checkbox'ý kilitle
            
            btnSelectRecModel.Enabled = false; // Model seçimi otomatik
            btnAnalyzeRecModel.Enabled = false; // Analiz gerekmiyor
        }

        private void btnSelectDetModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Detection model devre dýþý! Bu sürümde sadece Recognition modu aktif.", 
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSelectRecModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Recognition model otomatik olarak Tesseract Engine kullanýyor.\nManuel model seçimi gerekmiyor.", 
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAnalyzeDetModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Detection model devre dýþý!", 
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAnalyzeRecModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tesseract OCR Engine kullanýlýyor.\n\nModel Detaylarý:\n" +
                "- Engine: Tesseract 5.x\n" +
                "- Recognition: LSTM Neural Network\n" +
                "- Language: English (eng.traineddata)\n" +
                "- Optimized: License Plates\n" +
                "- Character Set: A-Z, 0-9\n" +
                "- Page Segmentation: Single Word Mode", 
                "Model Bilgisi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void chkUseRecModel_CheckedChanged(object sender, EventArgs e)
        {
            // Checkbox kilitli - deðiþiklik yok
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Plaka Resmi Seçin",
                Filter = "Resim Dosyalarý|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentImagePath = dialog.FileName;
                
                try
                {
                    // Load image using OpenCV for display (keeping original display logic)
                    using var mat = Cv2.ImRead(_currentImagePath);
                    if (!mat.Empty())
                    {
                        pictureBoxImage.Image?.Dispose();  
                        pictureBoxImage.Image = BitmapConverter.ToBitmap(mat);
                        
                        UpdateStatus($"?? Plaka resmi yüklendi: {Path.GetFileName(_currentImagePath)} ({mat.Width}x{mat.Height})");
                        Log($"Resim yüklendi: {_currentImagePath}");
                        
                        ValidateAndEnableProcessing();
                    }
                    else
                    {
                        UpdateStatus("? Resim dosyasý geçersiz!");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"? Resim yükleme hatasý: {ex.Message}");
                    Log($"Resim yükleme hatasý: {ex.Message}");
                    MessageBox.Show($"Resim yüklenirken hata oluþtu:\n{ex.Message}", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (_isProcessing || !IsReadyForProcessing())
                return;

            await ProcessImage();
        }

        private async Task ProcessImage()
        {
            _isProcessing = true;
            btnProcess.Enabled = false;
            btnProcess.Text = "?? Okuyorum...";
            btnProcess.BackColor = Color.Orange;

            try
            {
                UpdateStatus("?? Plaka okunuyor...");
                
                if (_tesseractDetector == null)
                {
                    UpdateStatus("? OCR motoru hazýr deðil!");
                    return;
                }

                // Load the image using Pix directly
                using (Pix imgSrc = Pix.LoadFromFile(_currentImagePath!))
                {
                    if (imgSrc == null)
                    {
                        UpdateStatus("? Resim yüklenemedi!");
                        return;
                    }

                    // Perform OCR and measure elapsed time
                    Stopwatch stopWatch = Stopwatch.StartNew();
                    var result = await Task.Run(() => _tesseractDetector.RecognizeDirectlyFromPix(imgSrc));
                    stopWatch.Stop();
                    
                    Console.WriteLine($"Elapsed={stopWatch.ElapsedMilliseconds} ms");
                    if (result.RecognitionResults.Count > 0)
                    {
                        Console.WriteLine(result.RecognitionResults[0].Text);
                    }

                    // Update UI with results
                    DisplayResults(result, stopWatch.ElapsedMilliseconds);
                    
                    // Create visualization using the original image
                    var visualizedImage = CreateVisualizationFromPath(result);
                    if (visualizedImage != null)
                    {
                        pictureBoxImage.Image?.Dispose();
                        pictureBoxImage.Image = visualizedImage;
                    }

                    if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                    {
                        UpdateStatus($"? Plaka okundu: {result.RecognitionResults[0].Text} ({stopWatch.ElapsedMilliseconds}ms)");
                    }
                    else
                    {
                        UpdateStatus($"?? Plaka okunamadý ({stopWatch.ElapsedMilliseconds}ms)");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"? OCR hatasý: {ex.Message}");
                Log($"OCR hatasý: {ex.Message}");
                MessageBox.Show($"Plaka okurken hata oluþtu:\n\n{ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnProcess.Enabled = true;
                btnProcess.Text = "?? Plakayý Oku";
                btnProcess.BackColor = SystemColors.Control;
            }
        }

        private void DisplayResults(TesseractOCRResult result, long totalTime)
        {
            lblProcessingTime.Text = $"Okuma Süresi: {totalTime}ms";
            lblDetectedRegions.Text = "Tespit: Direkt okuma modu (Pix)";

            var resultText = new List<string>();
            resultText.Add($"=== TESSERACT OCR PLAKA OKUMA SONUÇLARI ===");
            resultText.Add($"Okuma Süresi: {totalTime}ms");
            resultText.Add($"Ýþleme Modu: ?? Tesseract Engine (Pix direkt)");
            resultText.Add($"CPU Durumu: ??? Aktif (Tesseract CPU tabanlý)");
            resultText.Add("");

            // Hata mesajý varsa göster
            if (!string.IsNullOrEmpty(result.DetectionResult.ErrorMessage))
            {
                resultText.Add("? HATA:");
                resultText.Add(result.DetectionResult.ErrorMessage);
                resultText.Add("");
                resultText.Add("?? Çözüm Önerileri:");
                resultText.Add("- Tesseract-OCR'ýn yüklü olduðundan emin olun");
                resultText.Add("- tessdata dizininde eng.traineddata dosyasýnýn bulunduðunu kontrol edin");
                resultText.Add("- Plaka resminin net ve düzgün olduðundan emin olun");
                resultText.Add("- Resmin yeterince büyük olduðunu kontrol edin (min. 32x32)");
                resultText.Add("- Farklý bir plaka resmi deneyin");
                resultText.Add("");
                
                UpdateStatus($"? Hata: {result.DetectionResult.ErrorMessage}");
            }

            if (result.RecognitionResults.Count > 0)
            {
                var recognition = result.RecognitionResults[0];
                lblRecognizedText.Text = $"Okunan Plaka: {recognition.Text}";
                
                resultText.Add("=== OKUNAN PLAKA ===");
                resultText.Add($"?? Plaka: \"{recognition.Text}\"");
                resultText.Add($"?? Güven: {recognition.Confidence:P2}");
                resultText.Add($"?? Resim boyutu: {recognition.BoundingBox.Width:F0}x{recognition.BoundingBox.Height:F0}");
                resultText.Add("");
                
                // Plaka formatý kontrol et
                if (IsValidTurkishPlate(recognition.Text))
                {
                    resultText.Add("? Geçerli Türk plaka formatý");
                }
                else if (IsValidGeneralPlate(recognition.Text))
                {
                    resultText.Add("? Genel plaka formatý (yurtdýþý olabilir)");
                }
                else
                {
                    resultText.Add("?? Plaka formatý belirsiz");
                    
                    // Format analizi
                    if (recognition.Text != "NO_TEXT" && !string.IsNullOrEmpty(recognition.Text))
                    {
                        resultText.Add($"?? Okunan metin uzunluðu: {recognition.Text.Length}");
                        resultText.Add($"?? Ýçerik analizi: {AnalyzePlateContent(recognition.Text)}");
                    }
                }
                
                resultText.Add("");
                resultText.Add("=== TEKNÝK DETAYLAR ===");
                resultText.Add($"?? OCR Engine: Tesseract 5.x");
                resultText.Add($"?? Neural Network: LSTM");
                resultText.Add($"?? Language: English (eng)");
                resultText.Add($"?? Character Set: A-Z, 0-9");
                resultText.Add($"?? Page Segmentation: Single Word");
                resultText.Add($"?? Input Method: Pix.LoadFromFile (direkt)");
            }
            else
            {
                lblRecognizedText.Text = "Okunan Plaka: Yok";
                resultText.Add("? Hiç plaka okunamadý");
                resultText.Add("");
                resultText.Add("?? Öneriler:");
                resultText.Add("  - Daha net bir plaka resmi deneyin");
                resultText.Add("  - Resmin kontrastýný artýrýn");
                resultText.Add("  - Plaka metninin tamamen görünür olduðundan emin olun");
                resultText.Add("  - Farklý açýdan çekilmiþ resim deneyin");
                resultText.Add("  - Tesseract-OCR'ýn doðru kurulu olduðunu kontrol edin");
                resultText.Add("");
                resultText.Add("?? Troubleshooting:");
                resultText.Add("  - Resim boyutu yeterli mi? (önerilen: min 100x30)");
                resultText.Add("  - Plaka karakterleri net okunabiliyor mu?");
                resultText.Add("  - Arka plan çok karýþýk deðil mi?");
                resultText.Add("  - Aydýnlatma uygun mu?");
                resultText.Add("  - eng.traineddata dosyasý tessdata dizininde mevcut mu?");
            }

            txtResults.Text = string.Join(Environment.NewLine, resultText);
        }

        private string AnalyzePlateContent(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Boþ";
            
            var analysis = new List<string>();
            
            var digitCount = text.Count(char.IsDigit);
            var letterCount = text.Count(char.IsLetter);
            var otherCount = text.Length - digitCount - letterCount;
            
            analysis.Add($"Rakam: {digitCount}");
            analysis.Add($"Harf: {letterCount}");
            if (otherCount > 0) analysis.Add($"Diðer: {otherCount}");
            
            return string.Join(", ", analysis);
        }

        private bool IsValidTurkishPlate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 6 || text.Length > 8)
                return false;
                
            // Basit Türk plaka formatý kontrolü
            var turkishPlatePattern = @"^[0-9]{2}[A-Z]{1,3}[0-9]{2,4}$";
            return System.Text.RegularExpressions.Regex.IsMatch(text, turkishPlatePattern);
        }

        private bool IsValidGeneralPlate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 4 || text.Length > 10)
                return false;
                
            // Genel plaka formatý: en az 2 karakter, alfanumerik
            var hasDigit = text.Any(char.IsDigit);
            var hasLetter = text.Any(char.IsLetter);
            var allAlphaNumeric = text.All(char.IsLetterOrDigit);
            
            return allAlphaNumeric && (hasDigit || hasLetter);
        }

        private Bitmap? CreateVisualizationFromPath(TesseractOCRResult result)
        {
            try
            {
                // Load original image with OpenCV for visualization
                using var imgSrc = Cv2.ImRead(_currentImagePath!);
                if (imgSrc.Empty()) return null;
                
                // Convert Mat to Bitmap for visualization
                var originalBitmap = BitmapConverter.ToBitmap(imgSrc);
                var visualized = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(visualized);
                
                using var font = new Font("Arial", 14, FontStyle.Bold);

                if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                {
                    // Baþarýlý okuma - yeþil çerçeve
                    using var successPen = new Pen(Color.LimeGreen, 4);
                    g.DrawRectangle(successPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                    
                    var recognizedText = result.RecognitionResults[0].Text;
                    var confidence = result.RecognitionResults[0].Confidence;
                    var displayText = $"?? {recognizedText} ({confidence:P1}) [Pix]";
                    
                    // Text shadow için siyah arka plan
                    using var shadowBrush = new SolidBrush(Color.FromArgb(200, Color.Black));
                    using var textBrush = new SolidBrush(Color.White);
                    
                    var textSize = g.MeasureString(displayText, font);
                    var textRect = new RectangleF(10, 10, textSize.Width + 20, textSize.Height + 10);
                    
                    // Shadow effect
                    var shadowRect = new RectangleF(textRect.X + 2, textRect.Y + 2, textRect.Width, textRect.Height);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.Black)), shadowRect);
                    
                    g.FillRectangle(shadowBrush, textRect);
                    g.DrawString(displayText, font, textBrush, textRect.X + 10, textRect.Y + 5);
                    
                    // Baþarý ikonu
                    g.DrawString("?", new Font("Arial", 20), textBrush, textRect.Right - 30, textRect.Y);
                }
                else
                {
                    // Baþarýsýz okuma - kýrmýzý çerçeve
                    using var errorPen = new Pen(Color.Red, 4);
                    g.DrawRectangle(errorPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                    
                    var errorText = "? OKUNAMADI [Pix]";
                    var reason = !string.IsNullOrEmpty(result.DetectionResult.ErrorMessage) 
                        ? result.DetectionResult.ErrorMessage 
                        : "Metin bulunamadý";
                    
                    using var errorBrush = new SolidBrush(Color.FromArgb(200, Color.DarkRed));
                    using var textBrush = new SolidBrush(Color.White);
                    
                    var textSize = g.MeasureString(errorText, font);
                    var textRect = new RectangleF(10, 10, textSize.Width + 20, textSize.Height + 40);
                    
                    g.FillRectangle(errorBrush, textRect);
                    g.DrawString(errorText, font, textBrush, textRect.X + 10, textRect.Y + 5);
                    g.DrawString(reason, new Font("Arial", 9), textBrush, textRect.X + 10, textRect.Y + 25);
                }

                originalBitmap.Dispose();
                return visualized;
            }
            catch (Exception ex)
            {
                Log($"Görselleþtirme hatasý: {ex.Message}");
                return null;
            }
        }

        private void ValidateAndEnableProcessing()
        {
            bool hasImage = !string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath);
            
            btnProcess.Enabled = hasImage && _isTesseractInitialized && !_isProcessing;

            if (!hasImage)
            {
                UpdateStatus("?? Plaka resmi seçin!");
            }
            else if (!_isTesseractInitialized)
            {
                UpdateStatus("?? Tesseract OCR yükleniyor, lütfen bekleyin...");
            }
            else
            {
                UpdateStatus("? Plaka okumaya hazýr!");
            }
        }

        private bool IsReadyForProcessing()
        {
            bool hasImage = !string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath);
            return hasImage && _isTesseractInitialized;
        }

        private void SafeUpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatus(message));
                return;
            }
            UpdateStatus(message);
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

        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.Diagnostics.Debug.WriteLine($"[{timestamp}] frmTesseractOCR: {message}");
        }

        private void frmTesseractOCR_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log("Form kapatýlýyor...");
            _tesseractDetector?.Dispose();
            pictureBoxImage.Image?.Dispose();
        }
    }
}