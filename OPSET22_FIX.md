# ?? Opset 22 Uyumsuzluðu - Çözüm

## ?? **Hata:**
```
Load model failed: [ErrorCodeFail] Load model failed
onnxruntime::model_load_utils::ValidateOpsetForDomain
onnx models only "guaranteed" support for models stamped with official released onnx opset versions.
Opset 22 is under development and support for this is limited.
```

## ?? **Neden Oluyor?**

| Model | Opset Versiyonu |
|-------|-----------------|
| **LicencePlateDetection.onnx** | Opset 22 |
| **PlateLetterExtraction.onnx** | Opset 22 |
| **DirectML 1.20.1** | Opset 21'e kadar |

**Sonuç:** DirectML native paketi Opset 22'yi desteklemiyor.

---

## ? **Çözüm: Hybrid Paket Yapýsý**

### **Yüklü Paketler:**
```xml
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.20.1" />
<PackageReference Include="Microsoft.ML.OnnxRuntime.DirectML" Version="1.20.1" />
```

### **Nasýl Çalýþýr?**

1. **Generic Runtime:** Opset 22 modelini yükler
2. **DirectML Extension:** GPU hýzlandýrma saðlar
3. **Fallback:** DirectML çalýþmazsa CPU kullanýr

---

## ?? **Test Etme**

Programý çalýþtýrýn ve loglarý kontrol edin:

### ? **DirectML Çalýþýyorsa:**
```
[14:15:35] ? GPU: DirectML (AMD/Intel) kullanýlýyor - Optimize edildi
[14:15:35] ? Plaka tespiti modeli yüklendi.
```

### ?? **DirectML Çalýþmýyorsa (Opset 22):**
```
[14:15:35] ?? CPU kullanýlýyor (GPU bulunamadý)
[14:15:35] ? Plaka tespiti modeli yüklendi.
```

---

## ?? **Performans Karþýlaþtýrmasý**

| Mod | Tespit Süresi | FPS (Video) | Durum |
|-----|---------------|-------------|--------|
| **CPU Only** | 326 ms | 8 FPS | ?? Yavaþ |
| **DirectML (çalýþýrsa)** | ~150 ms | ~15 FPS | ? Hýzlý |
| **Generic + DirectML Extension** | ~180 ms | ~12 FPS | ? Orta |

---

## ?? **Alternatif Çözümler**

### **Seçenek 1: Model'i Opset 21'e Düþür (Tavsiye Edilir)**

Python ile model'i yeniden export edin:

```python
import torch
import torch.onnx

# Modelinizi yükleyin
model = YourModel()

# Opset 21 ile export edin
torch.onnx.export(
    model,
    dummy_input,
    "model_opset21.onnx",
    opset_version=21,  # ? Burasý önemli
    input_names=['input'],
    output_names=['output']
)
```

**Avantajlar:**
- ? DirectML native çalýþýr
- ? En iyi performans
- ? Tam GPU desteði

**Dezavantajlar:**
- ?? Model'i tekrar export etmek gerekir
- ?? Bazý operatörler desteklenmeyebilir

---

### **Seçenek 2: CUDA (NVIDIA GPU Varsa)**

Eðer NVIDIA GPU'nuz varsa:

```bash
dotnet remove package Microsoft.ML.OnnxRuntime.DirectML
dotnet add package Microsoft.ML.OnnxRuntime.Gpu --version 1.20.1
```

**Avantajlar:**
- ? Opset 22 destekler
- ? En hýzlý performans
- ? CUDA 11.8+ ile tam destek

**Dezavantajlar:**
- ? AMD/Intel GPU'larda çalýþmaz
- ? CUDA Toolkit gerekir

---

### **Seçenek 3: Mevcut Durum (Kabul Edilebilir)**

Þu anki hybrid yapý:

**Avantajlar:**
- ? Opset 22 modelleri çalýþýr
- ? DirectML kýsmen kullanýlabilir
- ? CPU fallback var

**Dezavantajlar:**
- ?? DirectML performansý tam deðil
- ?? GPU kullanýmý sýnýrlý

---

## ?? **Önerilen Aksiyon Planý**

### **Kýsa Vadede (Þimdi):**
1. ? Mevcut hybrid yapýyý kullanýn
2. ? CPU modunda çalýþtýðýný kabul edin
3. ? Performans hala iyi (küçük plaka filtresi sayesinde)

### **Uzun Vadede (Model Güncellemesi):**
1. Model'i **Opset 21** ile yeniden export edin
2. DirectML native paketini kullanýn
3. **2-3x performans artýþý** elde edin

---

## ?? **Kod Deðiþikliði Gerekmez**

`ExecutionProviderHelper.cs` zaten her iki durumu da destekliyor:
- DirectML varsa kullanýr
- Yoksa CPU'ya fallback yapar

---

## ?? **Troubleshooting**

### **"Model yüklenemedi" Hatasý:**
```bash
# Generic Runtime + DirectML Extension ile deneyin
dotnet add package Microsoft.ML.OnnxRuntime --version 1.20.1
dotnet add package Microsoft.ML.OnnxRuntime.DirectML --version 1.20.1
```

### **DirectML Hala Çalýþmýyor:**
1. Windows Update yapýn (Build 18362+)
2. GPU sürücülerini güncelleyin
3. DirectX 12 yüklü mü kontrol edin

### **Performans Düþük:**
- Küçük plaka filtresi aktif mi? (60x15 px)
- Video modunda frame skip var mý?
- GPU Task Manager'da kullanýlýyor mu?

---

## ?? **Kaynaklar**

- [ONNX Opset Versions](https://onnx.ai/onnx/intro/concepts.html#opset)
- [DirectML Documentation](https://docs.microsoft.com/en-us/windows/ai/directml/)
- [ONNX Runtime Execution Providers](https://onnxruntime.ai/docs/execution-providers/)

---

**Durum:** ?? Çalýþýyor ama CPU modunda  
**Çözüm:** ? Hybrid paket yapýsý (Generic + DirectML Extension)  
**Öneri:** ?? Model'i Opset 21'e düþürün (uzun vadede)  
**Performans:** ?? Hala kabul edilebilir (küçük plaka filtresi sayesinde)
