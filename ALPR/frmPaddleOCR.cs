using ALPR.Detection;
using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ALPR
{
    public partial class frmPaddleOCR : Form
    {
        private string? _currentImagePath;
        private PaddleOCRDetector? _paddleDetector;
        private bool _isProcessing;
        private bool _isPaddleOcrInitialized = false;

        public frmPaddleOCR()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            SetupGpuSettings();
            DisableDetectionUI();
            InitializePaddleOCR();
            UpdateStatus("?? PaddleOCR hazýr. Plaka resmi yükleyin ve iþleme baþlayýn.");
        }

        private async void InitializePaddleOCR()
        {
            try
            {
                Log("?? PaddleOCR baþlatýlýyor...");
                
                await Task.Run(async () =>
                {
                    await Task.Delay(1000); // Small delay to let UI load
                    
                    try
                    {
                        _paddleDetector = new PaddleOCRDetector(null, null, chkUseGpu.Checked);
                        
                        // Wait a bit for async initialization
                        await Task.Delay(3000);
                        
                        _isPaddleOcrInitialized = true;
                        SafeUpdateStatus("? PaddleOCR hazýr! Plaka resmi seçin.");
                    }
                    catch (Exception ex)
                    {
                        SafeUpdateStatus($"? PaddleOCR baþlatma hatasý: {ex.Message}");
                        Log($"? PaddleOCR baþlatma hatasý: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"? PaddleOCR initialization exception: {ex.Message}");
                UpdateStatus($"? PaddleOCR baþlatýlamadý: {ex.Message}");
            }
        }

        private void SetupGpuSettings()
        {
            bool gpuAvailable = ExecutionProviderHelper.IsGpuAvailable();
            chkUseGpu.Enabled = gpuAvailable;
            chkUseGpu.Checked = gpuAvailable;

            if (!gpuAvailable)
            {
                chkUseGpu.Text = "? GPU Yok (CPU)";
            }
            else
            {
                chkUseGpu.Text = "?? GPU Kullan";
            }

            Log($"GPU durumu: {(gpuAvailable ? "Mevcut" : "Mevcut deðil")}");
        }

        private void DisableDetectionUI()
        {
            // Detection bölümünü devre dýþý býrak - sadece recognition modu
            btnSelectDetModel.Enabled = false;
            btnAnalyzeDetModel.Enabled = false;
            lblDetModel.Text = "Detection Model: ? DEVRE DIÞI (Sadece Recognition)";
            lblDetModel.ForeColor = Color.Gray;
            
            // Recognition model UI'ýný güncelleyin
            lblRecModel.Text = "Recognition Model: ? PaddleOCR English V3 (Otomatik)";
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
            MessageBox.Show("Recognition model otomatik olarak PaddleOCR English V3 kullanýyor.\nManuel model seçimi gerekmiyor.", 
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAnalyzeDetModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Detection model devre dýþý!", 
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAnalyzeRecModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("PaddleOCR English V3 modeli kullanýlýyor.\n\nModel Detaylarý:\n" +
                "- Detection: PP-OCRv3\n" +
                "- Recognition: CRNN\n" +
                "- Classification: 180° döndürme desteði\n" +
                "- Dil: Ýngilizce (Plakalar için optimize)\n" +
                "- Format: ONNX", 
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
                    // Load image using OpenCV for display
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
                
                if (_paddleDetector == null)
                {
                    UpdateStatus("? OCR motoru hazýr deðil!");
                    return;
                }

                // Load the image using OpenCV Mat (similar to your example)
                using (Mat imgSrc = Cv2.ImRead(_currentImagePath!))
                {
                    if (imgSrc.Empty())
                    {
                        UpdateStatus("? Resim yüklenemedi!");
                        return;
                    }

                    // Perform OCR and measure elapsed time (like in your example)
                    Stopwatch stopWatch = Stopwatch.StartNew();
                    var result = await Task.Run(() => _paddleDetector.RecognizeDirectlyFromMat(imgSrc));

                    // Plaka bölgesi yüksekliði
                    double minRel = 0.18;
                    int minPx = Math.Max(10, (int)(imgSrc.Height * minRel));

                    // RecognitionResults üzerinden filtreleme
                    var filteredRegions = (result.RecognitionResults ?? new List<PaddleTextRecognition>())
                        .Where(r => r.BoundingBox.Height >= minPx && r.BoundingBox.Width >= 4)
                        .ToList();

                    // Sadece filtrelenen kutulardaki metinleri birleþtir
                    string filteredText = string.Join(" ", filteredRegions.Select(r => r.Text));
                    //filteredText = PaddleOCRDetector.CleanPlateText(filteredText);

                    float confidence = filteredRegions.Count > 0 ? (float)filteredRegions.Average(r => r.Confidence) : 0.0f;

                    var recognitionResult = new PaddleTextRecognition
                    {
                        Text = filteredText,
                        BoundingBox = new RectangleF(0, 0, imgSrc.Width, imgSrc.Height),
                        Confidence = confidence
                    };


                    stopWatch.Stop();
                    
                    Console.WriteLine($"Elapsed={stopWatch.ElapsedMilliseconds} ms");
                    if (result.RecognitionResults.Count > 0)
                    {
                        Console.WriteLine(result.RecognitionResults[0].Text);
                    }

                    // Update UI with results
                    DisplayResults(result, stopWatch.ElapsedMilliseconds);
                    
                    // Create visualization using the Mat
                    //var visualizedImage = CreateVisualizationFromMat(imgSrc, result);
                    //if (visualizedImage != null)
                    //{
                    //    pictureBoxImage.Image?.Dispose();
                    //    pictureBoxImage.Image = visualizedImage;
                    //}

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

        private void DisplayResults(PaddleOCRResult result, long totalTime)
        {
            lblProcessingTime.Text = $"Okuma Süresi: {totalTime}ms";
            lblDetectedRegions.Text = "Tespit: Direkt okuma modu";

            var resultText = new List<string>();
            resultText.Add($"=== PADDLEOCR PLAKA OKUMA SONUÇLARI ===");
            resultText.Add($"Okuma Süresi: {totalTime}ms");
            resultText.Add($"Ýþleme Modu: ?? Sdcb.PaddleOCR English V3");
            resultText.Add($"GPU Durumu: {(chkUseGpu.Checked ? "? Aktif" : "? Pasif")}");
            resultText.Add("");

            // Hata mesajý varsa göster
            if (!string.IsNullOrEmpty(result.DetectionResult.ErrorMessage))
            {
                resultText.Add("? HATA:");
                resultText.Add(result.DetectionResult.ErrorMessage);
                resultText.Add("");
                resultText.Add("?? Çözüm Önerileri:");
                resultText.Add("- Plaka resminin net ve düzgün olduðundan emin olun");
                resultText.Add("- Resmin yeterince büyük olduðunu kontrol edin (min. 32x32)");
                resultText.Add("- Farklý bir plaka resmi deneyin");
                resultText.Add("- Ýnternet baðlantýnýzý kontrol edin (model indirme için)");
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
                resultText.Add($"?? PaddleOCR Model: English V3");
                resultText.Add($"?? Preprocessing: Otomatik");
                resultText.Add($"?? Detection: PP-OCRv3");
                resultText.Add($"?? Recognition: CRNN");
                resultText.Add($"?? Rotation: 180° destekli");
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
                resultText.Add("");
                resultText.Add("?? Troubleshooting:");
                resultText.Add("  - Resim boyutu yeterli mi? (önerilen: min 100x30)");
                resultText.Add("  - Plaka karakterleri net okunabiliyor mu?");
                resultText.Add("  - Arka plan çok karýþýk deðil mi?");
                resultText.Add("  - Aydýnlatma uygun mu?");
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

        private Bitmap? CreateVisualizationFromMat(Mat imgSrc, PaddleOCRResult result)
        {
            try
            {
                // Convert Mat to Bitmap for visualization
                var originalBitmap = BitmapConverter.ToBitmap(imgSrc);
                var visualized = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(visualized);
                
                using var font = new Font("Arial", 14, FontStyle.Bold);
                using var shadowFont = new Font("Arial", 13, FontStyle.Bold);

                if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                {
                    // Baþarýlý okuma - yeþil çerçeve
                    using var successPen = new Pen(Color.LimeGreen, 4);
                    g.DrawRectangle(successPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                    
                    var recognizedText = result.RecognitionResults[0].Text;
                    var confidence = result.RecognitionResults[0].Confidence;
                    var displayText = $"? {recognizedText} ({confidence:P1})";
                    
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
                    
                    var errorText = "? OKUNAMADI";
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
                return BitmapConverter.ToBitmap(imgSrc);
            }
        }

        private Bitmap? CreateVisualization(Bitmap originalBitmap, PaddleOCRResult result)
        {
            try
            {
                var visualized = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(visualized);
                
                using var font = new Font("Arial", 14, FontStyle.Bold);
                using var shadowFont = new Font("Arial", 13, FontStyle.Bold);

                if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                {
                    // Baþarýlý okuma - yeþil çerçeve
                    using var successPen = new Pen(Color.LimeGreen, 4);
                    g.DrawRectangle(successPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                    
                    var recognizedText = result.RecognitionResults[0].Text;
                    var confidence = result.RecognitionResults[0].Confidence;
                    var displayText = $"? {recognizedText} ({confidence:P1})";
                    
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
                    
                    var errorText = "? OKUNAMADI";
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

                return visualized;
            }
            catch (Exception ex)
            {
                Log($"Görselleþtirme hatasý: {ex.Message}");
                return new Bitmap(originalBitmap);
            }
        }

        private void ValidateAndEnableProcessing()
        {
            bool hasImage = !string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath);
            
            btnProcess.Enabled = hasImage && _isPaddleOcrInitialized && !_isProcessing;

            if (!hasImage)
            {
                UpdateStatus("?? Plaka resmi seçin!");
            }
            else if (!_isPaddleOcrInitialized)
            {
                UpdateStatus("? PaddleOCR yükleniyor, lütfen bekleyin...");
            }
            else
            {
                UpdateStatus("? Plaka okumaya hazýr!");
            }
        }

        private bool IsReadyForProcessing()
        {
            bool hasImage = !string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath);
            return hasImage && _isPaddleOcrInitialized;
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
            System.Diagnostics.Debug.WriteLine($"[{timestamp}] frmPaddleOCR: {message}");
        }

        private void frmPaddleOCR_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log("Form kapatýlýyor...");
            _paddleDetector?.Dispose();
            pictureBoxImage.Image?.Dispose();
        }
    }

    // Model analizi form (basitleþtirilmiþ)
    public partial class frmModelAnalysis : Form
    {
        public frmModelAnalysis(string title, string analysisText)
        {
            InitializeComponent();
            this.Text = title;
            this.txtAnalysis.Text = analysisText;
        }

        private void InitializeComponent()
        {
            this.txtAnalysis = new TextBox();
            this.btnClose = new Button();
            this.SuspendLayout();

            // txtAnalysis
            this.txtAnalysis.Location = new System.Drawing.Point(12, 12);
            this.txtAnalysis.Multiline = true;
            this.txtAnalysis.Name = "txtAnalysis";
            this.txtAnalysis.ReadOnly = true;
            this.txtAnalysis.ScrollBars = ScrollBars.Vertical;
            this.txtAnalysis.Size = new System.Drawing.Size(560, 400);
            this.txtAnalysis.TabIndex = 0;
            this.txtAnalysis.Font = new Font("Consolas", 9F);

            // btnClose
            this.btnClose.Location = new System.Drawing.Point(497, 418);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Kapat";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += (s, e) => this.Close();

            // frmModelAnalysis
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 451);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtAnalysis);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmModelAnalysis";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private TextBox txtAnalysis;
        private Button btnClose;
    }
}