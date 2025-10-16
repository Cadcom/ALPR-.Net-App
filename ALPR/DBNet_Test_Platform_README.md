# DBNet Test Platformu - ALPR Sistemi

Bu geliþtirme, mevcut ALPR sistemine DBNet text detection modellerini test etmek için özel bir platform ekler.

## ?? Yeni Özellikler

### DBNet Test Platformu
- **Çoklu Model Desteði**: Ayný anda 4 farklý DBNet modelini test edebilme
- **Performans Karþýlaþtýrmasý**: Model hýzlarý ve doðruluk oranlarýnýn karþýlaþtýrýlmasý
- **JSON Çýktýsý**: Her model için detaylý JSON sonuçlarý
- **Görselleþtirme**: Test sonuçlarýnýn görsel olarak incelenmesi

## ?? Özellikler

### Model Test Desteði
- Improved DBNet model çýktýsý formatý
- Text coverage percentage hesaplama
- Contour sayýsý tahmini
- Confidence istatistikleri (ortalama, maksimum, minimum)

### Test JSON Formatý
```json
{
  "model_info": {
    "name": "Improved DBNet",
    "backbone": "resnet34",
    "input_size": [640, 640],
    "version": "2.0",
    "timestamp": "2025-01-12 15:30:45",
    "user": "Kullanýcý Adý"
  },
  "prediction": {
    "image_shape": [320, 320],
    "text_coverage_percent": 34.5205078125,
    "num_contours": 134,
    "confidence_stats": {
      "mean_probability": 0.36078885,
      "max_probability": 0.9999997,
      "min_probability": 2.3336069e-13
    }
  },
  "output_maps": {
    "probability_map_available": true,
    "threshold_map_available": true,
    "binary_map_available": true,
    "final_mask_available": true
  }
}
```

## ?? Kullaným

### DBNet Test Platformunu Açma
1. Ana ALPR arayüzünde **"?? DBNet Test"** butonuna týklayýn
2. DBNet test platformu yeni bir pencerede açýlacaktýr

### Model Seçimi
1. **Model 1-4 Seç** butonlarý ile test edilecek ONNX modellerini seçin
2. En az bir model seçilmelidir
3. Desteklenen model formatlarý:
   - `dbnet_improved_cpu.onnx`
   - `dbnet_improved_gpu.onnx`
   - `dbnet_improved_quantized.onnx`
   - `dbnet_improved_universal.onnx`

### Test Resmi Seçimi
1. **"?? Resim Seç"** butonu ile test edilecek plaka resmini seçin
2. Desteklenen formatlar: JPG, PNG, BMP, TIFF

### Test Çalýþtýrma
1. **"?? Testleri Baþlat"** butonuna týklayýn
2. Her model sýrasýyla test edilecektir
3. Sonuçlar otomatik olarak tabloda görüntülenecektir

### Sonuçlarý Ýnceleme
- **?? Model Karþýlaþtýrmasý**: Tablodan tüm modellerin performansýný karþýlaþtýrýn
- **?? Görselleþtirme**: Sol panelden model seçerek görsel sonuçlarý inceleyin
- **?? JSON Çýktýsý**: Dropdown'dan model seçerek detaylý JSON çýktýsýný görün

## ?? Test Sonuçlarý

Test sonuçlarý þunlarý içerir:
- **Model Adý**: Model dosyasýnýn adý
- **Çýkarým Süresi**: Milisaniye cinsinden iþlem süresi
- **Metin Kapsamý**: Tespit edilen text alanýnýn yüzdesi
- **Kontur Sayýsý**: Bulunan text bölgelerinin sayýsý
- **Güven Ýstatistikleri**: Ortalama ve maksimum güven skorlarý

## ?? Avantajlar

### Mevcut Sistemi Bozmadan Geliþtirme
- Ana ALPR sistemi deðiþtirilmedi
- Yeni özellik baðýmsýz bir platform olarak eklendi
- Mevcut model karþýlaþtýrma özelliði korundu

### Kolay Kullaným
- Sezgisel arayüz
- Otomatik sonuç karþýlaþtýrmasý
- Detaylý JSON çýktýsý
- Görsel feedback

### Performans Odaklý
- GPU/CPU desteði
- Çoklu model paralel testi (sýrayla)
- Bellek optimizasyonu

## ?? Teknik Detaylar

### Yeni Dosyalar
- `ALPR\Detection\DBNetTextDetector.cs`: DBNet model entegrasyonu
- `ALPR\frmDBNetTesting.cs`: Test platformu ana logic
- `ALPR\frmDBNetTesting.Designer.cs`: Test platformu arayüzü

### Deðiþiklikler
- `ALPR\frmALPR.cs`: DBNet test butonu eklendi
- `ALPR\frmALPR.Designer.cs`: Arayüz güncellemesi

## ?? Gelecek Geliþtirmeler

- Text segmentation overlay gösterimi
- Batch test desteði (çoklu resim)
- Test sonuçlarýný CSV'ye export
- Model accuracy metrics
- ROI (Region of Interest) seçimi
- Real-time model switching

## ?? Baþlangýç

1. Projeyi build edin
2. DBNet ONNX modellerinizi `models/` klasörüne koyun
3. Ana ALPR arayüzünü çalýþtýrýn
4. "?? DBNet Test" butonuna týklayýn
5. Modellerinizi ve test resmini seçin
6. Testleri baþlatýn ve sonuçlarý analiz edin!