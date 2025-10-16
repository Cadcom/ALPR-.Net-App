using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace ALPR.Detection
{
    public class DBNetTextDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _modelPath;
        private bool _disposed;
        private readonly ModelAnalysisResult _modelInfo;

        public DBNetTextDetector(string modelPath, bool useGpu = false)
        {
            _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
            
            if (!File.Exists(_modelPath))
                throw new FileNotFoundException($"Model dosyasý bulunamadý: {_modelPath}");

            // Model analizi yap
            _modelInfo = OnnxModelAnalyzer.AnalyzeModel(_modelPath);
            if (!_modelInfo.IsValid)
                throw new InvalidOperationException($"Model analizi baþarýsýz: {_modelInfo.ErrorMessage}");

            var sessionOptions = new SessionOptions();
            
            if (useGpu && ExecutionProviderHelper.IsGpuAvailable())
            {
                sessionOptions.AppendExecutionProvider_CUDA(0);
                sessionOptions.AppendExecutionProvider_DML(0);
            }
            else
            {
                sessionOptions.AppendExecutionProvider_CPU();
            }

            _session = new InferenceSession(_modelPath, sessionOptions);
        }

        public DBNetResult Detect(Bitmap image, float confidenceThreshold = 0.5f)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DBNetTextDetector));

            var result = new DBNetResult
            {
                ModelInfo = new DBNetModelInfo
                {
                    Name = "Improved DBNet",
                    Backbone = "resnet34",
                    InputSize = new int[] { 640, 640 },
                    Version = "2.0",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User = Environment.UserName
                },
                Prediction = new DBNetPrediction
                {
                    ImageShape = new int[] { image.Width, image.Height }
                }
            };

            try
            {
                // Doðru preprocessing ile input tensor oluþtur
                var inputTensor = PreprocessImageForDBNet(image);
                
                // Model çýkarýmý - dinamik input adý kullan
                var inputName = _modelInfo.InputNames.FirstOrDefault() ?? "input";
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
                };

                using var outputs = _session.Run(inputs);
                
                // DBNet'in output'unu al (genelde [1,1,H,W])
                var outputList = outputs.ToList();
                
                if (outputList.Count >= 1)
                {
                    // Son output çoðunlukla binary/probability haritasýdýr
                    var mainOutput = outputList[outputList.Count - 1];
                    
                    // Çýktý tensör verisi ve gerçek boyutlar
                    var tensor = mainOutput.AsTensor<float>();
                    var outData = tensor.ToArray();
                    var dims = tensor.Dimensions.ToArray() ?? Array.Empty<int>();

                    int outH = 320, outW = 320;
                    if (dims.Length >= 2)
                    {
                        outH = dims[^2];
                        outW = dims[^1];
                    }

                    ProcessDBNetOutputs(outData, result, confidenceThreshold, outW, outH);

                    // Görselleþtirme için sakla
                    result.BinaryMap = outData;
                    result.MapDimensions = new int[] { outH, outW };
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"DBNet model çýkarýmýnda hata: {ex.Message}", ex);
            }
        }

        private Tensor<float> PreprocessImageForDBNet(Bitmap image)
        {
            // DBNet için sabit boyutlar: 640x640
            const int targetHeight = 640;
            const int targetWidth = 640;
            const int channels = 3;

            // Bitmap'i OpenCV Mat'e çevir
            using var originalMat = BitmapToMat(image);
            
            // 640x640'a resize et
            using var resizedMat = new Mat();
            Cv2.Resize(originalMat, resizedMat, new OpenCvSharp.Size(targetWidth, targetHeight));
            
            // BGR'den RGB'ye çevir
            using var rgbMat = new Mat();
            Cv2.CvtColor(resizedMat, rgbMat, ColorConversionCodes.BGR2RGB);
            
            // Tensor oluþtur: [1, 3, 640, 640]
            var tensor = new DenseTensor<float>(new[] { 1, channels, targetHeight, targetWidth });
            
            // ImageNet normalizasyon deðerleri (DBNet analiz sonuçlarýndan)
            var mean = new float[] { 0.485f, 0.456f, 0.406f };
            var std = new float[] { 0.229f, 0.224f, 0.225f };
            
            // Preprocessing: HWC -> CHW ve normalizasyon
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    var pixel = rgbMat.At<Vec3b>(y, x);
                    
                    for (int c = 0; c < channels; c++)
                    {
                        // Önce [0,1] aralýðýna normalize et
                        float value = pixel[c] / 255.0f;
                        
                        // Sonra ImageNet standardizasyonu uygula
                        value = (value - mean[c]) / std[c];
                        
                        // NCHW formatýnda tensor'a yerleþtir
                        tensor[0, c, y, x] = value;
                    }
                }
            }
            
            return tensor;
        }

        private Mat BitmapToMat(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                var mat = Mat.FromPixelData(bitmap.Height, bitmap.Width, MatType.CV_8UC3, bitmapData.Scan0, bitmapData.Stride);
                return mat.Clone();
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        private void ProcessDBNetOutputs(float[] output, DBNetResult result, float confidenceThreshold, int outputWidth, int outputHeight)
        {
            // Çýktý boyutlarýný parametreden kullan
            int expectedSize = outputHeight * outputWidth;

            // Output array boyutunu kontrol et
            int actualSize = Math.Min(output.Length, expectedSize);
            
            // NaN ve geçersiz deðerleri filtrele
            var validOutput = output.Take(actualSize)
                .Select(v => float.IsNaN(v) || float.IsInfinity(v) ? 0f : Math.Max(0f, Math.Min(1f, v)))
                .ToArray();

            // Ýstatistikleri hesapla
            var positiveValues = validOutput.Where(p => p > 0.001f).ToArray();
            
            if (positiveValues.Length > 0)
            {
                result.Prediction.ConfidenceStats = new DBNetConfidenceStats
                {
                    MeanProbability = positiveValues.Average(),
                    MaxProbability = positiveValues.Max(),
                    MinProbability = positiveValues.Min()
                };

                // Metin kapsamýný hesapla
                var textPixels = validOutput.Count(p => p > confidenceThreshold);
                result.Prediction.TextCoveragePercent = (textPixels * 100.0) / actualSize;

                // Kontur analizi
                result.Prediction.NumContours = EstimateTextContours(validOutput, outputWidth, outputHeight, confidenceThreshold);
            }
            else
            {
                // Hiç pozitif deðer yoksa
                result.Prediction.ConfidenceStats = new DBNetConfidenceStats
                {
                    MeanProbability = 0.0,
                    MaxProbability = validOutput.Length > 0 ? validOutput.Max() : 0.0,
                    MinProbability = validOutput.Length > 0 ? validOutput.Min() : 0.0
                };

                result.Prediction.TextCoveragePercent = 0.0;
                result.Prediction.NumContours = 0;
            }

            // Output map durumlarý
            result.OutputMaps = new DBNetOutputMaps
            {
                ProbabilityMapAvailable = true,
                ThresholdMapAvailable = _modelInfo.Outputs.Count > 1,
                BinaryMapAvailable = _modelInfo.Outputs.Count > 2,
                FinalMaskAvailable = true
            };
        }

        private int EstimateTextContours(float[] binaryMap, int width, int height, float threshold)
        {
            // Connected components analizi ile text bölgelerini say
            var visited = new bool[binaryMap.Length];
            int contourCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (idx < binaryMap.Length && 
                        binaryMap[idx] > threshold && 
                        !visited[idx])
                    {
                        contourCount++;
                        FloodFillComponent(binaryMap, visited, x, y, width, height, threshold);
                    }
                }
            }

            return contourCount;
        }

        private void FloodFillComponent(float[] map, bool[] visited, int startX, int startY, int width, int height, float threshold)
        {
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                int idx = y * width + x;
                if (idx >= map.Length || visited[idx] || map[idx] <= threshold)
                    continue;

                visited[idx] = true;

                // 4-connectivity flood fill
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Data Classes

    public class DBNetResult
    {
        public DBNetModelInfo ModelInfo { get; set; } = new();
        public DBNetPrediction Prediction { get; set; } = new();
        public DBNetOutputMaps OutputMaps { get; set; } = new();
        
        // Visualization için eklenen alanlar
        public float[]? BinaryMap { get; set; }
        public int[] MapDimensions { get; set; } = Array.Empty<int>();
    }

    public class DBNetModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Backbone { get; set; } = string.Empty;
        public int[] InputSize { get; set; } = Array.Empty<int>();
        public string Version { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }

    public class DBNetPrediction
    {
        public int[] ImageShape { get; set; } = Array.Empty<int>();
        public double TextCoveragePercent { get; set; }
        public int NumContours { get; set; }
        public DBNetConfidenceStats ConfidenceStats { get; set; } = new();
    }

    public class DBNetConfidenceStats
    {
        public double MeanProbability { get; set; }
        public double MaxProbability { get; set; }
        public double MinProbability { get; set; }
    }

    public class DBNetOutputMaps
    {
        public bool ProbabilityMapAvailable { get; set; }
        public bool ThresholdMapAvailable { get; set; }
        public bool BinaryMapAvailable { get; set; }
        public bool FinalMaskAvailable { get; set; }
    }

    #endregion
}
