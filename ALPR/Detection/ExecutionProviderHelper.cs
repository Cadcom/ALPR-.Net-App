using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX Runtime için GPU/CPU ExecutionProvider yönetimi
    /// DirectML (Windows GPU), CUDA (NVIDIA GPU) ve CPU desteði
    /// Python fast_plate_ocr'daki providers=['DmlExecutionProvider', 'CUDAExecutionProvider', 'CPUExecutionProvider'] ile ayný mantýk
    /// </summary>
    public static class ExecutionProviderHelper
    {
        private static readonly Lazy<bool> _cudaAvailability = new(CheckCudaAvailability);
        private static readonly Lazy<bool> _directMlAvailability = new(CheckDirectMLAvailability);
        
        public static Action<string>? Logger { get; set; }

        /// <summary>
        /// Optimize edilmiþ SessionOptions oluþturur
        /// Öncelik sýrasý: DirectML (Windows GPU) -> CUDA (NVIDIA GPU) -> CPU
        /// </summary>
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
                Log("??? CPU kullanýlýyor (Optimize edilmiþ)");
                return sessionOptions;
            }

            // Önce DirectML (Windows için - tüm GPU'larý destekler: NVIDIA, AMD, Intel)
            if (TryEnableDirectML(sessionOptions))
            {
                Log("? GPU: DirectML (Windows) kullanýlýyor - Tüm GPU'lar destekleniyor");
                return sessionOptions;
            }

            // DirectML yoksa CUDA dene (Sadece NVIDIA)
            if (TryEnableCuda(sessionOptions))
            {
                Log("?? GPU: CUDA (NVIDIA) kullanýlýyor");
                return sessionOptions;
            }

            Log("??? CPU kullanýlýyor (GPU bulunamadý)");
            return sessionOptions;
        }

        /// <summary>
        /// DirectML provider'ý etkinleþtirmeyi dener (Windows GPU - AMD, NVIDIA, Intel)
        /// Python'daki 'DmlExecutionProvider' ile ayný
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
        /// CUDA provider'ý etkinleþtirmeyi dener (NVIDIA GPU)
        /// Python'daki 'CUDAExecutionProvider' ile ayný
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
                System.Diagnostics.Debug.WriteLine("? DirectML provider baþarýyla yüklendi!");
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
                System.Diagnostics.Debug.WriteLine("? CUDA provider baþarýyla yüklendi!");
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
                    System.Diagnostics.Debug.WriteLine("   ?? veya onnxruntime_providers_cuda.dll dosyasýný çalýþma dizinine kopyalayýn");
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
            
            info.AppendLine($"Ýþletim Sistemi: {RuntimeInformation.OSDescription}");
            info.AppendLine($"Mimari: {RuntimeInformation.OSArchitecture}");
            info.AppendLine($"Ýþlemci Sayýsý: {Environment.ProcessorCount}");
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
                info.AppendLine($"GPU Kullanýlabilir: Hayýr");
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
