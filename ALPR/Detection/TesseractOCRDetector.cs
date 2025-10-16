using Tesseract;
using System.Diagnostics;

namespace ALPR.Detection
{
    public class TesseractOCRDetector : IDisposable
    {
        private TesseractEngine? _ocrEngine;
        private bool _disposed = false;
        private string _tessDataPath;

        public bool HasRecognitionModel => _ocrEngine != null;

        public TesseractOCRDetector(string? tessDataPath = null, bool useGpu = false)
        {
            _tessDataPath = tessDataPath ?? GetDefaultTessDataPath();
            DebugLog("?? TesseractOCRDetector baþlatýlýyor...");
            DebugLog($"?? TessData path: {_tessDataPath}");
            DebugLog($"?? GPU kullan: {useGpu} (Not: Tesseract CPU tabanlý)");
            DebugLog("?? License plate recognition için özelleþtirildi!");
            
            _ = InitializeTesseractAsync();
        }

        private string GetDefaultTessDataPath()
        {
            // Birkaç farklý lokasyonu kontrol et
            var possiblePaths = new[]
            {
                Path.Combine(Environment.CurrentDirectory, "tessdata"),
                Path.Combine(Environment.CurrentDirectory, "TessData"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TessData"),
                @"C:\Program Files\Tesseract-OCR\tessdata",
                @"C:\Tools\tesseract\tessdata",
                Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? ""
            };

            foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (Directory.Exists(path))
                {
                    DebugLog($"? TessData dizini bulundu: {path}");
                    return path;
                }
            }

            // Default olarak tessdata dizinini oluþtur
            var defaultPath = Path.Combine(Environment.CurrentDirectory, "tessdata");
            Directory.CreateDirectory(defaultPath);
            DebugLog($"?? TessData dizini oluþturuldu: {defaultPath}");
            return defaultPath;
        }

        private async Task InitializeTesseractAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    DebugLog("?? Tesseract engine yükleniyor...");
                    
                    // Tesseract engine'i baþlat (Ýngilizce dil paketi)
                    _ocrEngine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                    
                    // License plate için optimize ayarlar
                    //_ocrEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
                    //_ocrEngine.SetVariable("classify_bln_numeric_mode", "1");
                    //_ocrEngine.SetVariable("tessedit_pageseg_mode", "8"); // Single word mode
                    //_ocrEngine.SetVariable("tessedit_ocr_engine_mode", "1"); // LSTM only
                    
                    DebugLog("? Tesseract baþarýyla yüklendi (Optimized for License Plates)!");
                });
            }
            catch (Exception ex)
            {
                DebugLog($"? Tesseract yükleme hatasý: {ex.Message}");
                DebugLog($"?? Stack trace: {ex.StackTrace}");
                DebugLog("?? Çözüm: Tesseract-OCR'ýn yüklü olduðundan ve tessdata dizininde eng.traineddata dosyasýnýn bulunduðundan emin olun");
            }
        }

        public TesseractOCRResult RecognizeDirectly(Bitmap bitmap)
        {
            DebugLog("?? RecognizeDirectly baþlýyor...");
            DebugLog($"?? Input resim: {bitmap.Width}x{bitmap.Height}, Format: {bitmap.PixelFormat}");

            var sw = Stopwatch.StartNew();
            
            try
            {
                if (_ocrEngine == null)
                {
                    DebugLog("? OCR engine hazýr deðil!");
                    return CreateErrorResult(bitmap, "OCR engine hazýr deðil - Tesseract baþlatýlamadý", sw.ElapsedMilliseconds);
                }

                // Bitmap'i Pix'e dönüþtür - Tesseract.NET için gerekli
                using var pix = Pix.LoadFromMemory(GetBitmapData(bitmap));
                using var page = _ocrEngine.Process(pix);
                
                var text = page.GetText().Trim();
                var confidence = page.GetMeanConfidence();
                
                sw.Stop();

                DebugLog($"?? Tesseract iþlemi {sw.ElapsedMilliseconds}ms sürdü");
                DebugLog($"?? Algýlanan metin: '{text}'");
                DebugLog($"?? Güven oraný: {confidence:P2}");

                // Clean up the text (remove spaces, special characters for license plates)
                var cleanText = CleanPlateText(text);
                
                if (cleanText != text)
                {
                    DebugLog($"?? Temizlenmiþ metin: '{cleanText}'");
                }

                var recognitionResult = new TesseractTextRecognition
                {
                    Text = cleanText,
                    BoundingBox = new RectangleF(0, 0, bitmap.Width, bitmap.Height),
                    Confidence = confidence
                };

                DebugLog($"? Baþarýlý result oluþturuluyor");
                return new TesseractOCRResult
                {
                    DetectionResult = new TesseractDetectionResult
                    {
                        TextRegions = new List<TesseractTextRegion>(),
                        InferenceTimeMs = sw.ElapsedMilliseconds,
                        InputWidth = bitmap.Width,
                        InputHeight = bitmap.Height
                    },
                    RecognitionResults = new List<TesseractTextRecognition> { recognitionResult },
                    InferenceTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                DebugLog($"? RecognizeDirectly exception: {ex.Message}");
                DebugLog($"?? Stack trace: {ex.StackTrace}");
                
                return CreateErrorResult(bitmap, $"Recognition hatasý: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public TesseractOCRResult RecognizeDirectlyFromPix(Pix pix)
        {
            DebugLog("?? RecognizeDirectlyFromPix baþlýyor...");
            DebugLog($"?? Input Pix: {pix.Width}x{pix.Height}, Depth: {pix.Depth}");

            var sw = Stopwatch.StartNew();
            
            try
            {
                if (_ocrEngine == null)
                {
                    DebugLog("? OCR engine hazýr deðil!");
                    return CreateErrorResultFromPix(pix, "OCR engine hazýr deðil - Tesseract baþlatýlamadý", sw.ElapsedMilliseconds);
                }

                // Pix'i doðrudan kullan - dönüþüm gerekmez
                using var page = _ocrEngine.Process(pix);
                
                var text = page.GetText().Trim();
                var confidence = page.GetMeanConfidence();
                
                sw.Stop();

                DebugLog($"?? Tesseract iþlemi {sw.ElapsedMilliseconds}ms sürdü");
                DebugLog($"?? Algýlanan metin: '{text}'");
                DebugLog($"?? Güven oraný: {confidence:P2}");

                // Clean up the text (remove spaces, special characters for license plates)
                var cleanText = CleanPlateText(text);
                
                if (cleanText != text)
                {
                    DebugLog($"?? Temizlenmiþ metin: '{cleanText}'");
                }

                var recognitionResult = new TesseractTextRecognition
                {
                    Text = cleanText,
                    BoundingBox = new RectangleF(0, 0, pix.Width, pix.Height),
                    Confidence = confidence
                };

                DebugLog($"? Baþarýlý result oluþturuluyor");
                return new TesseractOCRResult
                {
                    DetectionResult = new TesseractDetectionResult
                    {
                        TextRegions = new List<TesseractTextRegion>(),
                        InferenceTimeMs = sw.ElapsedMilliseconds,
                        InputWidth = pix.Width,
                        InputHeight = pix.Height
                    },
                    RecognitionResults = new List<TesseractTextRecognition> { recognitionResult },
                    InferenceTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                DebugLog($"? RecognizeDirectlyFromPix exception: {ex.Message}");
                DebugLog($"?? Stack trace: {ex.StackTrace}");
                
                return CreateErrorResultFromPix(pix, $"Recognition hatasý: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        //public TesseractOCRResult RecognizeDirectlyFromMat(Mat imgSrc)
        //{
        //    DebugLog("?? RecognizeDirectlyFromMat baþlýyor...");
        //    DebugLog($"?? Input Mat: {imgSrc.Width}x{imgSrc.Height}, Channels: {imgSrc.Channels()}");

        //    using var bitmap = BitmapConverter.ToBitmap(imgSrc);
        //    return RecognizeDirectly(bitmap);
        //}

        private byte[] GetBitmapData(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private TesseractOCRResult CreateErrorResult(Bitmap bitmap, string errorMessage, long elapsedMs)
        {
            return new TesseractOCRResult
            {
                DetectionResult = new TesseractDetectionResult
                {
                    TextRegions = new List<TesseractTextRegion>(),
                    InferenceTimeMs = elapsedMs,
                    InputWidth = bitmap.Width,
                    InputHeight = bitmap.Height,
                    ErrorMessage = errorMessage
                },
                RecognitionResults = new List<TesseractTextRecognition>(),
                InferenceTimeMs = elapsedMs
            };
        }

        private TesseractOCRResult CreateErrorResultFromPix(Pix pix, string errorMessage, long elapsedMs)
        {
            return new TesseractOCRResult
            {
                DetectionResult = new TesseractDetectionResult
                {
                    TextRegions = new List<TesseractTextRegion>(),
                    InferenceTimeMs = elapsedMs,
                    InputWidth = pix.Width,
                    InputHeight = pix.Height,
                    ErrorMessage = errorMessage
                },
                RecognitionResults = new List<TesseractTextRecognition>(),
                InferenceTimeMs = elapsedMs
            };
        }

        private static string CleanPlateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove common OCR artifacts and normalize text for license plates
            var cleaned = text.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("|", "")
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .ToUpperInvariant();

            // Filter out non-alphanumeric characters (keep only letters and numbers)
            return new string(cleaned.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }

        private static void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.Diagnostics.Debug.WriteLine($"[{timestamp}] {message}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DebugLog("??? TesseractOCRDetector dispose ediliyor...");
                _ocrEngine?.Dispose();
                _disposed = true;
                DebugLog("? TesseractOCRDetector dispose edildi");
            }
        }
    }

    public class TesseractOCRResult
    {
        public TesseractDetectionResult DetectionResult { get; set; } = new();
        public List<TesseractTextRecognition> RecognitionResults { get; set; } = new();
        public long InferenceTimeMs { get; set; }
    }

    public class TesseractDetectionResult
    {
        public List<TesseractTextRegion> TextRegions { get; set; } = new();
        public long InferenceTimeMs { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TesseractTextRegion
    {
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
        public PointF[] Contour { get; set; } = Array.Empty<PointF>();
    }

    public class TesseractTextRecognition
    {
        public string Text { get; set; } = string.Empty;
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
    }
}