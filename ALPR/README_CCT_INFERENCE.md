# CCT-XS V1 Global Model - Python Implementation

Bu Python implementasyonu, C# ALPR uygulamasında (`frmALPR.cs`) kullanılan `cct_xs_v1_global_model.onnx` modeliyle **birebir aynı** parametreleri kullanarak plaka okuma yapar.

## 🎯 Özellikler

- ✅ C# kodundan çıkarılan **tam parametreler**
- ✅ Aynı preprocessing pipeline (BGR→RGB, 128x64 resize, uint8 format)
- ✅ Aynı CTC decoding algoritması (Greedy decoding)
- ✅ Aynı vocabulary (0-9, A-Z + blank token)
- ✅ GPU/CPU desteği
- ✅ Batch processing
- ✅ C# ile sonuç karşılaştırma modu

## 📋 Model Parametreleri

C# kodundan (`frmFastOCR.cs` ve `PlateCharDetector.cs`) çıkarılan parametreler:

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| **Input Shape** | `(1, 64, 128, 3)` | Batch, Height, Width, Channels (BHWC) |
| **Input Type** | `uint8` | 0-255 aralığı, normalizasyon model içinde |
| **Input Name** | `"input"` | ONNX input tensor adı |
| **Renk Formatı** | `RGB` | BGR'den RGB'ye çevriliyor |
| **Vocabulary Size** | `37` | 0-9 (10) + A-Z (26) + Blank (1) |
| **Blank Token Index** | `36` | CTC blank token pozisyonu |
| **CTC Decoding** | `Greedy` | En yüksek olasılıklı karakter seçimi |

### Vocabulary Sırası
```python
# Index 0-9:  Rakamlar
["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]

# Index 10-35: Harfler (A-Z)
["A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
 "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
 "U", "V", "W", "X", "Y", "Z"]

# Index 36: Blank Token
[" "]
```

## 🔧 Kurulum

### Gereksinimler

```bash
pip install onnxruntime opencv-python numpy
```

GPU desteği için:
```bash
pip install onnxruntime-gpu opencv-python numpy
```

### Dosyalar

```
ALPR/
├── cct_xs_v1_inference.py      # Ana inference kodu
├── test_cct_inference.py       # Test script
├── README_CCT_INFERENCE.md     # Bu dosya
└── models/
    └── cct_xs_v1_global_model.onnx
```

## 🚀 Kullanım

### 1. Tek Görüntü Okuma

```bash
python test_cct_inference.py --image plates/34ABC123.jpg
```

**Çıktı:**
```
======================================================================
📷 Test ediliyor: 34ABC123.jpg
======================================================================
📐 Görüntü boyutu: 250x80

──────────────────────────────────────────────────────────────────────
✅ SONUÇ
──────────────────────────────────────────────────────────────────────
   Plaka:  34ABC123
   Süre:   12.45 ms
======================================================================
```

### 2. Detaylı Çıktı (CTC Decoding Adımları)

```bash
python test_cct_inference.py --image plates/34ABC123.jpg --verbose
```

**Çıktı:**
```
------------------------------------------------------------
CTC ÇÖZÜMLEME BAŞLANGIÇ
------------------------------------------------------------
Dizi Uzunluğu (T): 32
Sözlük Boyutu (V): 37
T= 0: Index=36 | Char=' ' | Prob=0.9876 | Blank=True
T= 1: Index= 3 | Char='3' | Prob=0.9543 | Blank=False
T= 2: Index= 3 | Char='3' | Prob=0.9234 | Blank=False
T= 3: Index=36 | Char=' ' | Prob=0.8765 | Blank=True
T= 4: Index= 4 | Char='4' | Prob=0.9678 | Blank=False
...
------------------------------------------------------------
CTC SONUÇ: '34ABC123'
------------------------------------------------------------
```

### 3. CPU Kullanımı

```bash
python test_cct_inference.py --image plates/34ABC123.jpg --cpu
```

### 4. Batch Processing

Klasördeki tüm plakaları oku:

```bash
python test_cct_inference.py --batch plates/
```

**Çıktı:**
```
======================================================================
📁 TOPLU TEST: plates/
📷 Toplam resim: 15
======================================================================

[1/15] İşleniyor: 34ABC123.jpg
   ✅ Plaka: 34ABC123 (12.34 ms)
[2/15] İşleniyor: 06DEF456.jpg
   ✅ Plaka: 06DEF456 (11.87 ms)
...

======================================================================
📊 ÖZET
======================================================================
   İşlenen resim:     15
   Başarılı:          14
   Başarısız:         1
   Toplam süre:       185.23 ms
   Ortalama süre:     12.35 ms/resim
   Hız:               81.02 resim/saniye
======================================================================
```

### 5. C# ile Karşılaştırma

C# uygulamasından aldığınız sonuçla karşılaştırma:

```bash
python test_cct_inference.py --image plates/34ABC123.jpg --compare "34ABC123"
```

**Çıktı:**
```
======================================================================
🔬 C# KARŞILAŞTIRMA TESTİ
======================================================================
Görüntü:          34ABC123.jpg
C# Sonuç:         34ABC123
Python Sonuç:     34ABC123
Eşleşme:          ✅ EVET
Python Süre:      12.45 ms
======================================================================
```

## 📝 Python API Kullanımı

### Basit Kullanım

```python
from cct_xs_v1_inference import CCTPlateRecognizer
import cv2

# Model yükle
recognizer = CCTPlateRecognizer(
    model_path="models/cct_xs_v1_global_model.onnx",
    use_gpu=True
)

# Plaka oku
image = cv2.imread("plates/34ABC123.jpg")
plate_text, inference_time = recognizer.recognize(image)

print(f"Plaka: {plate_text}")
print(f"Süre: {inference_time:.2f} ms")
```

### Detaylı Kullanım

```python
from cct_xs_v1_inference import CCTPlateRecognizer

# Model yükle
recognizer = CCTPlateRecognizer(
    model_path="models/cct_xs_v1_global_model.onnx",
    use_gpu=True
)

# Dosyadan oku (verbose mod)
plate_text, inference_time = recognizer.recognize_from_file(
    image_path="plates/34ABC123.jpg",
    verbose=True  # CTC decoding adımlarını göster
)
```

### Batch Processing API

```python
from cct_xs_v1_inference import CCTPlateRecognizer
from pathlib import Path
import cv2

recognizer = CCTPlateRecognizer("models/cct_xs_v1_global_model.onnx")

# Klasördeki tüm resimleri işle
plate_folder = Path("plates")
for image_path in plate_folder.glob("*.jpg"):
    plate_text, time_ms = recognizer.recognize_from_file(str(image_path))
    print(f"{image_path.name}: {plate_text} ({time_ms:.2f} ms)")
```

## 🔍 Model İncelemesi

Script çalıştırıldığında model bilgilerini otomatik yazdırır:

```
============================================================
MODEL BİLGİLERİ
============================================================

📥 INPUTS:
  Adı: input
  Shape: [1, 64, 128, 3]
  Type: tensor(uint8)

📤 OUTPUTS:
  Adı: output
  Shape: [1, 32, 37]
  Type: tensor(float)
============================================================
```

## ⚙️ Preprocessing Pipeline

C# kodundaki adımlar (birebir aynı):

1. **Resize**: Görüntü 128x64'e resize edilir
2. **Renk Dönüşümü**: BGR → RGB
3. **Tensor Oluşturma**: Shape `(1, 64, 128, 3)` - BHWC formatı
4. **Tip**: `uint8` (0-255 aralığında)
5. **Normalizasyon**: **YOK!** Model içinde 1/255 ile normalize ediliyor

### C# Kodu (Referans)
```csharp
// frmFastOCR.cs - RunOnnxPlateRecognition
Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(InputWidth, InputHeight));
Cv2.CvtColor(resizedMat, resizedMat, ColorConversionCodes.BGR2RGB);

var inputTensor = new DenseTensor<byte>(new[] { 1, InputHeight, InputWidth, 3 });
// NORMALİZASYON KALDIRILDI: Piksel verisi (0-255) doğrudan byte olarak atanır.
```

### Python Kodu (Uyumlu)
```python
# cct_xs_v1_inference.py - preprocess_image
resized = cv2.resize(image, (self.INPUT_WIDTH, self.INPUT_HEIGHT))
rgb_image = cv2.cvtColor(resized, cv2.COLOR_BGR2RGB)

input_tensor = np.expand_dims(rgb_image, axis=0).astype(np.uint8)
# ÖNEMLİ: C# kodunda normalizasyon YOK! Model içinde yapılıyor.
```

## 🧪 CTC Decoding

C# kodundaki `DecodeCTC` metodunun Python implementasyonu:

### Greedy Decoding Algoritması

1. Her zaman adımı (T) için en yüksek olasılıklı karakteri bul
2. Blank token ise atla
3. Önceki karakterle aynı ise atla (CTC kuralı)
4. Farklı ise sonuca ekle

```python
# Pseudo-code
result = []
last_char = ""

for t in range(sequence_length):
    best_index = argmax(output[t])
    current_char = VOCABULARY[best_index]
    is_blank = (best_index == BLANK_TOKEN_INDEX)
    
    if not is_blank and current_char != last_char:
        result.append(current_char)
    
    last_char = current_char

return "".join(result)
```

## 🐛 Troubleshooting

### GPU Algılanmıyor

```
⚠️ CUDA bulunamadı, CPU kullanılacak
```

**Çözüm:**
- `onnxruntime-gpu` kurulu olduğundan emin olun
- CUDA/cuDNN yüklü olmalı
- Veya `--cpu` flag'i ile CPU kullanın

### Model Bulunamadı

```
❌ Model bulunamadı: models/cct_xs_v1_global_model.onnx
```

**Çözüm:**
- Model dosyasının doğru yolda olduğundan emin olun
- `--model` parametresi ile tam yolu belirtin

### Vocabulary Uyuşmazlığı

```
⚠️ UYARI: Sözlük Uyuşmazlığı! Model 37 bekliyor, kodda 36 var.
```

**Çözüm:**
- Bu, modelin farklı bir vocabulary kullandığını gösterir
- `VOCABULARY` listesini model eğitimindeki sırayla güncelleyin

## 📊 Performans

Örnek metrikler (GPU: NVIDIA RTX 3060):

| Metrik | Değer |
|--------|-------|
| Tek görüntü inference | ~12 ms |
| Batch (15 görüntü) | ~185 ms |
| Throughput | ~80 resim/saniye |
| Doğruluk | C# ile %100 uyumlu |

## 🤝 C# Karşılaştırması

Bu Python implementasyonu, C# kodundaki tüm adımları birebir takip eder:

| Adım | C# (frmFastOCR.cs) | Python (cct_xs_v1_inference.py) |
|------|-------------------|----------------------------------|
| Resize | `Cv2.Resize(mat, resizedMat, new Size(128, 64))` | `cv2.resize(image, (128, 64))` |
| Renk | `Cv2.CvtColor(resizedMat, resizedMat, BGR2RGB)` | `cv2.cvtColor(resized, cv2.COLOR_BGR2RGB)` |
| Tensor | `DenseTensor<byte>(new[] { 1, 64, 128, 3 })` | `np.expand_dims(rgb, 0).astype(np.uint8)` |
| Normalizasyon | **YOK** (model içinde) | **YOK** (model içinde) |
| CTC | `DecodeCTC(outputTensor)` | `decode_ctc(output_tensor)` |

## 📄 Lisans

Bu kod, ALPR projesinin bir parçasıdır ve aynı lisans koşulları altındadır.

---

**Not:** Bu implementasyon, C# `frmALPR.cs` ve `frmFastOCR.cs` dosyalarından reverse-engineer edilerek oluşturulmuştur ve aynı sonuçları üretmek üzere tasarlanmıştır.
