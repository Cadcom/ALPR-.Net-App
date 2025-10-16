using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace ALPR.Detection
{
    public static class ModelInspector
    {
        /// <summary>
        /// ONNX modelinin input/output bilgilerini analiz eder
        /// </summary>
        public static string InspectModel(string modelPath)
        {
            if (!File.Exists(modelPath))
                return $"? Model dosyasý bulunamadý: {modelPath}";

            try
            {
                using var session = new InferenceSession(modelPath);
                var sb = new StringBuilder();
                
                sb.AppendLine($"?? Model Analizi: {Path.GetFileName(modelPath)}");
                sb.AppendLine($"?? Dosya yolu: {modelPath}");
                sb.AppendLine($"?? Dosya boyutu: {new FileInfo(modelPath).Length / (1024 * 1024):F1} MB");
                sb.AppendLine($"?? Model tipi: {DetectModelType(modelPath)}");
                sb.AppendLine();
                
                // Input bilgileri
                sb.AppendLine("?? INPUT BÝLGÝLERÝ:");
                if (session.InputMetadata.Count > 0)
                {
                    foreach (var input in session.InputMetadata)
                    {
                        sb.AppendLine($"  ?? Ad: '{input.Key}'");
                        sb.AppendLine($"     Tip: {input.Value.ElementType}");
                        sb.AppendLine($"     Boyutlar: [{string.Join(", ", input.Value.Dimensions)}]");
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("  ? Input metadata bulunamadý");
                }
                
                // Output bilgileri
                sb.AppendLine("?? OUTPUT BÝLGÝLERÝ:");
                if (session.OutputMetadata.Count > 0)
                {
                    foreach (var output in session.OutputMetadata)
                    {
                        sb.AppendLine($"  ?? Ad: '{output.Key}'");
                        sb.AppendLine($"     Tip: {output.Value.ElementType}");
                        sb.AppendLine($"     Boyutlar: [{string.Join(", ", output.Value.Dimensions)}]");
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("  ? Output metadata bulunamadý");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"? Model analiz hatasý: {ex.Message}";
            }
        }

        /// <summary>
        /// Model tipini analiz eder (Detection/Recognition/Unknown)
        /// </summary>
        public static string DetectModelType(string modelPath)
        {
            if (!File.Exists(modelPath))
                return "Unknown";

            try
            {
                using var session = new InferenceSession(modelPath);
                var inputs = session.InputMetadata;
                var outputs = session.OutputMetadata;

                var firstInput = inputs.Values.FirstOrDefault();
                var firstOutput = outputs.Values.FirstOrDefault();

                if (firstInput?.Dimensions != null && firstInput.Dimensions.Length == 4)
                {
                    var dims = firstInput.Dimensions;
                    var height = dims[dims.Length - 2];
                    var width = dims[dims.Length - 1];
                    
                    // Detection model: Genellikle sabit boyut (örn. 640x640)
                    if (height > 0 && width > 0 && height == width && height >= 320)
                    {
                        return "?? Detection Model (Sabit boyut)";
                    }
                    
                    // Recognition model: Genellikle sabit yükseklik, deðiþken geniþlik
                    if (height > 0 && height <= 64 && (width <= 0 || width == -1))
                    {
                        return "?? Recognition Model (Deðiþken geniþlik)";
                    }
                    
                    // Recognition model: Sabit boyut ama küçük
                    if (height > 0 && width > 0 && height <= 64)
                    {
                        return "?? Recognition Model (Sabit küçük boyut)";
                    }
                }

                // Output boyutuna bak
                if (firstOutput?.Dimensions != null)
                {
                    var outputDims = firstOutput.Dimensions;
                    var outputSize = outputDims.Where(d => d > 0).Aggregate(1L, (a, b) => a * b);
                    
                    // Büyük output genellikle detection
                    if (outputSize > 100000)
                        return "?? Detection Model (Büyük output)";
                    
                    // Text sequence output (recognition)
                    if (outputDims.Length >= 2 && outputDims[outputDims.Length - 1] > 30 && outputDims[outputDims.Length - 1] < 200)
                        return "?? Recognition Model (Text sequence)";
                }

                return $"? Belirsiz (Input: {string.Join("x", firstInput?.Dimensions ?? new int[0])})";
            }
            catch (Exception ex)
            {
                return $"? Analiz Hatasý: {ex.Message}";
            }
        }

        /// <summary>
        /// Model dosya adýndan da tip tahmini yapar
        /// </summary>
        public static string GuessModelTypeFromName(string modelPath)
        {
            var fileName = Path.GetFileName(modelPath).ToLower();
            
            if (fileName.Contains("det") || fileName.Contains("detection"))
                return "?? Detection (Dosya adýndan)";
            else if (fileName.Contains("rec") || fileName.Contains("recognition"))
                return "?? Recognition (Dosya adýndan)";
            else
                return "? Belirsiz";
        }

        /// <summary>
        /// Sadece recognition model analizi yapar
        /// </summary>
        public static string InspectRecognitionModel(string? recModelPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PADDLE OCR RECOGNÝTÝON MODEL ANALÝZÝ ===");
            sb.AppendLine();

            if (string.IsNullOrEmpty(recModelPath) || !File.Exists(recModelPath))
            {
                sb.AppendLine("? Recognition model bulunamadý!");
                sb.AppendLine("Lütfen geçerli bir paddle_rec_model.onnx dosyasý seçin.");
                return sb.ToString();
            }

            try
            {
                using var session = new InferenceSession(recModelPath);
                
                sb.AppendLine($"?? Model: {Path.GetFileName(recModelPath)}");
                sb.AppendLine($"?? Boyut: {new FileInfo(recModelPath).Length / (1024 * 1024):F1} MB");
                
                var fileNameGuess = GuessModelTypeFromName(recModelPath);
                var dimensionGuess = DetectModelType(recModelPath);
                
                sb.AppendLine($"?? Dosya adý analizi: {fileNameGuess}");
                sb.AppendLine($"?? Boyut analizi: {dimensionGuess}");
                sb.AppendLine();

                // Input analizi
                sb.AppendLine("?? INPUT BÝLGÝLERÝ:");
                var inputs = session.InputMetadata;
                foreach (var input in inputs)
                {
                    sb.AppendLine($"  ?? Ad: '{input.Key}'");
                    sb.AppendLine($"     Tip: {input.Value.ElementType}");
                    sb.AppendLine($"     Boyutlar: [{string.Join(", ", input.Value.Dimensions)}]");
                    
                    // Boyut analizi
                    if (input.Value.Dimensions?.Length == 4)
                    {
                        var dims = input.Value.Dimensions;
                        var h = dims[dims.Length - 2];
                        var w = dims[dims.Length - 1];
                        
                        if (h > 0 && w > 0 && h == w && h >= 320)
                        {
                            sb.AppendLine($"     ?? UYARI: Bu bir DETECTION modeli gibi görünüyor!");
                            sb.AppendLine($"     ?? Recognition modeller genellikle küçük yükseklik (32-64px) kullanýr.");
                        }
                        else if (h > 0 && h <= 64)
                        {
                            sb.AppendLine($"     ? Recognition model formatýna uygun görünüyor.");
                        }
                    }
                    sb.AppendLine();
                }

                // Output analizi
                sb.AppendLine("?? OUTPUT BÝLGÝLERÝ:");
                var outputs = session.OutputMetadata;
                foreach (var output in outputs)
                {
                    sb.AppendLine($"  ?? Ad: '{output.Key}'");
                    sb.AppendLine($"     Tip: {output.Value.ElementType}");
                    sb.AppendLine($"     Boyutlar: [{string.Join(", ", output.Value.Dimensions)}]");
                    
                    // Output analizi
                    if (output.Value.Dimensions?.Length >= 2)
                    {
                        var dims = output.Value.Dimensions;
                        var lastDim = dims[dims.Length - 1];
                        
                        if (lastDim > 30 && lastDim < 200)
                        {
                            sb.AppendLine($"     ? Text sequence output ({lastDim} karakter sýnýfý)");
                        }
                        else if (lastDim > 1000)
                        {
                            sb.AppendLine($"     ?? Çok büyük output - detection model olabilir");
                        }
                    }
                    sb.AppendLine();
                }

                // Sonuç
                sb.AppendLine("?? SONUÇ:");
                if (dimensionGuess.Contains("Detection"))
                {
                    sb.AppendLine("? Bu model bir DETECTION MODELÝ!");
                    sb.AppendLine("?? Recognition modeli için farklý bir .onnx dosyasý seçin.");
                    sb.AppendLine("?? Recognition modeller genellikle 'rec' veya 'recognition' içerir.");
                    sb.AppendLine("?? Recognition input boyutu: ~[1, 3, 32, Variable]");
                    sb.AppendLine("?? Detection input boyutu: ~[1, 3, 640, 640]");
                }
                else if (dimensionGuess.Contains("Recognition"))
                {
                    sb.AppendLine("? Bu model bir RECOGNITION MODELÝ!");
                    sb.AppendLine("?? Plaka okuma için kullanýlabilir.");
                }
                else
                {
                    sb.AppendLine("? Model tipi belirsiz.");
                    sb.AppendLine("?? Input boyutlarýný kontrol edin:");
                    sb.AppendLine("   - Detection: ~640x640");
                    sb.AppendLine("   - Recognition: ~32xVariable");
                }

            }
            catch (Exception ex)
            {
                sb.AppendLine($"? Model analiz hatasý: {ex.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// PaddleOCR formatýndaki modelin input adýný döndürür
        /// </summary>
        public static string GetInputName(string? modelPath)
        {
            if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                return "input"; // Fallback
                
            try
            {
                using var session = new InferenceSession(modelPath);
                return session.InputMetadata.Keys.FirstOrDefault() ?? "input";
            }
            catch
            {
                return "input"; // Fallback
            }
        }

        /// <summary>
        /// Model için önerilen input boyutlarýný döndürür
        /// </summary>
        public static (int height, int width) GetRecommendedInputSize(string? modelPath)
        {
            if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                return (32, 320); // Recognition default
                
            try
            {
                using var session = new InferenceSession(modelPath);
                var firstInput = session.InputMetadata.Values.FirstOrDefault();
                
                if (firstInput?.Dimensions != null && firstInput.Dimensions.Length >= 3)
                {
                    // NCHW formatý varsayýmý: [batch, channel, height, width]
                    var dims = firstInput.Dimensions;
                    var height = dims[dims.Length - 2];
                    var width = dims[dims.Length - 1];
                    
                    // Dinamik boyutlar (-1) için varsayýlan deðerler
                    if (height <= 0) height = 32; // Recognition default
                    if (width <= 0) width = 320; // Recognition default
                    
                    return (height, width);
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return (32, 320); // Recognition default
        }
    }
}