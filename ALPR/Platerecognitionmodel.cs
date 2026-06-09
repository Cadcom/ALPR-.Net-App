using ALPR.Detection;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.Json;

namespace YoloOnnxRunner
{
    public class DetectionResult
    {
        public int ClassId { get; set; }
        public float Confidence { get; set; }
        public RectangleF Box { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
    }

    // --- Config Classes ---
    public class ModelConfigRoot
    {
        public ModelConfigData model_config { get; set; } = new();
        public List<ClassInfo>? classes { get; set; }
    }

    public class ModelConfigData
    {
        public int nc { get; set; } = 21; // Default
        public InputSizeConfig input_size { get; set; } = new();
        public OutputNamesConfig output_names { get; set; } = new();
    }

    public class InputSizeConfig
    {
        public int width { get; set; } = 640;
        public int height { get; set; } = 640;
    }

    public class OutputNamesConfig
    {
        public string ocr { get; set; } = "";
        public string country { get; set; } = "";
    }

    public class ClassInfo
    {
        public int id { get; set; }
        public string code { get; set; } = "";
        public string label { get; set; } = "";
        public string name { get; set; } = "";
        public bool is_country { get; set; }
    }

    /// <summary>
    /// YOLO ONNX model runner. IDisposable — çağıran Dispose etmeyi unutmasın,
    /// yoksa native session belleği (RAM/VRAM) serbest bırakılmaz.
    /// </summary>
    public class PlateRecognitionModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly List<string>? _classLabels;
        private readonly Dictionary<int, string> _classNames = new();

        private int _numClasses = 21;
        private int _inputWidth = 640;
        private int _inputHeight = 640;
        
        private const float ConfThreshold = 0.11f;
        private const float IouThreshold = 0.45f;
        private const string RegistryFileName = "class_registry.json";

        public List<string>? ClassLabels => _classLabels;

        public PlateRecognitionModel(string modelPath, bool useGpu = true)
        {
            // ExecutionProviderHelper kullanarak optimize edilmiş seçenekleri al (CUDA veya DirectML dener)
            var sessionOptions = ALPR.Detection.ExecutionProviderHelper.CreateOptimizedSessionOptions(useGpu);

            _session = new InferenceSession(modelPath, sessionOptions);
            _inputName = _session.InputMetadata.Keys.First();
            
            // 1. Önce konfigürasyon dosyasını yükle (Varsayılan değerler buradan gelir)
            LoadConfig(modelPath);

            // 2. Modelin kendi metadata'sını kontrol et (Metadata konfigürasyondan daha üstündür)
            var inputMeta = _session.InputMetadata[_inputName];
            if (inputMeta.Dimensions.Length == 4)
            {
                // Index 2: Height, Index 3: Width (NCHW formatı)
                int metaH = inputMeta.Dimensions[2];
                int metaW = inputMeta.Dimensions[3];

                if (metaH > 0 && metaH != _inputHeight)
                {
                    ALPR.Detection.ExecutionProviderHelper.Logger?.Invoke($"?? Bilgi: Model metadata'sından yükseklik güncellendi: {metaH} (Config: {_inputHeight})");
                    _inputHeight = metaH;
                }
                if (metaW > 0 && metaW != _inputWidth)
                {
                    ALPR.Detection.ExecutionProviderHelper.Logger?.Invoke($"?? Bilgi: Model metadata'sından genişlik güncellendi: {metaW} (Config: {_inputWidth})");
                    _inputWidth = metaW;
                }
            }
            
            _classLabels = TryLoadClassLabels(modelPath);
        }

        private void LoadConfig(string modelPath)
        {
            try
            {
                var modelDir = Path.GetDirectoryName(modelPath) ?? Directory.GetCurrentDirectory();
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // Daha agresif arama yolları (Üst dizinlere doğru çıkarak Proje Root'unu bulmaya çalışır)
                var searchPaths = new List<string>();
                searchPaths.Add(Path.Combine(modelDir, RegistryFileName));
                searchPaths.Add(Path.Combine(appDir, RegistryFileName));
                
                var current = new DirectoryInfo(appDir);
                for (int i = 0; i < 4 && current.Parent != null; i++)
                {
                    current = current.Parent;
                    searchPaths.Add(Path.Combine(current.FullName, RegistryFileName));
                }

                string? configPath = searchPaths.FirstOrDefault(File.Exists);
                
                if (configPath != null)
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var config = JsonSerializer.Deserialize<ModelConfigRoot>(json, options);
                    if (config != null)
                    {
                        if (config.model_config != null)
                        {
                            _numClasses = config.model_config.nc;
                            if (config.model_config.input_size.width > 0)
                                _inputWidth = config.model_config.input_size.width;
                            if (config.model_config.input_size.height > 0)
                                _inputHeight = config.model_config.input_size.height;
                        }

                        if (config.classes != null)
                        {
                            foreach (var cls in config.classes)
                            {
                                if (!string.IsNullOrEmpty(cls.name))
                                    _classNames[cls.id] = cls.name;
                            }
                            ALPR.Detection.ExecutionProviderHelper.Logger?.Invoke($"?? Konfigürasyon bulundu: {configPath} ({_classNames.Count} sınıf)");
                        }
                    }
                }
                else
                {
                    ALPR.Detection.ExecutionProviderHelper.Logger?.Invoke($"?? HATA: {RegistryFileName} hiçbir yerde bulunamadı! Lütfen dosyayı 'models' klasörüne kopyalayın.");
                }
            }
            catch (Exception ex)
            {
                ALPR.Detection.ExecutionProviderHelper.Logger?.Invoke($"?? Konfigürasyon hatası: {ex.Message}");
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public List<DetectionResult> Predict(Bitmap image)
        {
            var swPre = Stopwatch.StartNew();
            float scale = Math.Min((float)_inputWidth / image.Width,
                                   (float)_inputHeight / image.Height);
            float padX = (_inputWidth - image.Width * scale) / 2f;
            float padY = (_inputHeight - image.Height * scale) / 2f;

            var input = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor(_inputName,
            BuildLetterboxTensor(image, scale, padX, padY))
    };
            swPre.Stop();

            var swInfer = Stopwatch.StartNew();
            using var outputs = _session.Run(input);
            swInfer.Stop();

            var swPost = Stopwatch.StartNew();
            var rawTensor = outputs.First().AsTensor<float>();
            var result = ApplyNMS(DecodeOutput(rawTensor, scale, padX, padY, _classLabels), IouThreshold);
            swPost.Stop();

            ExecutionProviderHelper.Logger?.Invoke(
                $"⏱ Pre: {swPre.ElapsedMilliseconds}ms | " +
                $"Infer: {swInfer.ElapsedMilliseconds}ms | " +
                $"Post: {swPost.ElapsedMilliseconds}ms");

            // Bunu da ekle — Output penceresinde kesin görünür
            System.Diagnostics.Debug.WriteLine(
                $"⏱ Pre: {swPre.ElapsedMilliseconds}ms | " +
                $"Infer: {swInfer.ElapsedMilliseconds}ms | " +
                $"Post: {swPost.ElapsedMilliseconds}ms");

            return result;
        }

        public void Dispose() => _session?.Dispose();

        // ── Preprocessing ────────────────────────────────────────────────────

        private DenseTensor<float> BuildLetterboxTensor(
    Bitmap image, float scale, float padX, float padY)
        {
            using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(image);
            using var rgb = new Mat();
            Cv2.CvtColor(src, rgb, ColorConversionCodes.BGR2RGB);

            using var canvas = new Mat(
                new OpenCvSharp.Size(_inputWidth, _inputHeight),
                MatType.CV_8UC3,
                new Scalar(114, 114, 114));

            using var resized = new Mat();
            Cv2.Resize(rgb, resized,
                new OpenCvSharp.Size((int)(image.Width * scale), (int)(image.Height * scale)),
                interpolation: InterpolationFlags.Linear);

            var roi = canvas[new Rect((int)padX, (int)padY, resized.Width, resized.Height)];
            resized.CopyTo(roi);

            // Float Mat'e çevir — normalize et
            using var floatCanvas = new Mat();
            canvas.ConvertTo(floatCanvas, MatType.CV_32FC3, 1.0 / 255.0);

            var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });
            var tensorSpan = tensor.Buffer.Span; // bounds check yok

            int planeSize = _inputHeight * _inputWidth;

            unsafe
            {
                float* ptr = (float*)floatCanvas.DataPointer;

                for (int y = 0; y < _inputHeight; y++)
                {
                    int rowOffset = y * _inputWidth;
                    for (int x = 0; x < _inputWidth; x++)
                    {
                        int srcIdx = (rowOffset + x) * 3;
                        int dstX = rowOffset + x;
                        tensorSpan[dstX] = ptr[srcIdx];                      // R
                        tensorSpan[planeSize + dstX] = ptr[srcIdx + 1];      // G
                        tensorSpan[planeSize * 2 + dstX] = ptr[srcIdx + 2];  // B
                    }
                }
            }

            return tensor;
        }

        // ── Post-processing ──────────────────────────────────────────────────

        private List<DetectionResult> DecodeOutput(
            Tensor<float> output, float scale, float padX, float padY, List<string>? classLabels)
        {
            var results = new List<DetectionResult>();

            int dim1 = output.Dimensions.Length > 1 ? output.Dimensions[1] : 0;
            int dim2 = output.Dimensions.Length > 2 ? output.Dimensions[2] : 0;

            // End-2-End models emit [1, N, 6] (xmin,ymin,xmax,ymax,conf,classId)
            bool isEnd2End = dim2 == 6 || dim1 == 6;
            bool isTransposed = dim1 > dim2;

            int anchors = isTransposed ? dim1 : dim2;
            int numFeatures = isTransposed ? dim2 : dim1;

            for (int i = 0; i < anchors; i++)
            {
                if (isEnd2End)
                    TryAddEnd2End(output, i, isTransposed, scale, padX, padY, results, classLabels);
                else
                    TryAddClassic(output, i, isTransposed, numFeatures, scale, padX, padY, results, classLabels);
            }

            return results;
        }

        private void TryAddEnd2End(
            Tensor<float> o, int i, bool t,
            float scale, float padX, float padY,
            List<DetectionResult> results,
            List<string>? classLabels)
        {
            float conf = t ? o[0, i, 4] : o[0, 4, i];
            if (conf < ConfThreshold) return;

            float xMin = ((t ? o[0, i, 0] : o[0, 0, i]) - padX) / scale;
            float yMin = ((t ? o[0, i, 1] : o[0, 1, i]) - padY) / scale;
            float xMax = ((t ? o[0, i, 2] : o[0, 2, i]) - padX) / scale;
            float yMax = ((t ? o[0, i, 3] : o[0, 3, i]) - padY) / scale;
            int cls = (int)(t ? o[0, i, 5] : o[0, 5, i]);

            results.Add(new DetectionResult
            {
                ClassId = cls,
                Confidence = conf,
                Box = RectangleF.FromLTRB(xMin, yMin, xMax, yMax),
                CountryCode = ResolveCountryCode(cls, classLabels),
                CountryName = ResolveCountryName(cls)
            });
        }

        private void TryAddClassic(
            Tensor<float> o, int i, bool t, int numFeatures,
            float scale, float padX, float padY,
            List<DetectionResult> results,
            List<string>? classLabels)
        {
            float maxConf = 0f;
            int classId = -1;

            for (int c = 0; c < _numClasses && (4 + c) < numFeatures; c++)
            {
                float conf = t ? o[0, i, 4 + c] : o[0, 4 + c, i];
                if (conf > maxConf) { maxConf = conf; classId = c; }
            }

            if (maxConf < ConfThreshold) return;

            float cx = t ? o[0, i, 0] : o[0, 0, i];
            float cy = t ? o[0, i, 1] : o[0, 1, i];
            float w = t ? o[0, i, 2] : o[0, 2, i];
            float h = t ? o[0, i, 3] : o[0, 3, i];

            float xMin = (cx - w / 2f - padX) / scale;
            float yMin = (cy - h / 2f - padY) / scale;

            results.Add(new DetectionResult
            {
                ClassId = classId,
                Confidence = maxConf,
                Box = new RectangleF(xMin, yMin, w / scale, h / scale),
                CountryCode = ResolveCountryCode(classId, classLabels),
                CountryName = ResolveCountryName(classId)
            });
        }

        private static string ResolveCountryCode(int classId, List<string>? labels)
        {
            if (classId < 0) return "UNKNOWN";
            if (classId == 0) return "PLATE";

            if (labels != null && classId >= 0 && classId < labels.Count)
            {
                var raw = labels[classId];
                if (string.IsNullOrWhiteSpace(raw)) return $"C{classId}";
                if (raw.Equals("lp_generic", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("plate", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("generic", StringComparison.OrdinalIgnoreCase))
                    return "GENERIC";
                return raw.Replace("lp_", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
            }

            return $"C{classId}";
        }

        private string ResolveCountryName(int classId)
        {
            if (_classNames.TryGetValue(classId, out var name))
                return name;
            return $"Bilinmiyor (ID:{classId})";
        }

        private List<string>? TryLoadClassLabels(string modelPath)
        {
            try
            {
                var modelDir = Path.GetDirectoryName(modelPath) ?? Directory.GetCurrentDirectory();
                var configPath = Path.Combine(modelDir, RegistryFileName);
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("classes", out var classesEl) &&
                        classesEl.ValueKind == JsonValueKind.Array)
                    {
                        var labels = new List<string>();
                        foreach (var el in classesEl.EnumerateArray())
                        {
                            if (el.ValueKind == JsonValueKind.String)
                            {
                                labels.Add(el.GetString() ?? "");
                            }
                            else if (el.ValueKind == JsonValueKind.Object)
                            {
                                // "label" veya "code" alanını al
                                if (el.TryGetProperty("label", out var labelProp))
                                    labels.Add(labelProp.GetString() ?? "");
                                else if (el.TryGetProperty("code", out var codeProp))
                                    labels.Add(codeProp.GetString() ?? "");
                                else
                                    labels.Add("");
                            }
                        }
                        return labels;
                    }
                }
            }
            catch
            {
                // Registry opsiyonel
            }

            return null;
        }

        private static List<DetectionResult> ApplyNMS(
            List<DetectionResult> detections, float iouThreshold)
        {
            // Sort by confidence descending
            detections.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

            var active = new bool[detections.Count];
            for (int i = 0; i < active.Length; i++) active[i] = true;

            var final = new List<DetectionResult>();

            for (int i = 0; i < detections.Count; i++)
            {
                if (!active[i]) continue;
                final.Add(detections[i]);

                for (int j = i + 1; j < detections.Count; j++)
                {
                    if (!active[j]) continue;
                    // Only suppress same-class overlaps (different classes may legitimately overlap)
                    if (detections[i].ClassId == detections[j].ClassId &&
                        IoU(detections[i].Box, detections[j].Box) > iouThreshold)
                    {
                        active[j] = false;
                    }
                }
            }

            return final;
        }

        private static float IoU(RectangleF a, RectangleF b)
        {
            float x1 = Math.Max(a.Left, b.Left);
            float y1 = Math.Max(a.Top, b.Top);
            float x2 = Math.Min(a.Right, b.Right);
            float y2 = Math.Min(a.Bottom, b.Bottom);

            float inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            float union = a.Width * a.Height + b.Width * b.Height - inter;
            return union <= 0 ? 0f : inter / union;
        }
    }
}