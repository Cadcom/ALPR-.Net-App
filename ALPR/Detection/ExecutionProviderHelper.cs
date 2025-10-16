using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;

namespace ALPR.Detection
{
    /// <summary>
    /// ONNX Runtime için GPU/CPU ExecutionProvider yönetimi
    /// CUDA (NVIDIA GPU) desteði ile optimize edilmiþ
    /// </summary>
    public static class ExecutionProviderHelper
    {
        private static readonly Lazy<bool> _cudaAvailability = new(CheckCudaAvailability);
        
        public static Action<string>? Logger { get; set; }

        /// <summary>
        /// Optimize edilmiþ SessionOptions oluþturur
        /// Önce CUDA (NVIDIA GPU), sonra CPU dener
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

            if (preferGpu && TryEnableCuda(sessionOptions))
            {
                Log("?? GPU: CUDA (NVIDIA) kullanýlýyor");
                return sessionOptions;
            }

            Log("?? CPU kullanýlýyor (Optimize edilmiþ)");
            return sessionOptions;
        }

        /// <summary>
        /// CUDA provider'ý etkinleþtirmeyi dener
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
        /// CUDA (NVIDIA GPU) sistemde mevcut mu kontrol eder
        /// Thread-safe ve performanslý (lazy loading)
        /// </summary>
        public static bool IsGpuAvailable() => _cudaAvailability.Value;

        private static bool CheckCudaAvailability()
        {
            try
            {
                // CUDA_PATH environment variable kontrolü
                var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
                if (!string.IsNullOrEmpty(cudaPath) && Directory.Exists(cudaPath))
                {
                    return true;
                }

                // Windows'ta nvidia-smi kontrolü
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var nvidiaSmiPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.System),
                        "..", "Program Files", "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe"
                    );

                    return File.Exists(nvidiaSmiPath);
                }

                return false;
            }
            catch
            {
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
            
            if (IsGpuAvailable())
            {
                info.AppendLine($"GPU Kullanýlabilir: Evet (NVIDIA CUDA)");
                info.AppendLine($"CUDA Path: {Environment.GetEnvironmentVariable("CUDA_PATH") ?? "Tespit edilemedi"}");
            }
            else
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