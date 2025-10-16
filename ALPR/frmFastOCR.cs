using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ALPR
{
    public partial class frmFastOCR : Form
    {
        private string? _currentImagePath;
        private InferenceSession? _onnxSession;

        public frmFastOCR()
        {
            InitializeComponent();
            InitializeFastOcr();
        }

        private void InitializeFastOcr()
        {
            try
            {
                string modelPath = "D:\\Programming\\Windows\\ALPR2\\ALPR\\ALPR\\bin\\Debug\\net9.0-windows\\models\\cct_xs_v1_global_model.onnx";
                if (!File.Exists(modelPath))
                {
                    MessageBox.Show($"Model dosyası bulunamadı: {modelPath}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnProcess.Enabled = false;
                    return;
                }
                _onnxSession = new InferenceSession(modelPath);
                btnProcess.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ONNX modeli yüklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Plaka Resmi Seçin",
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|Tüm Dosyalar|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentImagePath = dialog.FileName;
                try
                {
                    using var mat = Cv2.ImRead(_currentImagePath);
                    if (!mat.Empty())
                    {
                        pictureBoxImage.Image?.Dispose();
                        pictureBoxImage.Image = BitmapConverter.ToBitmap(mat);
                        btnProcess.Enabled = true;
                        lblResult.Text = "Resim yüklendi.";
                        lblTime.Text = "";
                    }
                    else
                    {
                        lblResult.Text = "Resim dosyası geçersiz!";
                        btnProcess.Enabled = false;
                    }
                }
                catch (Exception ex)
                {
                    lblResult.Text = $"Resim yükleme hatası: {ex.Message}";
                    btnProcess.Enabled = false;
                }
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (_onnxSession == null || string.IsNullOrEmpty(_currentImagePath))
                return;

            btnProcess.Enabled = false;
            lblResult.Text = "Okuma başlatıldı...";
            lblTime.Text = "";
            try
            {
                using var mat = Cv2.ImRead(_currentImagePath);
                if (mat.Empty())
                {
                    lblResult.Text = "Resim yüklenemedi!";
                    return;
                }
                Bitmap bitmap = BitmapConverter.ToBitmap(mat);
                Stopwatch sw = Stopwatch.StartNew();
                string plateText = await Task.Run(() => RunOnnxPlateRecognition(bitmap));
                sw.Stop();
                lblResult.Text = $"Plaka: {plateText}";
                lblTime.Text = $"Süre: {sw.ElapsedMilliseconds} ms";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"OCR hatası: {ex.Message}";
            }
            finally
            {
                btnProcess.Enabled = true;
            }
        }


        private const int BlankTokenIndex = 36; // Bu ayar KESİNLİKLE KORUNMALI!

        private readonly string[] PlateVocabulary = new string[]
        {
    "0", // Index 0: YENİ DÜZELTME! (Eskiden 'Z' idi)
    "1", // Index 1
    "2", // Index 2
    "3", // Index 3
    "4", // Index 4
    "5", // Index 5
    "6", // Index 6
    "7", // Index 7
    "8", // Index 8
    "9", // Index 9
    
    "A", // Index 10
    // Index 11'den 35'e kadar olan harfler (25 harf)
    
    "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", // B-K (10)
    "L", "M", // L-M (2)
    "N", // Index 23: YENİ DÜZELTME! (Eskiden 'M' idi)
    "O", "P", "Q", "R",
    "S", // Index 28: YENİ DÜZELTME! (Eskiden 'Q' idi)
    "T", "U", "V", "W", "X", "Y", 
    
    // Index 35'te kalır, Z eksik (Çünkü 0'ı başa aldık)
    
    // YENİDEN SAYIMI KONTROL EDELİM (En baştan 37'yi tutturmak için)
    // 0-9 (10 eleman) + A-Z (26 eleman) + Blank (1 eleman) = 37 olmalı.
    // Sizin Listenizde: 0-9 (10 eleman) + A (1) + B-Y (24) + Blank (1) = 36 eleman!

    // KESİN VE NİHAİ DİZİNİN YAPISI (37 eleman)
    "0", // Index 0 (0-9 ve Blank'ın yeri değişti)
    "1", // Index 1
    "2", // Index 2
    "3", // Index 3
    "4", // Index 4
    "5", // Index 5
    "6", // Index 6
    "7", // Index 7
    "8", // Index 8
    "9", // Index 9 (10 Rakam bitti)

    "A", // Index 10
    "B", // Index 11
    "C", // Index 12
    "D", "E", "F", "G", "H", "I", "J", "K", "L",
    "M", // Index 23 (YANLIŞ. Index 23 'N' olmalı!)
    "N", // Index 23: YENİ DÜZELTME! (Eskiden 'M' idi)
    "O", "P", "Q", "R",
    "S", // Index 28: YENİ DÜZELTME! (Eskiden 'Q' idi)
    "T", "U", "V", "W", "X", "Y",
    "Z", // YENİ EKLENEN 'Z'
    
    " ", // Index 36: Blank Token.
        };
        // Not: Harflerin arasındaki sıra da bozuk olabilir. Alfabetik sırayı bozarak 'N' ve 'S'i doğru yerlere koyun.
        // Lütfen bu harfleri listenizde manuel olarak düzeltin: Index 23 = 'N', Index 28 = 'S'




        // Modelin beklediği kesin giriş boyutları
        private const int InputHeight = 64;
        private const int InputWidth = 128;
        private const string InputName = "input"; // Model metadata'sından alındı
        private const float Scale = 1.0f / 255.0f; // 0.00392156862745098

        // Bitmap'ten Mat'a çevirme için uygun bir konverter kullanılmalıdır (OpenCvSharp.Extensions).
        // Bu metodun doğru çalışması için, input olarak gelen 'bitmap' değişkeninin 
        // plakanın algılanıp kesilmiş görüntüsü olduğu varsayılır.
        // YENİ VERSİYON: UInt8 tipini kullanır ve normalizasyonu atlar.
        public string RunOnnxPlateRecognition(Bitmap bitmap)
        {
            // 1. Görüntüyü OpenCV ile uygun boyuta getir
            using var mat = BitmapConverter.ToMat(bitmap);
            using var resizedMat = new Mat();

            Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(InputWidth, InputHeight));

            // 2. Renk çevirme: BGR -> RGB 
            Cv2.CvtColor(resizedMat, resizedMat, ColorConversionCodes.BGR2RGB);

            // 3. Tensör oluştur (B, H, W, C) -> { 1, 64, 128, 3 }
            // KESİN DÜZELTME: DenseTensor<float> yerine DenseTensor<byte> kullanın.
            var inputTensor = new DenseTensor<byte>(new[] { 1, InputHeight, InputWidth, 3 });

            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var color = resizedMat.At<Vec3b>(y, x);

                    // NORMALİZASYON KALDIRILDI: Piksel verisi (0-255) doğrudan byte olarak atanır.
                    // Model, ONNX dosyasının içinde gömülü olan scaling (1/255) adımını uygulayacaktır.
                    inputTensor[0, y, x, 0] = color.Item0; // R
                    inputTensor[0, y, x, 1] = color.Item1; // G
                    inputTensor[0, y, x, 2] = color.Item2; // B
                }
            }

            // 4. ONNX ile tahmin
            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(InputName, inputTensor)
        };

            using var results = _onnxSession.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();

            // 5. CTC post-processing (DecodeCTC metodunuz burada çağrılır.)
            string plateText = DecodeCTC(outputTensor);

            return plateText;
        }

        private void AppendLog( string message)
        {
            if (txtResults.InvokeRequired)
            {
                // 2. Başka bir iş parçacığındaysak, Invoke kullanarak UI iş parçacığına geç
                // Bir Action (temsilci) oluşturup, bu Action'ı UI iş parçacığında çalıştırırız.
                txtResults.Invoke(new Action(() =>
                {
                    txtResults.AppendText(message + Environment.NewLine);
                }));
            }
            else
            {
                // 3. Zaten doğru iş parçacığındaysak (örneğin buton olayındayken), doğrudan ekle
                txtResults.AppendText(message + Environment.NewLine);
            }
        }

        /// <summary>
        /// CTC çıktısını (olasılık tensörünü) alıp, Greedy Decoding ile plaka metnine çevirir.
        /// </summary>
        /// <summary>
        /// CTC çıktısını alıp Greedy Decoding ile plaka metnine çevirir ve logları TextBox'a yazar.
        /// </summary>
        private string DecodeCTC(Tensor<float> outputTensor)
        {
            var dimensions = outputTensor.Dimensions.ToArray();
            int sequenceLength;
            int vocabularySize;

            // Boyutları kontrol et ve al... (önceki koddan)
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
                AppendLog( $"HATA: Beklenmeyen çıktı tensörü boyutu: {dimensions.Length}.");
                return "DECODING_HATA";
            }

            // Sözlük Boyutu Kontrolü
            if (vocabularySize != PlateVocabulary.Length)
            {
                AppendLog( $"UYARI: Sözlük Uyuşmazlığı! Model {vocabularySize} bekliyor, kodda {PlateVocabulary.Length} var.");
                // Yine de devam edelim, ama bu büyük bir sorundur.
            }

            AppendLog( "--- CTC ÇÖZÜMLEME LOGLARI BAŞLANGIÇ ---");
            AppendLog( $"Dizi Uzunluğu (T): {sequenceLength}, Sözlük Boyutu (V): {vocabularySize}");

            var resultChars = new List<string>();
            string lastChar = "";

            // Her zaman adımını döngüye al
            for (int t = 0; t < sequenceLength; t++)
            {
                float maxProb = -1.0f;
                int bestIndex = -1;

                // En yüksek olasılıklı indeksi bul
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

                // LOGLAMA (Her Zaman Adımı)
                AppendLog(
                    $"T={t,2}: Index={bestIndex,2} | Char='{currentChar}' | Prob={maxProb:F4} | Blank={isBlank}");

                // CTC Greedy Kuralları
                if (!isBlank && currentChar != lastChar)
                {
                    resultChars.Add(currentChar);
                }

                lastChar = currentChar;
            }

            string finalPlate = string.Join("", resultChars);
            AppendLog( $"--- CTC ÇÖZÜMLEME SONUÇ ---");
            AppendLog( $"HAM ÇIKTI (CTC Sonrası): {finalPlate}");

            return finalPlate;
        }
    }
}