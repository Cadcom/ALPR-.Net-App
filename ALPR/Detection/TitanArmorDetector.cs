using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ALPR.Detection
{
    public class TitanArmorDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private static readonly string CharList = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int BlankIndex = 37;

        public TitanArmorDetector(string modelPath, bool useGpu = false)
        {
            var options = new SessionOptions();
            if (useGpu)
            {
                try
                {
                    options.AppendExecutionProvider_CUDA();
                }
                catch
                {
                    // Fallback to CPU
                }
            }
            _session = new InferenceSession(modelPath, options);
        }

        public string Predict(Mat plateImg)
        {
            if (plateImg == null || plateImg.Empty()) return string.Empty;

            // 1. Preprocessing (Python logic match)
            using var gray = new Mat();
            if (plateImg.Channels() > 1)
                Cv2.CvtColor(plateImg, gray, ColorConversionCodes.BGR2GRAY);
            else
                plateImg.CopyTo(gray);

            // A. CLAHE (Contrast Enhancement)
            using var clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8));
            using var contrastImg = new Mat();
            clahe.Apply(gray, contrastImg);

            // B. Smart Padding (5% Black Frame)
            int padH = Math.Max((int)(contrastImg.Height * 0.05), 2);
            int padW = Math.Max((int)(contrastImg.Width * 0.05), 2);
            using var paddedImg = new Mat();
            Cv2.CopyMakeBorder(contrastImg, paddedImg, padH, padH, padW, padW, BorderTypes.Constant, Scalar.Black);

            // C. Aspect Ratio Protection & Resize (128x64 target canvas)
            int targetW = 128;
            int targetH = 64;
            double scale = Math.Min((double)targetW / paddedImg.Width, (double)targetH / paddedImg.Height);
            int newW = (int)(paddedImg.Width * scale);
            int newH = (int)(paddedImg.Height * scale);
            
            using var resized = new Mat();
            Cv2.Resize(paddedImg, resized, new OpenCvSharp.Size(newW, newH), 0, 0, InterpolationFlags.Area);

            // D. Placement on Canvas (Center on Black Background)
            using var canvas = new Mat(targetH, targetW, MatType.CV_8UC1, Scalar.Black);
            int offsetX = (targetW - newW) / 2;
            int offsetY = (targetH - newH) / 2;
            Rect roi = new Rect(offsetX, offsetY, newW, newH);
            resized.CopyTo(new Mat(canvas, roi));

            // 3. Tensor Preparation [1, 64, 128, 1]
            var tensor = new DenseTensor<float>(new[] { 1, 64, 128, 1 });
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    tensor[0, y, x, 0] = canvas.At<byte>(y, x) / 255.0f;
                }
            }

            // 4. Inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_image", tensor) };
            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray(); // (1 * 64 * 38)

            // 5. CTC Decoding
            return DecodeCTC(output);
        }

        private string DecodeCTC(float[] output)
        {
            List<char> decoded = new List<char>();
            int prevIndex = -1;

            // Output steps: 64, Vocab size: 38
            for (int step = 0; step < 64; step++)
            {
                int maxIndex = 0;
                float maxVal = float.MinValue;
                for (int c = 0; c < 38; c++)
                {
                    float val = output[step * 38 + c];
                    if (val > maxVal)
                    {
                        maxVal = val;
                        maxIndex = c;
                    }
                }

                if (maxIndex != prevIndex && maxIndex != BlankIndex)
                {
                    if (maxIndex < CharList.Length)
                    {
                        char c = CharList[maxIndex];
                        if (c != ' ') decoded.Add(c);
                    }
                }
                prevIndex = maxIndex;
            }

            return new string(decoded.ToArray());
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
