using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX modeli kullanarak plaka karakteri tespiti yapan s�n�f.
    /// Thread-safe de�ildir, her thread i�in ayr� instance olu�turulmal�d�r.
    /// GPU deste�i: CUDA (NVIDIA), DirectML (Windows) ve CPU
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
        private readonly Type _inputElementType;
        private bool _disposed;

        private const int BlankTokenIndex = 36; // Bu ayar KES�NL�KLE KORUNMALI!

        private readonly string[] PlateVocabulary = new string[]
        {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L",
            "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            " ", // Index 36: Blank Token.
        };

        // Modelin bekledi�i kesin giri� boyutlar�
        private const int InputHeight = 64;
        private const int InputWidth = 128;
        private const string InputName = "input"; // Model metadata's�ndan al�nd�

        // Karakter s�n�flar�: 0-9 rakamlar, sonra A-Z harfler
        private static readonly Lazy<string[]> CharacterClassesLazy =
            new Lazy<string[]>(() => BuildCharacterClasses());

        private static string[] CharacterClasses => CharacterClassesLazy.Value;

        /// <summary>
        /// Karakter tespit modelini y�kler. GPU deste�i ile optimize edilmi�.
        /// </summary>
        /// <param name="modelPath">ONNX model dosya yolu</param>
        /// <param name="swapRB">RGB-BGR renk kanallar�n� de�i�tir</param>
        /// <param name="useGpu">GPU kullan (true: CUDA/DirectML otomatik tespit, false: sadece CPU)</param>
        public PlateCharDetector(string modelPath, bool swapRB = false, bool useGpu = true)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentNullException(nameof(modelPath));

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model dosyas� bulunamad�: {modelPath}", modelPath);

            _swapRB = swapRB;

            // GPU/CPU SessionOptions olu�tur - ExecutionProviderHelper kullanarak
            // Python'daki providers=['DmlExecutionProvider', 'CUDAExecutionProvider', 'CPUExecutionProvider'] ile ayn� mant�k
            var sessionOptions = ExecutionProviderHelper.CreateOptimizedSessionOptions(preferGpu: useGpu);

            try
            {
                _session = new InferenceSession(modelPath, sessionOptions);
                _inputName = _session.InputMetadata.Keys.First();

                var inputMetadata = _session.InputMetadata[_inputName];
                (_inputHeight, _inputWidth) = InferInputDimensions(inputMetadata.Dimensions);
                _inputElementType = inputMetadata.ElementType;

                // Log model y�kleme bilgisi
                System.Diagnostics.Debug.WriteLine($"? PlateCharDetector y�klendi: {Path.GetFileName(modelPath)}");
                System.Diagnostics.Debug.WriteLine($"   Input: {_inputName} [{_inputHeight}x{_inputWidth}]");
                System.Diagnostics.Debug.WriteLine($"   Data Type: {_inputElementType.Name}");
                System.Diagnostics.Debug.WriteLine($"   GPU: {(useGpu ? "�stendi (CUDA/DirectML)" : "Pasif - CPU Only")}");
            }
            catch (Exception ex)
            {
                sessionOptions?.Dispose();
                System.Diagnostics.Debug.WriteLine($"? PlateCharDetector y�kleme hatas�: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Plaka g�r�nt�s�nden OCR ile metin okuma (GPU destekli)
        /// </summary>
        public CharacterDetectionResult2 RunOnnxPlateRecognition(Bitmap bitmap)
        {
            using var mat = BitmapConverter.ToMat(bitmap);
            using var resizedMat = new Mat();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(_inputWidth, _inputHeight));

            // Renk �evirme: BGR -> RGB 
            Cv2.CvtColor(resizedMat, resizedMat, ColorConversionCodes.BGR2RGB);

            // Model veri tipine g�re tensor olu�tur ve tahmin yap
            object inputResult = _inputElementType.Name switch
            {
                nameof(Byte) => RunInferenceWithByteInput(resizedMat),
                nameof(Single) => RunInferenceWithFloatInput(resizedMat),
                nameof(Half) => RunInferenceWithFloat16Input(resizedMat),
                _ => throw new NotSupportedException($"Veri tipi desteklenmiyor: {_inputElementType.Name}")
            };

            var (plateText, elapsed) = ((string, long))inputResult;

            sw.Stop();

            return new CharacterDetectionResult2(plateText, elapsed);
        }

        private (string, long) RunInferenceWithByteInput(Mat resizedMat)
        {
            var expectedDims = _session.InputMetadata[_inputName].Dimensions;
            if (expectedDims[0] < 1) expectedDims[0] = 1; // Fix dynamic batch

            var inputTensor = new DenseTensor<byte>(expectedDims);
            bool isNCHW = expectedDims.Length == 4 && expectedDims[1] == 3;

            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    if (isNCHW)
                    {
                        inputTensor[0, 0, y, x] = color.Item0; // R
                        inputTensor[0, 1, y, x] = color.Item1; // G
                        inputTensor[0, 2, y, x] = color.Item2; // B
                    }
                    else
                    {
                        inputTensor[0, y, x, 0] = color.Item0; // R
                        inputTensor[0, y, x, 1] = color.Item1; // G
                        inputTensor[0, y, x, 2] = color.Item2; // B
                    }
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<byte>(_inputName, inputTensor)
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var results = _session.Run(inputs);

                if (results == null || !results.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX sonu� bo� d�nd� (Byte)");
                    return ("", 0);
                }

                var firstResult = results.First();
                if (firstResult == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX ilk sonu� null (Byte)");
                    return ("", 0);
                }

                var outputTensor = firstResult.AsTensor<float>();
                if (outputTensor == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? Output tensor null (Byte)");
                    return ("", 0);
                }

                string plateText = DecodeCTC(outputTensor);
                sw.Stop();
                return (plateText, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"? Byte inference hatas�: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                return ("", 0);
            }
        }

        private (string, long) RunInferenceWithFloatInput(Mat resizedMat)
        {
            var expectedDims = _session.InputMetadata[_inputName].Dimensions;
            if (expectedDims[0] < 1) expectedDims[0] = 1;

            var inputTensor = new DenseTensor<float>(expectedDims);
            bool isNCHW = expectedDims.Length == 4 && expectedDims[1] == 3;

            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    if (isNCHW)
                    {
                        inputTensor[0, 0, y, x] = color.Item0 / 255f;
                        inputTensor[0, 1, y, x] = color.Item1 / 255f;
                        inputTensor[0, 2, y, x] = color.Item2 / 255f;
                    }
                    else
                    {
                        inputTensor[0, y, x, 0] = color.Item0 / 255f;
                        inputTensor[0, y, x, 1] = color.Item1 / 255f;
                        inputTensor[0, y, x, 2] = color.Item2 / 255f;
                    }
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(_inputName, inputTensor)
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var results = _session.Run(inputs);
                if (results == null || !results.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX sonu� bo� d�nd� (Float)");
                    return ("", 0);
                }

                var firstResult = results.First();
                if (firstResult == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX ilk sonu� null (Float)");
                    return ("", 0);
                }

                var outputTensor = firstResult.AsTensor<float>();
                if (outputTensor == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? Output tensor null (Float)");
                    return ("", 0);
                }

                string plateText = DecodeCTC(outputTensor);
                sw.Stop();
                return (plateText, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"? Float inference hatas�: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                return ("", 0);
            }
        }

        private (string, long) RunInferenceWithFloat16Input(Mat resizedMat)
        {
            var expectedDims = _session.InputMetadata[_inputName].Dimensions;
            if (expectedDims[0] < 1) expectedDims[0] = 1;

            var inputTensor = new DenseTensor<Half>(expectedDims);
            bool isNCHW = expectedDims.Length == 4 && expectedDims[1] == 3;

            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    if (isNCHW)
                    {
                        inputTensor[0, 0, y, x] = (Half)(color.Item0 / 255f);
                        inputTensor[0, 1, y, x] = (Half)(color.Item1 / 255f);
                        inputTensor[0, 2, y, x] = (Half)(color.Item2 / 255f);
                    }
                    else
                    {
                        inputTensor[0, y, x, 0] = (Half)(color.Item0 / 255f);
                        inputTensor[0, y, x, 1] = (Half)(color.Item1 / 255f);
                        inputTensor[0, y, x, 2] = (Half)(color.Item2 / 255f);
                    }
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<Half>(_inputName, inputTensor)
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var results = _session.Run(inputs);
                if (results == null || !results.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX sonu� bo� d�nd� (Float16)");
                    return ("", 0);
                }

                var firstResult = results.First();
                if (firstResult == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? ONNX ilk sonu� null (Float16)");
                    return ("", 0);
                }

                var outputTensor = firstResult.AsTensor<float>();
                if (outputTensor == null)
                {
                    System.Diagnostics.Debug.WriteLine("?? Output tensor null (Float16)");
                    return ("", 0);
                }

                string plateText = DecodeCTC(outputTensor);
                sw.Stop();
                return (plateText, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                System.Diagnostics.Debug.WriteLine($"? Float16 inference hatas�: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                return ("", 0);
            }
        }

        private DenseTensor<byte> CreateByteInput(Mat resizedMat)
        {
            var inputTensor = new DenseTensor<byte>(new[] { 1, InputHeight, InputWidth, 3 });

            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    inputTensor[0, y, x, 0] = color.Item0; // R
                    inputTensor[0, y, x, 1] = color.Item1; // G
                    inputTensor[0, y, x, 2] = color.Item2; // B
                }
            }

            return inputTensor;
        }

        private DenseTensor<float> CreateFloatInput(Mat resizedMat)
        {
            var inputTensor = new DenseTensor<float>(new[] { 1, InputHeight, InputWidth, 3 });

            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    // 0-255'ten 0.0-1.0'a normalize et
                    inputTensor[0, y, x, 0] = color.Item0 / 255f; // R
                    inputTensor[0, y, x, 1] = color.Item1 / 255f; // G
                    inputTensor[0, y, x, 2] = color.Item2 / 255f; // B
                }
            }

            return inputTensor;
        }

        private DenseTensor<Float16> CreateFloat16Input(Mat resizedMat)
        {
            var inputTensor = new DenseTensor<Float16>(new[] { 1, InputHeight, InputWidth, 3 });

            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    // 0-255'ten 0.0-1.0'a normalize et
                    inputTensor[0, y, x, 0] = (Float16)(color.Item0 / 255f); // R
                    inputTensor[0, y, x, 1] = (Float16)(color.Item1 / 255f); // G
                    inputTensor[0, y, x, 2] = (Float16)(color.Item2 / 255f); // B
                }
            }

            return inputTensor;
        }

        /// <summary>
        /// CTC ��kt�s�n� Greedy Decoding ile plaka metnine �evirir
        /// </summary>
        private string DecodeCTC(Tensor<float> outputTensor)
        {
            var dimensions = outputTensor.Dimensions.ToArray();
            int sequenceLength = 0;
            int vocabularySize = PlateVocabulary.Length;

            if (dimensions.Length == 3 && dimensions[0] == 1)
            {
                sequenceLength = dimensions[1];
                vocabularySize = dimensions[2];
            }
            else if (dimensions.Length == 2)
            {
                sequenceLength = dimensions[0];
                vocabularySize = dimensions[1];
            }

            var resultChars = new List<string>();
            string lastChar = "";

            for (int t = 0; t < sequenceLength; t++)
            {
                float maxProb = -1.0f;
                int bestIndex = -1;

                for (int v = 0; v < vocabularySize; v++)
                {
                    float currentProb = (dimensions.Length == 3) ? outputTensor[0, t, v] : outputTensor[t, v];

                    if (currentProb > maxProb)
                    {
                        maxProb = currentProb;
                        bestIndex = v;
                    }
                }

                string currentChar = PlateVocabulary[bestIndex];
                bool isBlank = (bestIndex == BlankTokenIndex);

                // CTC Greedy Kurallar�
                if (!isBlank)
                {
                    resultChars.Add(currentChar);
                }

                lastChar = currentChar;
            }

            return string.Join("", resultChars);
        }

        /// <summary>
        /// Model metadata's�ndan input boyutlar�n� ��kar�r.
        /// </summary>
        private static (int Height, int Width) InferInputDimensions(ReadOnlySpan<int> dims)
        {
            if (dims.Length < 4)
                return (DefaultModelSize, DefaultModelSize);

            // NCHW format� (batch, channel, height, width)
            if (dims[1] == 3)
            {
                int h = dims[2] > 0 ? dims[2] : DefaultModelSize;
                int w = dims[3] > 0 ? dims[3] : DefaultModelSize;
                return (h, w);
            }

            // NHWC format� (batch, height, width, channel)
            if (dims[3] == 3)
            {
                int h = dims[1] > 0 ? dims[1] : DefaultModelSize;
                int w = dims[2] > 0 ? dims[2] : DefaultModelSize;
                return (h, w);
            }

            return (dims[2] > 0 ? dims[2] : DefaultModelSize,
                    dims[3] > 0 ? dims[3] : DefaultModelSize);
        }

        /// <summary>
        /// ROI g�r�nt�s�nde karakter tespiti yapar (eski metod - uyumluluk i�in korunuyor)
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

            float cx = GetValue(0);
            float cy = GetValue(1);
            float w = GetValue(2);
            float h = GetValue(3);

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

            if (bestScore < confidenceThreshold)
                return null;

            int classId = bestClassId - classStart;
            string label = GetCharacterLabel(classId);

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
            var classes = new string[36];
            int index = 0;

            for (int i = 0; i <= 9; i++)
            {
                classes[index++] = i.ToString();
            }

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

    public sealed record CharacterDetectionResult(
        List<PlateCharDetection> Detections,
        long ElapsedMs);

    public sealed record CharacterDetectionResult2(
        string Detection,
        long ElapsedMs);

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