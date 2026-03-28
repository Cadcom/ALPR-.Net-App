using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ALPR.Detection
{
    public class PredictionDetail
    {
        public char Character { get; set; }
        public float Confidence { get; set; }
        public bool IsSecure { get; set; }
    }

    public class SentinelResult
    {
        public string Text { get; set; }
        public bool IsSecure { get; set; }
        public List<PredictionDetail> Details { get; set; }
    }

    public class TitanArmorSentinel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _characters = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly int _blankIndex = 37;
        private readonly float _minCharConf = 0.85f;

        public TitanArmorSentinel(string modelPath, bool useGpu = false)
        {
            var options = new SessionOptions();
            if (useGpu)
            {
                try { options.AppendExecutionProvider_CUDA(0); } catch { }
            }
            _session = new InferenceSession(modelPath, options);
        }

        public SentinelResult Predict(Mat inputMat)
        {
            // 1. Preprocessing (Python 1:1 Match)
            using var processed = PrepareImage(inputMat);
            
            // Tensor olustur (NHWC: 1, 64, 128, 1)
            var inputTensor = new DenseTensor<float>(new[] { 1, 64, 128, 1 });
            var indexer = processed.GetGenericIndexer<float>();

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    inputTensor[0, y, x, 0] = indexer[y, x];
                }
            }

            // 2. Inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("v6_image", inputTensor) };
            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            // 3. Probabilistic CTC Decoding
            return DecodeProbabilistic(output);
        }

        private Mat PrepareImage(Mat src)
        {
            Mat gray = new Mat();
            if (src.Channels() == 3) Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else src.CopyTo(gray);

            // A. CLAHE
            using var clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8));
            clahe.Apply(gray, gray);

            // B. [SENTINEL FIX] Dynamic Padding (%10)
            int padH = Math.Max((int)(gray.Height * 0.10), 6);
            int padW = Math.Max((int)(gray.Width * 0.10), 6);
            Cv2.CopyMakeBorder(gray, gray, padH, padH, padW, padW, BorderTypes.Constant, Scalar.Black);

            // C. Aspect Ratio Resize
            double ratio = Math.Min(128.0 / gray.Width, 64.0 / gray.Height);
            int newW = (int)(gray.Width * ratio);
            int newH = (int)(gray.Height * ratio);

            // Dynamic Interpolation
            var interp = (newW > gray.Width) ? InterpolationFlags.Cubic : InterpolationFlags.Area;
            Cv2.Resize(gray, gray, new OpenCvSharp.Size(newW, newH), 0, 0, interp);

            // D. Center in Canvas (128x64)
            Mat canvas = Mat.Zeros(new OpenCvSharp.Size(128, 64), MatType.CV_8UC1);
            int xOffset = (128 - newW) / 2;
            int yOffset = (64 - newH) / 2;
            gray.CopyTo(new Mat(canvas, new Rect(xOffset, yOffset, newW, newH)));

            // E. Normalization
            Mat floatMat = new Mat();
            canvas.ConvertTo(floatMat, MatType.CV_32FC1, 1.0 / 255.0);
            return floatMat;
        }

        private SentinelResult DecodeProbabilistic(float[] rawOutput)
        {
            // Output shape: [64 steps, 38 classes]
            int steps = 64;
            int classes = 38;
            
            var details = new List<PredictionDetail>();
            var bestPath = new int[steps];

            // 1. Log-Softmax to Probs & Find Best Path
            for (int t = 0; t < steps; t++)
            {
                float maxVal = float.MinValue;
                int bestIdx = 0;

                for (int c = 0; c < classes; c++)
                {
                    float val = rawOutput[t * classes + c];
                    if (val > maxVal) { maxVal = val; bestIdx = c; }
                }
                bestPath[t] = bestIdx;
            }

            // 2. CTC Grouping & Confidence Calculation
            List<int> currentGroup = new List<int>();
            for (int t = 0; t < steps; t++)
            {
                int idx = bestPath[t];
                if (idx != _blankIndex)
                {
                    currentGroup.Add(t);
                    if (t == steps - 1 || bestPath[t + 1] != idx)
                    {
                        // Calculate average prob for the group
                        float totalProb = 0;
                        foreach (var stepIdx in currentGroup)
                        {
                            // Softmax on the fly for the winning class
                            float maxLogit = float.MinValue;
                            for (int c = 0; c < classes; c++) 
                                if (rawOutput[stepIdx * classes + c] > maxLogit) maxLogit = rawOutput[stepIdx * classes + c];
                            
                            double denom = 0;
                            for (int c = 0; c < classes; c++) 
                                denom += Math.Exp(rawOutput[stepIdx * classes + c] - maxLogit);
                            
                            float prob = (float)(Math.Exp(rawOutput[stepIdx * classes + idx] - maxLogit) / denom);
                            totalProb += prob;
                        }

                        float avgConf = totalProb / currentGroup.Count;
                        details.Add(new PredictionDetail {
                            Character = _characters[idx],
                            Confidence = avgConf,
                            IsSecure = avgConf >= _minCharConf
                        });
                        currentGroup.Clear();
                    }
                }
                else { currentGroup.Clear(); }
            }

            return new SentinelResult {
                Text = string.Join("", details.Select(d => d.Character)).Trim(),
                IsSecure = details.All(d => d.IsSecure) && details.Count > 0,
                Details = details
            };
        }

        public void Dispose() => _session?.Dispose();
    }
}
