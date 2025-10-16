using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using System.Diagnostics;
using static System.Windows.Forms.Design.AxImporter;

namespace ALPR.Detection
{
    public class PaddleOCRDetector : IDisposable
    {
        private PaddleOcrAll? _ocrEngine;
        private InferenceSession? _onnxSession;
        private bool _disposed = false;
        private bool _useGpu;
        private readonly string _onnxModelPath;
        

        public bool HasRecognitionModel => _ocrEngine != null || _onnxSession != null;

        public PaddleOCRDetector(string? detModelPath, string? recModelPath = null, bool useGpu = false)
        {
            _useGpu = useGpu;
            _onnxModelPath = recModelPath ?? "/models/paddle_rec_model.onnx"; // Default ONNX model path
            DebugLog("🔧 PaddleOCRDetector başlatılıyor...");
            DebugLog($"🎮 GPU kullan: {useGpu}");
            DebugLog("🚗 License plate recognition için özelleştirildi!");
            
            _ = InitializePaddleOCRAsync();
            _ = InitializeOnnxSessionAsync();
        }

        private async Task InitializeOnnxSessionAsync()
        {
            try
            {
                DebugLog("📥 ONNX model yükleniyor...");
                
                var sessionOptions = new SessionOptions();
                if (_useGpu)
                {
                    // Enable GPU if available
                    sessionOptions.AppendExecutionProvider_CUDA(0);
                }
                //sessionOptions.AppendExecutionProvider_CPU(); // veya GPU varsa CUDA
                sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
                sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                sessionOptions.EnableMemoryPattern = true;


                if (File.Exists(_onnxModelPath))
                {
                    _onnxSession = new InferenceSession(_onnxModelPath, sessionOptions);
                    DebugLog("✅ ONNX model başarıyla yüklendi!");
                }
                else
                {
                    DebugLog($"⚠️ ONNX model bulunamadı: {_onnxModelPath}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"❌ ONNX model yükleme hatası: {ex.Message}");
                DebugLog($"🔍 Stack trace: {ex.StackTrace}");
            }
        }

        private async Task InitializePaddleOCRAsync()
        {
            try
            {
                DebugLog("📥 PaddleOCR model indiriliyor...");
                
                // Download English OCR model
                FullOcrModel model = await OnlineFullModels.EnglishV3.DownloadAsync();
                

                // Initialize with minimal overhead for license plates
                _ocrEngine = new PaddleOcrAll(model)
                {
                    AllowRotateDetection = false, // Disable rotation for better performance
                    Enable180Classification = false, // Disable 180° classification for better performance  
                };
                
                DebugLog("✅ PaddleOCR başarıyla yüklendi (Optimized for License Plates)!");
            }
            catch (Exception ex)
            {
                DebugLog($"❌ PaddleOCR yükleme hatası: {ex.Message}");
                DebugLog($"🔍 Stack trace: {ex.StackTrace}");
            }
        }

        public PaddleOCRResult RecognizeDirectly(Bitmap bitmap)
        {
            DebugLog("🚀 RecognizeDirectly başlıyor (ONNX Runtime)...");
            DebugLog($"📷 Input resim: {bitmap.Width}x{bitmap.Height}, Format: {bitmap.PixelFormat}");

            var sw = Stopwatch.StartNew();
            
            try
            {
                // Prioritize ONNX Runtime if available
                if (_onnxSession != null)
                {
                    return RecognizeWithOnnx(bitmap, sw);
                }
                else if (_ocrEngine != null)
                {
                    // Fallback to PaddleOCR
                    return RecognizeWithPaddleOCR(bitmap, sw);
                }
                else
                {
                    DebugLog("❌ Hiçbir OCR engine hazır değil!");
                    return CreateErrorResult(bitmap, "OCR engine hazır değil", sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                DebugLog($"❌ RecognizeDirectly exception: {ex.Message}");
                DebugLog($"🔍 Stack trace: {ex.StackTrace}");
                
                return CreateErrorResult(bitmap, $"Recognition hatası: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        private PaddleOCRResult RecognizeWithOnnx(Bitmap bitmap, Stopwatch sw)
        {
            DebugLog("🔮 ONNX Runtime kullanılarak tanıma yapılıyor...");
            
            try
            {
                // Convert bitmap to the format expected by ONNX model (typically 32x128 for CRNN)
                var imageData = PreprocessImageForOnnx(bitmap);
                
                // Create input tensor with shape [1, 1, 32, 128] (batch_size, channels, height, width)
                var inputTensor = new DenseTensor<float>(imageData, new[] { 1, 1, 32, 128 });
                
                // Run inference using ONNX Runtime
                using var session = _onnxSession;
                var result = session!.Run(new[] { NamedOnnxValue.CreateFromTensor("input", inputTensor) });
                
                sw.Stop();
                
                DebugLog($"⏱️ ONNX inference {sw.ElapsedMilliseconds}ms sürdü");
                
                // Process the output tensor to get text
                var outputTensor = result.First().AsTensor<float>();
                var recognizedText = DecodeOnnxOutput(outputTensor);
                
                DebugLog($"📝 ONNX ile algılanan metin: '{recognizedText}'");
                
                // Clean up the text
                var cleanText = CleanPlateText(recognizedText);
                
                if (cleanText != recognizedText)
                {
                    DebugLog($"🧹 Temizlenmiş metin: '{cleanText}'");
                }

                var recognitionResult = new PaddleTextRecognition
                {
                    Text = cleanText,
                    BoundingBox = new RectangleF(0, 0, bitmap.Width, bitmap.Height),
                    Confidence = 0.95f // You can calculate this from the output probabilities if needed
                };

                DebugLog($"✅ ONNX ile başarılı result oluşturuluyor");
                return new PaddleOCRResult
                {
                    DetectionResult = new PaddleDetectionResult
                    {
                        TextRegions = new List<PaddleTextRegion>(),
                        InferenceTimeMs = sw.ElapsedMilliseconds,
                        InputWidth = bitmap.Width,
                        InputHeight = bitmap.Height
                    },
                    RecognitionResults = new List<PaddleTextRecognition> { recognitionResult },
                    InferenceTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                DebugLog($"❌ ONNX inference hatası: {ex.Message}");
                throw; // Re-throw to be caught by the main method
            }
        }

        private float[] PreprocessImageForOnnx(Bitmap bitmap)
        {
            // Convert bitmap to Mat
            using var mat = BitmapConverter.ToMat(bitmap);
            
            // Resize to model input size (32x128 is common for CRNN models)
            using var resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(128, 32));
            
            // Convert to grayscale if needed
            using var gray = resized.Channels() == 3 ? resized.CvtColor(ColorConversionCodes.BGR2GRAY) : resized;
            
            // Normalize pixel values to [0, 1]
            using var normalized = new Mat();
            gray.ConvertTo(normalized, MatType.CV_32F, 1.0 / 255.0);
            
            // Convert to float array
            var imageData = new float[32 * 128];
            
            // Safely copy data from Mat to array
            unsafe
            {
                var ptr = (float*)normalized.Data.ToPointer();
                for (int i = 0; i < imageData.Length; i++)
                {
                    imageData[i] = ptr[i];
                }
            }
            
            return imageData;
        }

        private string DecodeOnnxOutput(Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> outputTensor)
        {
            // This is a simplified decoder - you'll need to implement proper CTC decoding
            // or character mapping based on your specific ONNX model architecture
            
            DebugLog("⚠️ ONNX output decoding - placeholder implementation");
            
            // Basic implementation - find max indices and decode
            var shape = outputTensor.Dimensions;
            var length = shape[0]; // sequence length
            var numClasses = shape[1]; // number of character classes
            
            var result = "";
            for (int i = 0; i < length; i++)
            {
                var maxIndex = 0;
                var maxValue = float.MinValue;
                
                for (int j = 0; j < numClasses; j++)
                {
                    var value = outputTensor[i, j];
                    if (value > maxValue)
                    {
                        maxValue = value;
                        maxIndex = j;
                    }
                }
                
                // Simple character mapping (you'll need to use your model's actual character set)
                if (maxIndex > 0 && maxIndex < 37) // assuming 0 is blank, 1-36 are characters
                {
                    if (maxIndex <= 26)
                        result += (char)('A' + maxIndex - 1);
                    else if (maxIndex <= 36)
                        result += (char)('0' + maxIndex - 27);
                }
            }
            
            return result;
        }

        private PaddleOCRResult RecognizeWithPaddleOCR(Bitmap bitmap, Stopwatch sw)
        {
            DebugLog("🔄 PaddleOCR fallback kullanılıyor...");
            
            // Convert Bitmap to Mat for PaddleOCR
            using var imgSrc = BitmapConverter.ToMat(bitmap);

            // Perform OCR (optimized settings for license plates)
            var result = _ocrEngine!.Run(imgSrc);
            sw.Stop();

            DebugLog($"⏱️ PaddleOCR işlemi {sw.ElapsedMilliseconds}ms sürdü");
            DebugLog($"📝 Algılanan metin: '{result.Text}'");

            // Clean up the text (remove spaces, special characters for license plates)
            var cleanText = CleanPlateText(result.Text);
            
            if (cleanText != result.Text)
            {
                DebugLog($"🧹 Temizlenmiş metin: '{cleanText}'");
            }

            // Get confidence from regions if available
            float confidence = 0.95f;
            if (result.Regions != null && result.Regions.Length > 0)
            {
                confidence = (float)result.Regions.Average(r => r.Score);
            }

            var recognitionResult = new PaddleTextRecognition
            {
                Text = cleanText,
                BoundingBox = new RectangleF(0, 0, bitmap.Width, bitmap.Height),
                Confidence = confidence
            };

            DebugLog($"✅ PaddleOCR ile başarılı result oluşturuluyor");
            return new PaddleOCRResult
            {
                DetectionResult = new PaddleDetectionResult
                {
                    TextRegions = new List<PaddleTextRegion>(),
                    InferenceTimeMs = sw.ElapsedMilliseconds,
                    InputWidth = bitmap.Width,
                    InputHeight = bitmap.Height
                },
                RecognitionResults = new List<PaddleTextRecognition> { recognitionResult },
                InferenceTimeMs = sw.ElapsedMilliseconds
            };
        }

        public PaddleOCRResult RecognizeDirectlyFromMat(Mat imgSrc)
        {
            DebugLog("🚀 RecognizeDirectlyFromMat başlıyor...");
            DebugLog($"📷 Input Mat: {imgSrc.Width}x{imgSrc.Height}, Channels: {imgSrc.Channels()}");

            var sw = Stopwatch.StartNew();
            
            try
            {
                if (_ocrEngine == null)
                {
                    DebugLog("❌ OCR engine hazır değil!");
                    // Create a dummy bitmap for error result
                    using var dummyBitmap = new Bitmap(imgSrc.Width, imgSrc.Height);
                    return CreateErrorResult(dummyBitmap, "OCR engine hazır değil", sw.ElapsedMilliseconds);
                }

                // Perform OCR (exactly like your example, optimized for license plates)
                var result = _ocrEngine.Run(imgSrc);
                sw.Stop();

                DebugLog($"⏱️ PaddleOCR işlemi {sw.ElapsedMilliseconds}ms sürdü");
                DebugLog($"📝 Algılanan metin: '{result.Text}'");

                // Clean up the text (remove spaces, special characters for license plates)
                var cleanText = CleanPlateText(result.Text);
                
                if (cleanText != result.Text)
                {
                    DebugLog($"🧹 Temizlenmiş metin: '{cleanText}'");
                }

                // Get confidence from regions if available
                float confidence = 0.95f;
                if (result.Regions != null && result.Regions.Length > 0)
                {
                    confidence = (float)result.Regions.Average(r => r.Score);
                }

                var recognitionResult = new PaddleTextRecognition
                {
                    Text = cleanText,
                    BoundingBox = new RectangleF(0, 0, imgSrc.Width, imgSrc.Height),
                    Confidence = confidence
                };

                DebugLog($"✅ Başarılı result oluşturuluyor");
                return new PaddleOCRResult
                {
                    DetectionResult = new PaddleDetectionResult
                    {
                        TextRegions = new List<PaddleTextRegion>(),
                        InferenceTimeMs = sw.ElapsedMilliseconds,
                        InputWidth = imgSrc.Width,
                        InputHeight = imgSrc.Height
                    },
                    RecognitionResults = new List<PaddleTextRecognition> { recognitionResult },
                    InferenceTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                DebugLog($"❌ RecognizeDirectlyFromMat exception: {ex.Message}");
                DebugLog($"🔍 Stack trace: {ex.StackTrace}");
                
                // Create a dummy bitmap for error result
                using var dummyBitmap = new Bitmap(imgSrc.Width, imgSrc.Height);
                return CreateErrorResult(dummyBitmap, $"Recognition hatası: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        private PaddleOCRResult CreateErrorResult(Bitmap bitmap, string errorMessage, long elapsedMs)
        {
            return new PaddleOCRResult
            {
                DetectionResult = new PaddleDetectionResult
                {
                    TextRegions = new List<PaddleTextRegion>(),
                    InferenceTimeMs = elapsedMs,
                    InputWidth = bitmap.Width,
                    InputHeight = bitmap.Height,
                    ErrorMessage = errorMessage
                },
                RecognitionResults = new List<PaddleTextRecognition>(),
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
                DebugLog("🗑️ PaddleOCRDetector dispose ediliyor...");
                _ocrEngine?.Dispose();
                _onnxSession?.Dispose();
                _disposed = true;
                DebugLog("✅ PaddleOCRDetector dispose edildi");
            }
        }
        private Bitmap? CreateVisualizationFromMat(Mat imgSrc, PaddleOCRResult result)
        {
            try
            {
                var originalBitmap = BitmapConverter.ToBitmap(imgSrc);
                var visualized = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(visualized);

                if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                {
                    using var successPen = new Pen(Color.LimeGreen, 4);
                    g.DrawRectangle(successPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                }
                else
                {
                    using var errorPen = new Pen(Color.Red, 4);
                    g.DrawRectangle(errorPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                }

                originalBitmap.Dispose();
                return visualized;
            }
            catch (Exception ex)
            {
                //Log($"Görselleştirme hatası: {ex.Message}");
                return BitmapConverter.ToBitmap(imgSrc);
            }
        }

        private Bitmap? CreateVisualization(Bitmap originalBitmap, PaddleOCRResult result)
        {
            try
            {
                var visualized = new Bitmap(originalBitmap);
                using var g = Graphics.FromImage(visualized);

                if (result.RecognitionResults.Count > 0 && !string.IsNullOrEmpty(result.RecognitionResults[0].Text))
                {
                    using var successPen = new Pen(Color.LimeGreen, 4);
                    g.DrawRectangle(successPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                }
                else
                {
                    using var errorPen = new Pen(Color.Red, 4);
                    g.DrawRectangle(errorPen, 0, 0, originalBitmap.Width - 1, originalBitmap.Height - 1);
                }

                return visualized;
            }
            catch (Exception ex)
            {
                //Log($"Görselleştirme hatası: {ex.Message}");
                return new Bitmap(originalBitmap);
            }
        }
    }

    public class PaddleOCRResult
    {
        public PaddleDetectionResult DetectionResult { get; set; } = new();
        public List<PaddleTextRecognition> RecognitionResults { get; set; } = new();
        public long InferenceTimeMs { get; set; }
    }

    public class PaddleDetectionResult
    {
        public List<PaddleTextRegion> TextRegions { get; set; } = new();
        public long InferenceTimeMs { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaddleTextRegion
    {
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
        public PointF[] Contour { get; set; } = Array.Empty<PointF>();
    }

    public class PaddleTextRecognition
    {
        public string Text { get; set; } = string.Empty;
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
    }
}