using Microsoft.ML.OnnxRuntime;
using System.Text.Json;

namespace ALPR.Detection
{
    public static class OnnxModelAnalyzer
    {
        public static ModelAnalysisResult AnalyzeModel(string modelPath)
        {
            var result = new ModelAnalysisResult
            {
                ModelPath = modelPath,
                FileName = Path.GetFileName(modelPath)
            };

            try
            {
                using var session = new InferenceSession(modelPath);
                
                // Input metadata
                result.InputNames = session.InputMetadata.Keys.ToList();
                foreach (var input in session.InputMetadata)
                {
                    var inputInfo = new TensorInfo
                    {
                        Name = input.Key,
                        ElementType = input.Value.ElementType.ToString(),
                        Dimensions = input.Value.Dimensions?.ToArray() ?? Array.Empty<int>()
                    };
                    result.Inputs.Add(inputInfo);
                }

                // Output metadata  
                result.OutputNames = session.OutputMetadata.Keys.ToList();
                foreach (var output in session.OutputMetadata)
                {
                    var outputInfo = new TensorInfo
                    {
                        Name = output.Key,
                        ElementType = output.Value.ElementType.ToString(),
                        Dimensions = output.Value.Dimensions?.ToArray() ?? Array.Empty<int>()
                    };
                    result.Outputs.Add(outputInfo);
                }

                result.IsValid = true;
                result.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public static string FormatAnalysisResult(ModelAnalysisResult result)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            return json;
        }
    }

    public class ModelAnalysisResult
    {
        public string ModelPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> InputNames { get; set; } = new();
        public List<string> OutputNames { get; set; } = new();
        public List<TensorInfo> Inputs { get; set; } = new();
        public List<TensorInfo> Outputs { get; set; } = new();
    }

    public class TensorInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public int[] Dimensions { get; set; } = Array.Empty<int>();
    }
}