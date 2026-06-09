using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ALPR.Detection
{
    public class TitanV8Result
    {
        public string Text { get; set; } = string.Empty;
        public float MeanConfidence { get; set; }
        public List<float> CharConfidences { get; set; } = new List<float>();

        public override string ToString() => $"{Text} ({MeanConfidence:F1}%)";
    }

    public class TitanV8Engine : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _alphabet = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Boşluk + 36 Karakter
        private const int IMG_WIDTH = 192;
        private const int IMG_HEIGHT = 96;

        public TitanV8Engine(string modelPath, bool useGpu = false)
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

        public TitanV8Result Predict(Mat image)
        {
            // 1. Preprocessing (128x64, Grayscale, Float32 [0-1])
            var inputTensor = Preprocess(image);

            // 2. Inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("image", inputTensor) };
            using var results = _session.Run(inputs);

            // Outputlar: [logits (1, 128, 38)]
            // Not: User örneğine göre 'logits' adında bir output bekleniyor.
            var logitsProperty = results.FirstOrDefault(o => o.Name == "logits") ?? results.First();
            var logits = logitsProperty.AsEnumerable<float>().ToArray();

            // 3. CTC Greedy Decoding + Probabilistic Logic
            return Decode(logits);
        }

        private DenseTensor<float> Preprocess(Mat src)
        {
            using var resized = new Mat();
            Cv2.Resize(src, resized, new OpenCvSharp.Size(IMG_WIDTH, IMG_HEIGHT));
            
            if (resized.Channels() > 1) 
                Cv2.CvtColor(resized, resized, ColorConversionCodes.BGR2GRAY);

            var tensor = new DenseTensor<float>(new[] { 1, IMG_HEIGHT, IMG_WIDTH, 1 });
            resized.ConvertTo(resized, MatType.CV_32F, 1.0 / 255.0);

            // OpenCvSharp Mat indexing is faster with Indexer
            var indexer = resized.GetGenericIndexer<float>();
            for (int y = 0; y < IMG_HEIGHT; y++)
            {
                for (int x = 0; x < IMG_WIDTH; x++)
                {
                    tensor[0, y, x, 0] = indexer[y, x];
                }
            }
            return tensor;
        }

        private TitanV8Result Decode(float[] logits)
        {
            const int seqLen = 128;
            const int vocabSize = 38; // 37 chars + 1 blank
            const int blankIdx = 37;
            
            var decodedChars = new List<char>();
            var confidences = new List<float>();
            
            int lastIdx = -1;
            for (int t = 0; t < seqLen; t++)
            {
                float maxLogit = float.MinValue;
                int bestIdx = 0;
                
                for (int c = 0; c < vocabSize; c++)
                {
                    float val = logits[t * vocabSize + c];
                    if (val > maxLogit)
                    {
                        maxLogit = val;
                        bestIdx = c;
                    }
                }

                // log_softmax'tan olasılığa geri dön (e^maxLogit)
                // Not: Model çıktısı logit ise bu olasılıktır. User e^x kullanmış.
                float prob = (float)Math.Exp(maxLogit);
                
                // CTC Greedy Logic: Blank değilse ve ardışık tekrar değilse ekle
                if (bestIdx != blankIdx)
                {
                    if (bestIdx != lastIdx)
                    {
                        decodedChars.Add(_alphabet[bestIdx]);
                        confidences.Add(prob * 100f);
                    }
                }
                lastIdx = bestIdx;
            }

            return new TitanV8Result
            {
                Text = new string(decodedChars.ToArray()).Trim(),
                CharConfidences = confidences,
                MeanConfidence = confidences.Count > 0 ? confidences.Average() : 0f
            };
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
