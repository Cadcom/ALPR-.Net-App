using AlprApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace AlprApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class alprController : ControllerBase
    {
        private static readonly string ModelPath = "D:/Programming/Windows/ALPR2/ALPR/ALPR/bin/Debug/net9.0-windows/models/cct_xs_v1_global_model.onnx";
        private static readonly InferenceSession session = new InferenceSession(ModelPath);

        private const int InputHeight = 64;
        private const int InputWidth = 128;
        private const string InputName = "input";
        private const int BlankTokenIndex = 36;
        private static readonly string[] PlateVocabulary = new string[]
        {
            "0","1","2","3","4","5","6","7","8","9",
            "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"," "
        };

        [HttpPost]
        public IActionResult Post([FromBody] AlprRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ImageData))
                return BadRequest("Invalid request data.");

            try
            {
                Bitmap bitmap = Base64ToBitmap(request.ImageData);
                if (bitmap == null)
                    return BadRequest("Image decode failed.");

                string plate = RunOnnxPlateRecognition(bitmap);

                var response = new AlprResponse
                {
                    LicensePlate = plate,
                    Confidence = 0, // ONNX modelden confidence alınmıyor, isterseniz sabit verebilirsiniz
                    Timestamp = DateTime.UtcNow
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"ALPR error: {ex.Message}");
            }
        }

        private Bitmap Base64ToBitmap(string base64)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(imageBytes);
                return new Bitmap(ms);
            }
            catch
            {
                return null;
            }
        }

        private string RunOnnxPlateRecognition(Bitmap bitmap)
        {
            using var mat = BitmapConverter.ToMat(bitmap);
            using var resizedMat = new Mat();
            Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(InputWidth, InputHeight));
            Cv2.CvtColor(resizedMat, resizedMat, ColorConversionCodes.BGR2RGB);

            var inputTensor = new DenseTensor<byte>(new[] { 1, InputHeight, InputWidth, 3 });
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);
                    inputTensor[0, y, x, 0] = color.Item0;
                    inputTensor[0, y, x, 1] = color.Item1;
                    inputTensor[0, y, x, 2] = color.Item2;
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(InputName, inputTensor)
            };

            using var results = session.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();
            return DecodeCTC(outputTensor);
        }

        private string DecodeCTC(Tensor<float> outputTensor)
        {
            var dimensions = outputTensor.Dimensions.ToArray();
            int sequenceLength, vocabularySize;
            if (dimensions.Length == 3 && dimensions[0] == 1)
            {
                sequenceLength = dimensions[1];
                vocabularySize = dimensions[2];
            }
            else if (dimensions.Length == 2)
            {
                sequenceLength = dimensions[0];
                vocabularySize = dimensions[1];
            }
            else
            {
                return "DECODING_ERROR";
            }

            var resultChars = new List<string>();
            string lastChar = "";
            for (int t = 0; t < sequenceLength; t++)
            {
                float maxProb = -1.0f;
                int bestIndex = -1;
                for (int v = 0; v < vocabularySize; v++)
                {
                    float currentProb = (dimensions.Length == 3) ? outputTensor[0, t, v] : outputTensor[t, v];
                    if (currentProb > maxProb)
                    {
                        maxProb = currentProb;
                        bestIndex = v;
                    }
                }
                string currentChar = PlateVocabulary[bestIndex];
                bool isBlank = (bestIndex == BlankTokenIndex);
                if (!isBlank && currentChar != lastChar)
                {
                    resultChars.Add(currentChar);
                }
                lastChar = currentChar;
            }
            return string.Join("", resultChars);
        }
    }
}
