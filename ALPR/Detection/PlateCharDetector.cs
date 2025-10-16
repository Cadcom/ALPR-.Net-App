using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX modeli kullanarak plaka karakteri tespiti yapan sýnýf.
    /// Thread-safe deðildir, her thread için ayrý instance oluþturulmalýdýr.
    /// GPU desteði: CUDA (NVIDIA) ve DirectML (Windows)
    /// </summary>
    public sealed class PlateCharDetector : IDisposable
    {
        private const int DefaultModelSize = 640;
        private const int MinClassCount = 36; // 0-9 ve A-Z

        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly int _inputHeight;
        private readonly int _inputWidth;
        private readonly bool _swapRB;
        private bool _disposed;

        // Karakter sýnýflarý: 0-9 rakamlar, sonra A-Z harfler
        private static readonly string[] CharacterClasses = BuildCharacterClasses();

        /// <summary>
        /// Karakter tespit modelini yükler. Otomatik olarak GPU kullanýr (varsa).
        /// </summary>
        /// <param name="modelPath">ONNX model dosya yolu</param>
        /// <param name="swapRB">RGB-BGR renk kanallarýný deðiþtir</param>
        /// <param name="useGpu">GPU kullan (true: otomatik tespit, false: sadece CPU)</param>
        public PlateCharDetector(string modelPath, bool swapRB = false, bool useGpu = true)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentNullException(nameof(modelPath));

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model dosyasý bulunamadý: {modelPath}", modelPath);

            // GPU/CPU SessionOptions oluþtur
            var sessionOptions = ExecutionProviderHelper.CreateOptimizedSessionOptions(preferGpu: useGpu);

            _session = new InferenceSession(modelPath, sessionOptions);
            _inputName = _session.InputMetadata.Keys.First();
            _swapRB = swapRB;

            (_inputHeight, _inputWidth) = InferInputDimensions(_session.InputMetadata[_inputName].Dimensions);
        }

        /// <summary>
        /// Model metadata'sýndan input boyutlarýný çýkarýr.
        /// NCHW ([N,3,H,W]) veya NHWC ([N,H,W,3]) formatlarýný destekler.
        /// </summary>
        private static (int Height, int Width) InferInputDimensions(ReadOnlySpan<int> dims)
        {
            if (dims.Length < 4)
                return (DefaultModelSize, DefaultModelSize);

            // NCHW formatý (batch, channel, height, width)
            if (dims[1] == 3)
            {
                int h = dims[2] > 0 ? dims[2] : DefaultModelSize;
                int w = dims[3] > 0 ? dims[3] : DefaultModelSize;
                return (h, w);
            }

            // NHWC formatý (batch, height, width, channel)
            if (dims[3] == 3)
            {
                int h = dims[1] > 0 ? dims[1] : DefaultModelSize;
                int w = dims[2] > 0 ? dims[2] : DefaultModelSize;
                return (h, w);
            }

            // Varsayýlan: NCHW
            return (dims[2] > 0 ? dims[2] : DefaultModelSize,
                    dims[3] > 0 ? dims[3] : DefaultModelSize);
        }

        /// <summary>
        /// ROI görüntüsünde karakter tespiti yapar.
        /// </summary>
        public CharacterDetectionResult Detect(
            Bitmap roiBitmap,
            float confidenceThreshold,
            bool enableNms,
            float nmsThreshold)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (roiBitmap == null)
                throw new ArgumentNullException(nameof(roiBitmap));

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var inputTensor = Preprocess(roiBitmap);
            var detections = RunInference(inputTensor, roiBitmap, confidenceThreshold);

            if (enableNms && detections.Count > 1)
            {
                detections = ApplyNms(detections, nmsThreshold);
            }

            sw.Stop();
            return new CharacterDetectionResult(detections, sw.ElapsedMilliseconds);
        }

        private List<PlateCharDetection> RunInference(
            DenseTensor<float> inputTensor,
            Bitmap roiBitmap,
            float confidenceThreshold)
        {
            var inputValue = NamedOnnxValue.CreateFromTensor(_inputName, inputTensor);
            var inputs = new List<NamedOnnxValue> { inputValue };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            return ProcessOutput(output, roiBitmap.Width, roiBitmap.Height, confidenceThreshold);
        }

        private DenseTensor<float> Preprocess(Bitmap bmp)
        {
            using var mat = BitmapConverter.ToMat(bmp);
            using var resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(_inputWidth, _inputHeight), 0, 0, InterpolationFlags.Linear);

            using var blob = CvDnn.BlobFromImage(
                resized,
                scaleFactor: 1.0 / 255.0,
                size: new OpenCvSharp.Size(_inputWidth, _inputHeight),
                mean: new Scalar(),
                swapRB: _swapRB,
                crop: false);

            var length = checked((int)blob.Total());
            var data = new float[length];
            Marshal.Copy(blob.Data, data, 0, length);

            // BlobFromImage NCHW layout üretir
            return new DenseTensor<float>(data, new[] { 1, 3, _inputHeight, _inputWidth });
        }

        private List<PlateCharDetection> ProcessOutput(
            Tensor<float> output,
            int roiWidth,
            int roiHeight,
            float confidenceThreshold)
        {
            var dims = output.Dimensions;
            if (dims.Length != 3 || dims[0] != 1)
                return new List<PlateCharDetection>();

            var (channelIndex, detectionIndex, channelCount, detectionCount) = DetermineOutputLayout(dims);
            var (classStart, classEnd) = DetermineClassRange(channelCount);

            float scaleX = roiWidth / (float)_inputWidth;
            float scaleY = roiHeight / (float)_inputHeight;

            var detections = new List<PlateCharDetection>(detectionCount);

            for (int i = 0; i < detectionCount; i++)
            {
                var detection = ProcessSingleDetection(
                    output,
                    i,
                    channelIndex,
                    detectionIndex,
                    classStart,
                    classEnd,
                    scaleX,
                    scaleY,
                    confidenceThreshold);

                if (detection != null)
                {
                    detections.Add(detection);
                }
            }

            return detections;
        }

        private static (int ChannelIndex, int DetectionIndex, int ChannelCount, int DetectionCount) DetermineOutputLayout(
            ReadOnlySpan<int> dims)
        {
            // Heuristic: Channel boyutu genellikle Detection sayýsýndan küçüktür
            if (dims[1] > dims[2])
            {
                return (ChannelIndex: 2, DetectionIndex: 1, ChannelCount: dims[2], DetectionCount: dims[1]);
            }
            else
            {
                return (ChannelIndex: 1, DetectionIndex: 2, ChannelCount: dims[1], DetectionCount: dims[2]);
            }
        }

        private static (int ClassStart, int ClassEnd) DetermineClassRange(int channelCount)
        {
            int classCount = CharacterClasses.Length;
            int classStart = Math.Max(4, channelCount - classCount);
            int classEnd = Math.Min(channelCount, classStart + classCount);
            return (classStart, classEnd);
        }

        private PlateCharDetection? ProcessSingleDetection(
            Tensor<float> output,
            int detectionIndex,
            int channelIndex,
            int detectionDimIndex,
            int classStart,
            int classEnd,
            float scaleX,
            float scaleY,
            float confidenceThreshold)
        {
            float GetValue(int ch) =>
                channelIndex == 1
                    ? output[0, ch, detectionIndex]
                    : output[0, detectionIndex, ch];

            // Bounding box
            float cx = GetValue(0);
            float cy = GetValue(1);
            float w = GetValue(2);
            float h = GetValue(3);

            // En yüksek sýnýf skoru
            int bestClassId = classStart;
            float bestScore = float.MinValue;

            for (int ch = classStart; ch < classEnd; ch++)
            {
                float score = GetValue(ch);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestClassId = ch;
                }
            }

            // Confidence threshold kontrolü
            if (bestScore < confidenceThreshold)
                return null;

            int classId = bestClassId - classStart;
            string label = GetCharacterLabel(classId);

            // Merkez koordinattan köþe koordinatýna dönüþtür ve ölçekle
            float x = (cx - w / 2f) * scaleX;
            float y = (cy - h / 2f) * scaleY;
            w *= scaleX;
            h *= scaleY;

            return new PlateCharDetection
            {
                X = (int)Math.Round(x),
                Y = (int)Math.Round(y),
                Width = (int)Math.Round(w),
                Height = (int)Math.Round(h),
                Confidence = bestScore,
                Class = label,
                ClassId = classId
            };
        }

        private static List<PlateCharDetection> ApplyNms(
            List<PlateCharDetection> detections,
            float iouThreshold)
        {
            var ordered = detections.OrderByDescending(d => d.Confidence).ToList();
            var kept = new List<PlateCharDetection>(detections.Count);

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

        private static float CalculateIoU(PlateCharDetection a, PlateCharDetection b)
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

        private static string[] BuildCharacterClasses()
        {
            var classes = new string[36]; // 10 rakam + 26 harf
            int index = 0;

            // 0-9 rakamlar
            for (int i = 0; i <= 9; i++)
            {
                classes[index++] = i.ToString();
            }

            // A-Z harfler
            for (char c = 'A'; c <= 'Z'; c++)
            {
                classes[index++] = c.ToString();
            }

            return classes;
        }

        private static string GetCharacterLabel(int classId)
        {
            if (classId >= 0 && classId < CharacterClasses.Length)
                return CharacterClasses[classId];

            return classId.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _session?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Karakter tespiti sonucunu temsil eder.
    /// </summary>
    public sealed record CharacterDetectionResult(
        List<PlateCharDetection> Detections,
        long ElapsedMs);

    /// <summary>
    /// Tek bir karakter tespitini temsil eder.
    /// </summary>
    public sealed class PlateCharDetection
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Confidence { get; set; }
        public string Class { get; set; } = string.Empty;
        public int ClassId { get; set; }

        public Rectangle GetRectangle() => new(X, Y, Width, Height);
    }
}
