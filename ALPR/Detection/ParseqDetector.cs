using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    public sealed class ParseqCharacterConfidence
    {
        public char Character { get; set; }
        public float Confidence { get; set; }
    }

    public sealed class ParseqOcrResult
    {
        public string Text { get; set; } = string.Empty;
        public float AverageConfidence { get; set; }
        public List<ParseqCharacterConfidence> Details { get; set; } = new();
    }

    /// <summary>
    /// Parseq ONNX modelini kullanarak plaka OCR yapan sınıf.
    /// Thread-safe değildir, her thread için ayrı instance oluşturulmalıdır.
    /// GPU desteği: CUDA (NVIDIA), DirectML (Windows) ve CPU
    /// </summary>
    public sealed class ParseqDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string _outputName;
        private readonly int _inputHeight = 32;
        private readonly int _inputWidth = 128;
        private readonly object _sessionLock = new();
        private readonly RunOptions _runOptions = new RunOptions();
        private readonly bool _isDirectML; // IOBinding dallanması için
        private readonly TensorElementType _inputDataType; // Dinamik model desteği için
        private bool _disposed;

        private const int EOS_ID = 0;
        private const int CHAR_START_IDX = 1;
        private int _bosId;
        private int _padId;
        private string _charset;

        public List<string> InitLogs { get; } = new List<string>();
        private readonly Action<string>? _logCallback;

        private void LogStep(string message)
        {
            InitLogs.Add(message);
            Debug.WriteLine(message);
            _logCallback?.Invoke(message);
        }

        public ParseqDetector(
            string modelPath,
            string charset = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`",
            bool useGpu = true,
            Action<string>? logCallback = null)
        {
            _logCallback = logCallback;

            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentNullException(nameof(modelPath));
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model dosyası bulunamadı: {modelPath}", modelPath);
            if (string.IsNullOrEmpty(charset))
                throw new ArgumentNullException(nameof(charset));

            _charset = charset;
            LogStep($"[Parseq Detector] Kurucu başladı: {Path.GetFileName(modelPath)} (useGpu={useGpu})");

            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            };

            bool gpuAdded = false;

            if (useGpu)
            {
                // ── CUDA denemesi ─────────────────────────────────────────
                try
                {
                    LogStep("  CUDA provider eklenmesi deneniyor...");
                    var cudaOpts = new OrtCUDAProviderOptions();
                    cudaOpts.UpdateOptions(new Dictionary<string, string>
                    {
                        { "device_id", "0" },
                        { "cudnn_conv_algo_search", "EXHAUSTIVE" },
                        { "do_copy_in_default_stream", "1" },
                        { "arena_extend_strategy", "kNextPowerOfTwo" }
                    });

                    sessionOptions.EnableMemoryPattern = true;
                    sessionOptions.EnableCpuMemArena = true;
                    sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                    sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
                    sessionOptions.InterOpNumThreads = Environment.ProcessorCount;

                    sessionOptions.AppendExecutionProvider_CUDA(cudaOpts);
                    LogStep("  ✓ CUDA provider başarıyla eklendi.");
                    gpuAdded = true;
                    _isDirectML = false;
                }
                catch (Exception cudaEx)
                {
                    LogStep($"  ! CUDA yüklenemedi: {cudaEx.Message}");
                }

                // ── DirectML denemesi ─────────────────────────────────────
                if (!gpuAdded)
                {
                    sessionOptions.EnableMemoryPattern = false;
                    sessionOptions.EnableCpuMemArena = false;
                    sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                    sessionOptions.IntraOpNumThreads = Math.Min(2, Environment.ProcessorCount);
                    sessionOptions.InterOpNumThreads = 1;

                    int[] dmlDeviceIds = { 1, 0 };
                    foreach (int devId in dmlDeviceIds)
                    {
                        try
                        {
                            sessionOptions.AppendExecutionProvider_DML(devId);
                            LogStep($"  ✓ DirectML provider başarıyla eklendi (Device {devId}).");
                            gpuAdded = true;
                            _isDirectML = true;
                            break;
                        }
                        catch (Exception dmlEx)
                        {
                            LogStep($"  ! DirectML Device {devId} yüklenemedi: {dmlEx.Message}");
                        }
                    }
                }
            }

            if (!gpuAdded)
            {
                LogStep("  CPU modu kullanılacak.");
                sessionOptions.EnableMemoryPattern = true;
                sessionOptions.EnableCpuMemArena = true;
                sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                sessionOptions.IntraOpNumThreads = Math.Min(4, Environment.ProcessorCount);
                sessionOptions.InterOpNumThreads = 1;
                _isDirectML = false;
            }

            try
            {
                LogStep("  InferenceSession oluşturuluyor...");
                _session = new InferenceSession(modelPath, sessionOptions);
                _inputName = _session.InputMetadata.Keys.First();
                _outputName = _session.OutputMetadata.Keys.First();
                _inputDataType = _session.InputMetadata[_inputName].ElementDataType;

                var inputDims = _session.InputMetadata[_inputName].Dimensions;
                if (inputDims.Length >= 4)
                {
                    _inputHeight = inputDims[2] > 0 ? inputDims[2] : 32;
                    _inputWidth = inputDims[3] > 0 ? inputDims[3] : 128;
                }

                var outDims = _session.OutputMetadata.Values.First().Dimensions;
                int actualVocab = (int)(outDims.LastOrDefault());
                int expectedVocab = _charset.Length + 3;

                if (actualVocab > 0 && actualVocab != expectedVocab)
                {
                    LogStep($"  ⚠ UYARI: Model vocab={actualVocab}, beklenen={expectedVocab}");
                    int requiredCharCount = actualVocab - 3;
                    if (_charset.Length < requiredCharCount)
                    {
                        string paddingChars = "{|~}";
                        int missingCount = requiredCharCount - _charset.Length;
                        string padding = paddingChars.Substring(0, Math.Min(missingCount, paddingChars.Length));
                        if (padding.Length < missingCount)
                            padding += new string('?', missingCount - padding.Length);
                        _charset += padding;
                        LogStep($"    → Charset {requiredCharCount} karaktere tamamlandı: {_charset}");
                    }
                }

                _bosId = actualVocab > 0 ? actualVocab - 2 : _charset.Length + 1;
                _padId = actualVocab > 0 ? actualVocab - 1 : _charset.Length + 2;

                LogStep($"  ✓ ParseqDetector yüklendi: {Path.GetFileName(modelPath)}");
                LogStep($"    Input:    {_inputName} [{_inputHeight}x{_inputWidth}] ({_inputDataType})");
                LogStep($"    Charset:  {_charset.Length} karakter → beklenen vocab={actualVocab}");
                LogStep($"    Tokenmap: EOS=0, chars=[1..{_charset.Length}], BOS={_bosId}, PAD={_padId}");
                LogStep($"    IOBinding: {(_isDirectML ? "AÇIK (DirectML)" : "KAPALI (CUDA/CPU)")}");

                // Warm-up
                try
                {
                    LogStep("  [i] ONNX model ısındırılıyor (Warm-up)...");
                    var dummyData = new float[3 * _inputHeight * _inputWidth];
                    for (int i = 0; i < 5; i++)
                        _ = RunSessionInternal(dummyData);
                    LogStep("  ✓ ONNX model ısındı.");
                }
                catch (Exception warmupEx)
                {
                    LogStep($"  ❌ Warm-up hatası: {warmupEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LogStep($"  ❌ InferenceSession oluşturma hatası: {ex.Message}");
                sessionOptions?.Dispose();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        public ParseqOcrResult RunParseqOcr(Bitmap bitmap)
        {
            var sw = Stopwatch.StartNew();
            var inputData = BuildInputTensor(bitmap);
            var preTime = sw.ElapsedMilliseconds;



            sw.Restart();
            var result = RunSessionInternal(inputData);
            var infTime = sw.ElapsedMilliseconds;

            Debug.WriteLine($"[Parseq Profile] Preprocess: {preTime}ms, Inference+Decode: {infTime}ms");
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Preprocessing
        // ─────────────────────────────────────────────────────────────────

        private Bitmap ResizeBicubic(Bitmap src, int width, int height)
        {
            var dest = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(dest))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                using var attr = new System.Drawing.Imaging.ImageAttributes();
                attr.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                g.DrawImage(src, new System.Drawing.Rectangle(0, 0, width, height),
                    0, 0, src.Width, src.Height,
                    System.Drawing.GraphicsUnit.Pixel, attr);
            }
            return dest;
        }

        private float[] BuildInputTensor(Bitmap bitmap)
        {
            using var resizedBitmap = ResizeBicubic(bitmap, _inputWidth, _inputHeight);

            var rect = new System.Drawing.Rectangle(0, 0, _inputWidth, _inputHeight);
            var bmpData = resizedBitmap.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int cStride = _inputWidth * _inputHeight;
            byte[] bgrBytes = new byte[cStride * 3];
            Marshal.Copy(bmpData.Scan0, bgrBytes, 0, bgrBytes.Length);
            resizedBitmap.UnlockBits(bmpData);

            float[] data = new float[3 * cStride];

            for (int i = 0; i < cStride; i++)
            {
                float b = bgrBytes[i * 3 + 0] / 127.5f - 1.0f;
                float g = bgrBytes[i * 3 + 1] / 127.5f - 1.0f;
                float r = bgrBytes[i * 3 + 2] / 127.5f - 1.0f;

                data[0 * cStride + i] = r;
                data[1 * cStride + i] = g;
                data[2 * cStride + i] = b;
            }

            return data;
        }

        private static Float16[] ConvertToFloat16(float[] input)
        {
            var output = new Float16[input.Length];
            for (int i = 0; i < input.Length; i++)
                output[i] = (Float16)input[i];
            return output;
        }

        // ─────────────────────────────────────────────────────────────────
        // ONNX Çalıştırma
        // ─────────────────────────────────────────────────────────────────

        private ParseqOcrResult RunSessionInternal(float[] inputData)
        {
            lock (_sessionLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_inputDataType == TensorElementType.Float16)
                {
                    var fp16Data = ConvertToFloat16(inputData);
                    if (_isDirectML)
                        return RunWithIOBindingHalf(fp16Data);
                    else
                        return RunDirectHalf(fp16Data);
                }
                else
                {
                    if (_isDirectML)
                        return RunWithIOBindingFloat(inputData);
                    else
                        return RunDirectFloat(inputData);
                }
            }
        }

        private ParseqOcrResult RunWithIOBindingHalf(Float16[] inputData)
        {
            var gch = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            try
            {
                long[] shape = { 1, 3, _inputHeight, _inputWidth };
                using var binding = _session.CreateIoBinding();

                using var inputTensor = OrtValue.CreateTensorValueFromMemory<Float16>(
                    OrtMemoryInfo.DefaultInstance,
                    inputData,
                    shape);

                binding.BindInput(_inputName, inputTensor);
                binding.BindOutputToDevice(_outputName, OrtMemoryInfo.DefaultInstance);

                var swInf = Stopwatch.StartNew();
                _session.RunWithBinding(_runOptions, binding);
                swInf.Stop();

                using var outputValues = binding.GetOutputValues();
                return DecodeOutput(outputValues.First(), swInf.ElapsedMilliseconds);
            }
            finally
            {
                gch.Free();
            }
        }

        private ParseqOcrResult RunWithIOBindingFloat(float[] inputData)
        {
            var gch = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            try
            {
                long[] shape = { 1, 3, _inputHeight, _inputWidth };
                using var binding = _session.CreateIoBinding();

                using var inputTensor = OrtValue.CreateTensorValueFromMemory<float>(
                    OrtMemoryInfo.DefaultInstance,
                    inputData,
                    shape);

                binding.BindInput(_inputName, inputTensor);
                binding.BindOutputToDevice(_outputName, OrtMemoryInfo.DefaultInstance);

                var swInf = Stopwatch.StartNew();
                _session.RunWithBinding(_runOptions, binding);
                swInf.Stop();

                using var outputValues = binding.GetOutputValues();
                return DecodeOutput(outputValues.First(), swInf.ElapsedMilliseconds);
            }
            finally
            {
                gch.Free();
            }
        }

        private ParseqOcrResult RunDirectHalf(Float16[] inputData)
        {
            using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(
                inputData,
                new long[] { 1, 3, _inputHeight, _inputWidth });

            var inputs = new Dictionary<string, OrtValue> { { _inputName, inputOrtValue } };

            var swInf = Stopwatch.StartNew();
            using var results = _session.Run(_runOptions, inputs, _session.OutputMetadata.Keys.ToArray());
            swInf.Stop();

            return DecodeOutput(results.First(), swInf.ElapsedMilliseconds);
        }

        private ParseqOcrResult RunDirectFloat(float[] inputData)
        {
            using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(
                inputData,
                new long[] { 1, 3, _inputHeight, _inputWidth });

            var inputs = new Dictionary<string, OrtValue> { { _inputName, inputOrtValue } };

            var swInf = Stopwatch.StartNew();
            using var results = _session.Run(_runOptions, inputs, _session.OutputMetadata.Keys.ToArray());
            swInf.Stop();

            return DecodeOutput(results.First(), swInf.ElapsedMilliseconds);
        }

        private ParseqOcrResult DecodeOutput(OrtValue output, long elapsedMs)
        {
            var shape = output.GetTensorTypeAndShape().Shape;
            int seqLen = (int)shape[1];
            int tokenCount = (int)shape[2];
            var elementType = output.GetTensorTypeAndShape().ElementDataType;



            if (elementType == TensorElementType.Float16)
            {
                var halfSpan = output.GetTensorDataAsSpan<Float16>();
                return DecodeHalf(halfSpan, seqLen, tokenCount);
            }
            else
            {
                var floatSpan = output.GetTensorDataAsSpan<float>();
                return Decode(floatSpan, seqLen, tokenCount);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Decoding — Greedy (argmax per position + Softmax Confidence)
        // ─────────────────────────────────────────────────────────────────

        private ParseqOcrResult Decode(ReadOnlySpan<float> span, int seqLen, int tokenCount)
        {
            var result = new ParseqOcrResult();
            var indices = new List<int>();

            for (int i = 0; i < seqLen; i++)
            {
                var slice = span.Slice(i * tokenCount, tokenCount);
                int maxIdx = ArgMax(slice);
                indices.Add(maxIdx);

                if (maxIdx == EOS_ID) break;
                if (maxIdx == _bosId || maxIdx == _padId) continue;

                int charIdx = maxIdx - CHAR_START_IDX;
                if ((uint)charIdx < (uint)_charset.Length)
                {
                    char c = _charset[charIdx];
                    float conf = GetSoftmaxConfidence(slice, maxIdx);
                    result.Details.Add(new ParseqCharacterConfidence { Character = c, Confidence = conf });
                }
            }

            result.Text = new string(result.Details.Select(d => d.Character).ToArray());
            result.AverageConfidence = result.Details.Any() ? result.Details.Average(d => d.Confidence) : 0f;


            return result;
        }

        private ParseqOcrResult DecodeHalf(ReadOnlySpan<Float16> span, int seqLen, int tokenCount)
        {
            var result = new ParseqOcrResult();
            var indices = new List<int>();

            for (int i = 0; i < seqLen; i++)
            {
                var slice = span.Slice(i * tokenCount, tokenCount);
                int maxIdx = ArgMaxHalf(slice);
                indices.Add(maxIdx);

                if (maxIdx == EOS_ID) break;
                if (maxIdx == _bosId || maxIdx == _padId) continue;

                int charIdx = maxIdx - CHAR_START_IDX;
                if ((uint)charIdx < (uint)_charset.Length)
                {
                    char c = _charset[charIdx];
                    float conf = GetSoftmaxConfidenceHalf(slice, maxIdx);
                    result.Details.Add(new ParseqCharacterConfidence { Character = c, Confidence = conf });
                }
            }

            result.Text = new string(result.Details.Select(d => d.Character).ToArray());
            result.AverageConfidence = result.Details.Any() ? result.Details.Average(d => d.Confidence) : 0f;


            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ArgMax(ReadOnlySpan<float> slice)
        {
            int best = 0; float bestVal = slice[0];
            for (int j = 1; j < slice.Length; j++)
                if (slice[j] > bestVal) { bestVal = slice[j]; best = j; }
            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ArgMaxHalf(ReadOnlySpan<Float16> slice)
        {
            int best = 0; float bestVal = (float)slice[0];
            for (int j = 1; j < slice.Length; j++)
            {
                float val = (float)slice[j];
                if (val > bestVal) { bestVal = val; best = j; }
            }
            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSoftmaxConfidence(ReadOnlySpan<float> slice, int maxIdx)
        {
            float maxVal = slice[maxIdx];
            double sum = 0.0;
            for (int j = 0; j < slice.Length; j++)
            {
                sum += Math.Exp(slice[j] - maxVal);
            }
            return (float)(1.0 / sum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSoftmaxConfidenceHalf(ReadOnlySpan<Float16> slice, int maxIdx)
        {
            float maxVal = (float)slice[maxIdx];
            double sum = 0.0;
            for (int j = 0; j < slice.Length; j++)
            {
                sum += Math.Exp((float)slice[j] - maxVal);
            }
            return (float)(1.0 / sum);
        }

        public void Dispose()
        {
            lock (_sessionLock)
            {
                if (_disposed) return;
                _disposed = true;
                _session?.Dispose();
                _runOptions?.Dispose();
            }
        }
    }
}