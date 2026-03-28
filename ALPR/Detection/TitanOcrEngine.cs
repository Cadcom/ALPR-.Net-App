using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ALPR.Detection
{
    public class TitanOcrResult
    {
        public string Text { get; set; } = string.Empty;
        public float MeanConfidence { get; set; }
        public List<float> CharConfidences { get; set; } = new List<float>();

        public override string ToString() => $"{Text} ({MeanConfidence:P1})";
    }

    public class TitanOcrEngine : IDisposable
    {
        private InferenceSession _session;
        private const int IMG_WIDTH = 128;
        private const int IMG_HEIGHT = 64;
        private const string CHAR_LIST = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public TitanOcrEngine(string modelPath, bool useGpu = false)
        {
            var options = new SessionOptions();
            options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
            
            if (useGpu)
            {
                try
                {
                    options.AppendExecutionProvider_CUDA(0);
                }
                catch
                {
                    // Fallback to CPU if GPU fails
                }
            }
            
            _session = new InferenceSession(modelPath, options);
        }

        public TitanOcrResult Predict(string imagePath)
        {
            using (var image = new Bitmap(imagePath))
            {
                return Predict(image);
            }
        }

        public TitanOcrResult Predict(Bitmap originalImage)
        {
            var inputTensor = Preprocess(originalImage);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("image", inputTensor)
            };

            using (var results = _session.Run(inputs))
            {
                var outputTensor = results.First().AsTensor<float>();
                return DecodeGreedy(outputTensor);
            }
        }

        private DenseTensor<float> Preprocess(Bitmap image)
        {
            // Create target bitmap (128x64)
            var resized = new Bitmap(IMG_WIDTH, IMG_HEIGHT, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Black);
                g.DrawImage(image, 0, 0, IMG_WIDTH, IMG_HEIGHT);
            }

            // Convert to Tensor [1, 64, 128, 1]
            var tensor = new DenseTensor<float>(new[] { 1, IMG_HEIGHT, IMG_WIDTH, 1 });
            
            // Lock bits for fast access
            var data = resized.LockBits(
                new Rectangle(0, 0, IMG_WIDTH, IMG_HEIGHT),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;
                for (int y = 0; y < IMG_HEIGHT; y++)
                {
                    for (int x = 0; x < IMG_WIDTH; x++)
                    {
                        // Standard Grayscale: 0.299R + 0.587G + 0.114B
                        // In 24bppRgb, layout is BGR
                        byte b = ptr[y * stride + x * 3];
                        byte g = ptr[y * stride + x * 3 + 1];
                        byte r = ptr[y * stride + x * 3 + 2];

                        float gray = (0.299f * r + 0.587f * g + 0.114f * b);

                        // Normalize [0, 255] -> [0.0, 1.0]
                        tensor[0, y, x, 0] = gray / 255.0f;
                    }
                }
            }

            resized.UnlockBits(data);
            resized.Dispose();
            return tensor;
        }

        private TitanOcrResult DecodeGreedy(Tensor<float> output)
        {
            // Output shape: [1, 64, 37]
            int timeSteps = output.Dimensions[1];
            int vocabSize = output.Dimensions[2];

            var indices = new List<int>();
            var confidences = new List<float>();

            int lastIdx = -1;
            for (int t = 0; t < timeSteps; t++)
            {
                float maxLogProb = float.NegativeInfinity;
                int maxIdx = -1;
                for (int v = 0; v < vocabSize; v++)
                {
                    float logProb = output[0, t, v];
                    if (logProb > maxLogProb)
                    {
                        maxLogProb = logProb;
                        maxIdx = v;
                    }
                }

                // CTC Logic with Confidence
                if (maxIdx != lastIdx)
                {
                    if (maxIdx != 0) // 0 is Blank
                    {
                        indices.Add(maxIdx);
                        // Convert LogProb to Prob (0.0 - 1.0)
                        confidences.Add((float)Math.Exp(maxLogProb));
                    }
                    lastIdx = maxIdx;
                }
            }

            char[] chars = indices.Select(idx => CHAR_LIST[idx - 1]).ToArray();

            return new TitanOcrResult
            {
                Text = new string(chars),
                CharConfidences = confidences,
                MeanConfidence = confidences.Count > 0 ? confidences.Average() : 0.0f
            };
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
