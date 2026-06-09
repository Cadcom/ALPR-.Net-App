using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX modeli kullanarak plaka tespiti yapan sınıf.
    /// Thread-safe değildir, her thread için ayrı instance oluşturulmalıdır.
    /// GPU desteği: CUDA (NVIDIA) ve DirectML (Windows)
    /// </summary>
    public sealed class LicensePlateDetector : IDisposable
    {
        private const int ModelSize = 640;
        private const string DefaultClassName = "Licence_Plate";

        private readonly InferenceSession _session;
        private readonly string _inputName;
        private bool _disposed;

        /// <summary>
        /// Plaka tespit modelini yükler. Otomatik olarak GPU kullanır (varsa).
        /// </summary>
        /// <param name="modelPath">ONNX model dosya yolu</param>
        /// <param name="useGpu">GPU kullan (true: otomatik tespit, false: sadece CPU)</param>
        public LicensePlateDetector(string modelPath, bool useGpu = true)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentNullException(nameof(modelPath));

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model dosyası bulunamadı: {modelPath}", modelPath);

            // GPU/CPU SessionOptions oluştur
            var sessionOptions = ExecutionProviderHelper.CreateOptimizedSessionOptions(preferGpu: useGpu);

            _session = new InferenceSession(modelPath, sessionOptions);
            _inputName = _session.InputMetadata.Keys.First();
        }

        /// <summary>
        /// Görüntüde plaka tespiti yapar.
        /// </summary>
        /// <param name="originalImage">İşlenecek görüntü</param>
        /// <param name="confidenceThreshold">Minimum güven eşiği (0-1 arası)</param>
        /// <param name="enableNms">NMS (Non-Maximum Suppression) aktif mi</param>
        /// <param name="nmsThreshold">NMS IoU eşiği</param>
        /// <returns>Tespit edilen plakalar ve geçen süre (ms)</returns>
        public DetectionResult Detect(
            Bitmap originalImage,
            float confidenceThreshold,
            bool enableNms,
            float nmsThreshold)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (originalImage == null)
                throw new ArgumentNullException(nameof(originalImage));

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var inputTensor = Preprocess(originalImage);
            var detections = RunInference(inputTensor, originalImage, confidenceThreshold);

            if (enableNms && detections.Count > 1)
            {
                detections = ApplyNms(detections, nmsThreshold);
            }

            sw.Stop();
            return new DetectionResult(detections, sw.ElapsedMilliseconds);
        }

        private List<LicensePlateDetection> RunInference(
            DenseTensor<float> inputTensor,
            Bitmap originalImage,
            float confidenceThreshold)
        {
            var inputValue = NamedOnnxValue.CreateFromTensor(_inputName, inputTensor);
            var inputs = new List<NamedOnnxValue> { inputValue };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            return ProcessOutput(output, originalImage.Width, originalImage.Height, confidenceThreshold);
        }

        private static DenseTensor<float> Preprocess(Bitmap bmp)
        {
            using var mat = BitmapConverter.ToMat(bmp);
            using var resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(ModelSize, ModelSize), 0, 0, InterpolationFlags.Linear);

            using var blob = CvDnn.BlobFromImage(
                resized,
                scaleFactor: 1.0 / 255.0,
                size: new OpenCvSharp.Size(ModelSize, ModelSize),
                mean: new Scalar(),
                swapRB: true,
                crop: false);

            var length = checked((int)blob.Total());
            var data = new float[length];
            Marshal.Copy(blob.Data, data, 0, length);

            return new DenseTensor<float>(data, new[] { 1, 3, ModelSize, ModelSize });
        }

        private static List<LicensePlateDetection> ProcessOutput(
            Tensor<float> output,
            int originalWidth,
            int originalHeight,
            float confidenceThreshold)
        {
            var detections = new List<LicensePlateDetection>();
            int count = output.Dimensions[2];

            float scaleX = originalWidth / (float)ModelSize;
            float scaleY = originalHeight / (float)ModelSize;

            for (int i = 0; i < count; i++)
            {
                float confidence = output[0, 4, i];
                if (confidence < confidenceThreshold)
                    continue;

                var detection = CreateDetection(output, i, scaleX, scaleY, confidence);
                detections.Add(detection);
            }

            return detections;
        }

        private static LicensePlateDetection CreateDetection(
            Tensor<float> output,
            int index,
            float scaleX,
            float scaleY,
            float confidence)
        {
            float cx = output[0, 0, index];
            float cy = output[0, 1, index];
            float w = output[0, 2, index];
            float h = output[0, 3, index];

            // Merkez koordinattan köşe koordinatına dönüştür ve ölçekle
            float x = (cx - w / 2f) * scaleX;
            float y = (cy - h / 2f) * scaleY;
            w *= scaleX;
            h *= scaleY;

            return new LicensePlateDetection
            {
                X = (int)Math.Round(x),
                Y = (int)Math.Round(y),
                Width = (int)Math.Round(w),
                Height = (int)Math.Round(h),
                Confidence = confidence,
                Class = DefaultClassName,
                ClassId = 0
            };
        }

        private static List<LicensePlateDetection> ApplyNms(
            List<LicensePlateDetection> detections,
            float iouThreshold)
        {
            // Güvene göre azalan sırada sırala
            var ordered = detections.OrderByDescending(d => d.Confidence).ToList();
            var kept = new List<LicensePlateDetection>(detections.Count);

            while (ordered.Count > 0)
            {
                var current = ordered[0];
                kept.Add(current);
                ordered.RemoveAt(0);

                // Mevcut tespit ile örtüşen tespitleri kaldır
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

        /// <summary>
        /// İki tespit arasındaki IoU (Intersection over Union) değerini hesaplar.
        /// </summary>
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

        public void Dispose()
        {
            if (_disposed) return;

            _session?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Plaka tespiti sonucunu temsil eder.
    /// </summary>
    public sealed record DetectionResult(
        List<LicensePlateDetection> Detections,
        long ElapsedMs);

    /// <summary>
    /// Tek bir plaka tespitini temsil eder.
    /// </summary>
    public sealed class LicensePlateDetection
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Confidence { get; set; }
        public string Class { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string CountryName { get; set; } = string.Empty;

        public Rectangle GetRectangle() => new(X, Y, Width, Height);
    }
}
