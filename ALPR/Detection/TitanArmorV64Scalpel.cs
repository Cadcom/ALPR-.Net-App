using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ALPR.Detection
{
    public class TitanArmorV64Scalpel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _characters = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly int _blankIndex = 37;
        private readonly float _temperature = 1.35f; // User specified T=1.35 for v6.4/v6.3 logic
        private readonly float _blankThreshold = 0.12f;

        public TitanArmorV64Scalpel(string modelPath, bool useGpu = false)
        {
            var options = new SessionOptions();
            if (useGpu)
            {
                try { options.AppendExecutionProvider_CUDA(0); } catch { }
            }
            _session = new InferenceSession(modelPath, options);
        }

        public OcrResult PredictDetailed(Mat inputMat)
        {
            using var processed = Preprocess(inputMat);
            var tensor = CreateTensor(processed);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("v6_image", tensor) };

            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            return DecodeDetailed(output);
        }

        private Mat Preprocess(Mat src)
        {
            Mat gray = new Mat();
            if (src.Channels() == 3) Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else src.CopyTo(gray);

            using var clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8));
            clahe.Apply(gray, gray);

            // Cerebral Edge Protection (CEP) padding
            int padH = Math.Max((int)(gray.Height * 0.10), 6);
            int padW = Math.Max((int)(gray.Width * 0.10), 6);
            Cv2.CopyMakeBorder(gray, gray, padH, padH, padW, padW, BorderTypes.Constant, Scalar.Black);

            double ratio = Math.Min(128.0 / gray.Width, 64.0 / gray.Height);
            int newW = (int)(gray.Width * ratio);
            int newH = (int)(gray.Height * ratio);

            var interp = (newW > gray.Width) ? InterpolationFlags.Cubic : InterpolationFlags.Area;
            Cv2.Resize(gray, gray, new OpenCvSharp.Size(newW, newH), 0, 0, interp);

            Mat canvas = Mat.Zeros(new OpenCvSharp.Size(128, 64), MatType.CV_8UC1);
            int xOffset = (128 - newW) / 2;
            int yOffset = (64 - newH) / 2;
            gray.CopyTo(new Mat(canvas, new Rect(xOffset, yOffset, newW, newH)));

            Mat floatMat = new Mat();
            canvas.ConvertTo(floatMat, MatType.CV_32FC1, 1.0 / 255.0);
            return floatMat;
        }

        private DenseTensor<float> CreateTensor(Mat processed)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 64, 128, 1 });
            var indexer = processed.GetGenericIndexer<float>();

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    tensor[0, y, x, 0] = indexer[y, x];
                }
            }
            return tensor;
        }

        private OcrResult DecodeDetailed(float[] flatOutput)
        {
            int classes = 38;
            int steps = flatOutput.Length / classes;
            var result = new OcrResult();
            
            // 1. Olasılıkları Hesapla (Softmax + Temperature Scaling)
            float[][] probMatrix = new float[steps][];
            for (int t = 0; t < steps; t++)
            {
                float[] logits = new float[classes];
                Array.Copy(flatOutput, t * classes, logits, 0, classes);
                for (int i = 0; i < classes; i++) logits[i] /= _temperature;

                float maxLogit = logits.Max();
                float sum = 0;
                probMatrix[t] = new float[classes];
                for (int i = 0; i < classes; i++)
                {
                    probMatrix[t][i] = (float)Math.Exp(logits[i] - maxLogit);
                    sum += probMatrix[t][i];
                }
                for (int i = 0; i < classes; i++) probMatrix[t][i] /= sum;
            }

            // 2. Probabilistic Decoding with Blank Threshold
            List<int> bestPath = new List<int>();
            for (int t = 0; t < steps; t++)
            {
                int idxMax = Array.IndexOf(probMatrix[t], probMatrix[t].Max());
                if (idxMax == _blankIndex)
                {
                    int idxBestChar = 0; float pBestChar = 0;
                    for(int i=0; i<_blankIndex; i++) if(probMatrix[t][i] > pBestChar) { pBestChar = probMatrix[t][i]; idxBestChar = i; }

                    if (probMatrix[t][_blankIndex] - pBestChar < _blankThreshold) bestPath.Add(idxBestChar);
                    else bestPath.Add(_blankIndex);
                }
                else bestPath.Add(idxMax);
            }

            // 3. CTC Grouping & Selection
            for (int i = 0; i < bestPath.Count; i++)
            {
                if (bestPath[i] != _blankIndex)
                {
                    int cid = bestPath[i];
                    int start = i;
                    while (i + 1 < bestPath.Count && bestPath[i + 1] == cid) i++;
                    int end = i;

                    float sumConf = 0;
                    for (int t = start; t <= end; t++) sumConf += probMatrix[t][cid];
                    float avgConf = sumConf / (end - start + 1);

                    result.Details.Add(new PlateCharDetail { 
                        Character = _characters[cid], 
                        Confidence = avgConf 
                    });
                }
            }

            result.Text = string.Join("", result.Details.Select(x => x.Character)).Replace(" ", "");
            return result;
        }

        public void Dispose() => _session?.Dispose();
    }
}
