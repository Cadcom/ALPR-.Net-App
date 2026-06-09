using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX Runtime için GPU/CPU ExecutionProvider yönetimi
    /// DirectML (Windows GPU), CUDA (NVIDIA GPU) ve CPU desteği
    /// Python fast_plate_ocr'daki providers=['DmlExecutionProvider', 'CUDAExecutionProvider', 'CPUExecutionProvider'] ile aynı mantık
    /// </summary>
    public static class ExecutionProviderHelper
    {
        private static readonly Lazy<bool> _cudaAvailability = new(CheckCudaAvailability);
        private static readonly Lazy<bool> _directMlAvailability = new(CheckDirectMLAvailability);
        
        public static Action<string>? Logger { get; set; }

        public static SessionOptions CreateOptimizedSessionOptions(bool preferGpu = true)
        {
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                EnableMemoryPattern = true,
                EnableCpuMemArena = true,
                InterOpNumThreads = GetOptimalThreadCount(),
                IntraOpNumThreads = GetOptimalThreadCount(),
                ExecutionMode = ExecutionMode.ORT_PARALLEL,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            };

            if (!preferGpu)
            {
                Log("⚠️ CPU kullanılıyor (Optimize edilmiş)");
                return sessionOptions;
            }

            // 1. TensorRT (NVIDIA - en hızlı)
            if (TryEnableTensorRT(sessionOptions))
            {
                Log("🚀 GPU: TensorRT (NVIDIA) kullanılıyor");
                return sessionOptions;
            }

            // 2. CUDA (NVIDIA)
            if (TryEnableCuda(sessionOptions))
            {
                Log("🔥 GPU: CUDA (NVIDIA) kullanılıyor");
                return sessionOptions;
            }

            // 3. DirectML (Windows - AMD/Intel/NVIDIA fallback)
            if (TryEnableDirectML(sessionOptions))
            {
                Log("✅ GPU: DirectML (Windows) kullanılıyor");
                return sessionOptions;
            }

            Log("⚠️ CPU kullanılıyor (GPU bulunamadı)");
            return sessionOptions;
        }

        private static bool TryEnableTensorRT(SessionOptions options)
        {
            try
            {
                var trtOptions = new OrtTensorRTProviderOptions();
                trtOptions.UpdateOptions(new Dictionary<string, string>
                {
                    ["device_id"] = "0",
                    ["trt_engine_cache_enable"] = "1",
                    ["trt_engine_cache_path"] = "./trt_cache",
                    ["trt_fp16_enable"] = "1"
                });
                options.AppendExecutionProvider_Tensorrt(trtOptions);
                options.AppendExecutionProvider_CUDA(0);
                Directory.CreateDirectory("./trt_cache");
                System.Diagnostics.Debug.WriteLine("TensorRT: ✅ Yüklendi");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TensorRT: ❌ Hata → {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"TensorRT: Inner → {ex.InnerException.Message}");
                return false;
            }
        }

        /// <summary>
        /// DirectML provider'ı etkinleştirmeyi dener (Windows GPU - AMD, NVIDIA, Intel)
        /// Python'daki 'DmlExecutionProvider' ile aynı
        /// </summary>
        private static bool TryEnableDirectML(SessionOptions options)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            if (!IsDirectMLAvailable())
                return false;

            try
            {
                options.AppendExecutionProvider_DML(0);
                return true;
            }
            catch (Exception ex)
            {
                Log($"?? DirectML yüklenemedi: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// CUDA provider'ı etkinleştirmeyi dener (NVIDIA GPU)
        /// Python'daki 'CUDAExecutionProvider' ile aynı
        /// </summary>
        private static bool TryEnableCuda(SessionOptions options)
        {
            if (!IsGpuAvailable())
                return false;

            try
            {
                options.AppendExecutionProvider_CUDA(0);
                return true;
            }
            catch (Exception ex)
            {
                Log($"?? CUDA yüklenemedi: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// DirectML (Windows GPU) sistemde mevcut mu kontrol eder
        /// </summary>
        public static bool IsDirectMLAvailable() => _directMlAvailability.Value;

        private static bool CheckDirectMLAvailability()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                using var opts = new SessionOptions();
                opts.AppendExecutionProvider_DML(0);
                System.Diagnostics.Debug.WriteLine("? DirectML provider başarıyla yüklendi!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("? DirectML provider yüklenemedi:");
                System.Diagnostics.Debug.WriteLine($"   Hata: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Mesaj: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }

                if (ex.Message.Contains("DLL") || ex.Message.Contains("library"))
                {
                    System.Diagnostics.Debug.WriteLine("   ?? Çözüm: Microsoft.ML.OnnxRuntime.DirectML NuGet paketini yükleyin");
                }

                return false;
            }
        }

        /// <summary>
        /// CUDA (NVIDIA GPU) sistemde mevcut mu kontrol eder
        /// </summary>
        public static bool IsGpuAvailable() => _cudaAvailability.Value;

        private static bool CheckCudaAvailability()
        {
            try
            {
                using var opts = new SessionOptions();
                opts.AppendExecutionProvider_CUDA(0);
                System.Diagnostics.Debug.WriteLine("? CUDA provider başarıyla yüklendi!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("? CUDA provider yüklenemedi:");
                System.Diagnostics.Debug.WriteLine($"   Hata: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Mesaj: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }

                if (ex.Message.Contains("DLL") || ex.Message.Contains("library"))
                {
                    System.Diagnostics.Debug.WriteLine("   ?? Çözüm: Microsoft.ML.OnnxRuntime.Gpu NuGet paketini yükleyin");
                    System.Diagnostics.Debug.WriteLine("   ?? veya onnxruntime_providers_cuda.dll dosyasını çalışma dizinine kopyalayın");
                }

                return false;
            }
        }

        private static int GetOptimalThreadCount()
        {
            return Math.Max(1, Environment.ProcessorCount / 2);
        }

        public static string GetAvailableProviders()
        {
            var providers = new List<string>();

            if (IsDirectMLAvailable())
            {
                providers.Add("DirectML (Windows GPU - AMD/NVIDIA/Intel)");
            }

            if (IsGpuAvailable())
            {
                providers.Add("CUDA (NVIDIA GPU)");
            }

            providers.Add("CPU");
            return string.Join(", ", providers);
        }

        public static string GetSystemInfo()
        {
            var info = new System.Text.StringBuilder();
            
            info.AppendLine($"İşletim Sistemi: {RuntimeInformation.OSDescription}");
            info.AppendLine($"Mimari: {RuntimeInformation.OSArchitecture}");
            info.AppendLine($"İşlemci Sayısı: {Environment.ProcessorCount}");
            info.AppendLine($"Optimal Thread: {GetOptimalThreadCount()}");
            
            if (IsDirectMLAvailable())
            {
                info.AppendLine($"GPU DirectML: Evet (AMD/NVIDIA/Intel desteklenir)");
            }
            
            if (IsGpuAvailable())
            {
                info.AppendLine($"GPU CUDA: Evet (NVIDIA)");
                info.AppendLine($"CUDA Path: {Environment.GetEnvironmentVariable("CUDA_PATH") ?? "Tespit edilemedi"}");
            }
            
            if (!IsDirectMLAvailable() && !IsGpuAvailable())
            {
                info.AppendLine($"GPU Kullanılabilir: Hayır");
            }
            
            info.AppendLine($"Desteklenen Provider'lar: {GetAvailableProviders()}");

            return info.ToString();
        }

        private static void Log(string message)
        {
            Logger?.Invoke(message);
        }
    }
}
