# ?? GPU Kullaným UI Ekle

## ? **Yapýlacak Deðiþiklikler**

### **1. Designer.cs - CheckBox Ekle**

`ALPR/frmALPR.Designer.cs` dosyasýný açýn ve þu deðiþiklikleri yapýn:

#### **a) Field Tanýmý (En alta)**
```csharp
private CheckBox chkUseGpu; // ? YENÝ (diðer CheckBox'larýn yanýna)
```

#### **b) InitializeComponent Ýçinde**
```csharp
private void InitializeComponent()
{
    // ...existing code...
    chkUseGpu = new CheckBox(); // ? EKLE (diðer kontrollerden sonra)
    
    // ...existing code...
    
    // chkUseGpu
    //
    chkUseGpu.AutoSize = true;
    chkUseGpu.Enabled = false; // Baþlangýçta devre dýþý
    chkUseGpu.Location = new Point(412, 58);
    chkUseGpu.Name = "chkUseGpu";
    chkUseGpu.Size = new Size(108, 19);
    chkUseGpu.TabIndex = 19;
    chkUseGpu.Text = "?? GPU Kullan";
    chkUseGpu.UseVisualStyleBackColor = true;
    chkUseGpu.CheckedChanged += chkUseGpu_CheckedChanged;
    
    // chkSavePlates location'ýný deðiþtir
    chkSavePlates.Location = new Point(280, 58); // Eski: (412, 57)
    
    // frmALPR
    Controls.Add(chkUseGpu); // ? EKLE
    // ...existing code...
}
```

---

### **2. frmALPR.cs - Event Handler Ekle**

`ALPR/frmALPR.cs` dosyasýna þu kodlarý ekleyin:

#### **a) LoadOnnxModel'de GPU CheckBox'ý Aktifleþtir**
```csharp
private void LoadOnnxModel()
{
    try
    {
        AddLog("?? MODEL YÜKLEME");
        AddLog(LogSeparator);
        
        LoadPlateDetectionModel();
        LoadCharacterDetectionModel();
        
        // GPU CheckBox'ýný aktifleþtir (eðer GPU varsa)
        if (ExecutionProviderHelper.IsGpuAvailable())
        {
            chkUseGpu.Enabled = true;
            chkUseGpu.Checked = false; // CPU varsayýlan (Opset 22 sorunu)
        }
        
        AddLog(LogSeparator);
        AddLog($"?? Minimum plaka boyutu: {MinPlateWidth}x{MinPlateHeight} px (Alan: {MinPlateArea} px²)");
        AddLog("? Sistem hazýr, resim veya video seçebilirsiniz.");
    }
    catch (Exception ex)
    {
        HandleModelLoadError(ex);
    }
}
```

#### **b) GPU CheckedChanged Event Handler**
```csharp
#region GPU Control
/// <summary>
/// GPU kullanýmý açýldýðýnda/kapatýldýðýnda tetiklenir
/// </summary>
private void chkUseGpu_CheckedChanged(object sender, EventArgs e)
{
    var useGpu = chkUseGpu.Checked;
    
    // Modelleri yeniden yükle
    ReloadModelsWithGpuSetting(useGpu);
}

/// <summary>
/// Modelleri GPU ayarýna göre yeniden yükler
/// </summary>
private void ReloadModelsWithGpuSetting(bool useGpu)
{
    try
    {
        AddLog(LogSeparator);
        AddLog($"?? Modeller yeniden yükleniyor... (GPU: {(useGpu ? "Aktif" : "Pasif")})");
        
        // Eski modelleri dispose et
        _detector?.Dispose();
        _charDetector?.Dispose();
        
        // Yeni modelleri yükle
        if (File.Exists(_modelPath))
        {
            _detector = new LicensePlateDetector(_modelPath, useGpu);
            AddLog($"? Plaka tespiti modeli yüklendi. (GPU: {(useGpu ? "Evet" : "Hayýr")})");
        }
        
        if (File.Exists(_charModelPath))
        {
            _charDetector = new PlateCharDetector(_charModelPath, swapRB: false, useGpu);
            AddLog($"? Karakter tespiti modeli yüklendi. (GPU: {(useGpu ? "Evet" : "Hayýr")})");
        }
        
        if (useGpu)
        {
            AddLog("?? Not: Opset 22 modelleri GPU'da çalýþmayabilir.");
        }
        
        AddLog("? Model yenileme tamamlandý.");
    }
    catch (Exception ex)
    {
        AddLog($"? Model yenileme hatasý: {ex.Message}");
        
        // Hata durumunda CPU moduna geri dön
        chkUseGpu.Checked = false;
        
        MessageBox.Show(
            $"GPU moduna geçiþ yapýlamadý: {ex.Message}\n\nCPU modunda devam ediliyor.",
            "Hata",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
#endregion
```

---

### **3. LicensePlateDetector & PlateCharDetector - useGpu Parametresi**

Her iki dosyada da `useGpu` varsayýlan deðerini `false` yapýn:

#### **LicensePlateDetector.cs** (Satýr ~31):
```csharp
public LicensePlateDetector(string modelPath, bool useGpu = false) // ? false
```

#### **PlateCharDetector.cs** (Satýr ~39):
```csharp
public PlateCharDetector(string modelPath, bool swapRB = false, bool useGpu = false) // ? false
```

---

## ?? **UI Görünümü**

```
???????????????????????????????????????????????????????????????????????
? [Resim Seç] [Video Seç] [? Baþlat] [? Durdur]                     ?
? FPS: 12.34  [?? Plakalarý Kaydet] [?? GPU Kullan]                  ?
? [? NMS Etkin] [? Karakter Kutularý]  NMS: 0.45  Plaka Güven: 0.60 ?
?                                      Frame Atla: 2  Kar. Güven: 0.30?
???????????????????????????????????????????????????????????????????????
?                                                                     ?
?                      [Plaka Görüntüsü]                             ?
?                                                                     ?
???????????????????????????????????????????????????????????????????????
? Tespit Bilgileri:                                                  ?
? [15:45:32] ? GPU Kullanýlabilir                                   ?
? [15:45:32] ?? Desteklenen: DirectML (AMD/Intel GPU), CPU          ?
? [15:45:32] ?? GPU devre dýþý - CPU kullanýlacak                   ?
? [15:45:33] ?? Frame 42: 68BM201                                    ?
???????????????????????????????????????????????????????????????????????
```

---

## ?? **Kullaným Senaryolarý**

### **Senaryo 1: CPU Modu (Varsayýlan)**
- ? Checkbox: Kapalý
- ? Stabilite: %100
- ? Opset 22: Çalýþýr
- ? Performans: ~220ms

### **Senaryo 2: GPU Modu (Deneysel)**
- ?? Checkbox: Açýk
- ?? Stabilite: Opset 22 sorunu
- ? Opset 22: Çalýþmayabilir
- ? Performans: ~150ms (çalýþýrsa)

---

## ?? **Çalýþma Akýþý**

```
1. Program açýlýr
   ?? GPU kullanýlabilir mi? (ExecutionProviderHelper)
   ?? Evet ? chkUseGpu.Enabled = true
   ?? Hayýr ? chkUseGpu.Enabled = false

2. Kullanýcý checkboxý deðiþtirir
   ?? Checked = true ? GPU moduna geç
   ?   ?? Modelleri dispose et
   ?   ?? Yeni modelleri GPU ile yükle
   ?   ?? Opset 22 uyarýsý göster
   ?? Checked = false ? CPU moduna geç
       ?? Modelleri dispose et
       ?? Yeni modelleri CPU ile yükle

3. Tespit yapýlýr
   ?? Mevcut modeller (GPU/CPU) kullanýlýr
```

---

## ?? **Önemli Notlar**

### **1. Model Yenileme**
- CheckBox deðiþtiðinde modeller dispose edilir
- Yeni modeller GPU/CPU ayarýna göre yüklenir
- Bu iþlem ~1-2 saniye sürer

### **2. Opset 22 Uyarýsý**
- GPU modu açýldýðýnda kullanýcýya uyarý gösterilir
- "Opset 22 modelleri GPU'da çalýþmayabilir"
- Hata durumunda otomatik CPU moduna döner

### **3. Performans**
- CPU ? GPU: Modeller yeniden yüklenir (~2s bekleme)
- GPU ? CPU: Modeller yeniden yüklenir (~2s bekleme)
- Video iþlenirken deðiþtirmeyin!

---

## ?? **Test Etme**

### **1. UI Kontrolü**
```
? CheckBox görünüyor mu?
? GPU varsa enabled mi?
? Varsayýlan olarak unchecked mi?
```

### **2. Fonksiyonel Test**
```
1. Program aç ? GPU checkbox disabled (GPU yoksa)
2. GPU varsa ? checkbox enabled
3. Checkbox iþaretle ? "Model yeniden yükleniyor..."
4. Log'da "GPU: Evet" görmeli
5. Checkbox kaldýr ? "CPU modunda devam"
```

### **3. Hata Testi**
```
1. GPU checkbox iþaretle
2. Opset 22 hatasý olursa ? Otomatik CPU'ya dönmeli
3. MessageBox uyarýsý görmeli
```

---

## ?? **Özet Checklist**

- [ ] `frmALPR.Designer.cs` - `chkUseGpu` field ekle
- [ ] `frmALPR.Designer.cs` - `InitializeComponent()` içinde checkbox oluþtur
- [ ] `frmALPR.Designer.cs` - `chkSavePlates` konumunu kaydýr
- [ ] `frmALPR.cs` - `LoadOnnxModel()` içinde checkbox'ý enabled yap
- [ ] `frmALPR.cs` - `chkUseGpu_CheckedChanged` event handler ekle
- [ ] `frmALPR.cs` - `ReloadModelsWithGpuSetting()` metodu ekle
- [ ] `LicensePlateDetector.cs` - `useGpu = false` yap
- [ ] `PlateCharDetector.cs` - `useGpu = false` yap

---

**Durum:** ?? Manuel deðiþiklik gerekli  
**Dosyalar:** 4 dosya (Designer, frmALPR, LicensePlateDetector, PlateCharDetector)  
**Süre:** ~5-10 dakika  
**Sonuç:** Kullanýcý UI'dan GPU açýp kapatabilir
