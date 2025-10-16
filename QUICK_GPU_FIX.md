# ?? GPU Test Eklemek Ýçin Hýzlý Rehber

## ?? Yapmanýz Gerekenler (3 Basit Adým)

### 1?? Constructor'a 1 Satýr Ekle

`frmALPR.cs` dosyasýnda **satýr 44** öncesine þunu ekle:

```csharp
public frmALPR()
{
    InitializeComponent();
    LogSystemInfo(); // ? BU SATIRI EKLE
    LoadOnnxModel();
    EnsurePlatesFolderExists();
}
```

---

### 2?? LogSystemInfo Metodunu Ekle

`LoadOnnxModel()` metodundan **HEMEN ÖNCE** þu metodu ekle:

```csharp
/// <summary>
/// Sistem ve GPU bilgilerini log'a yazar
/// </summary>
private void LogSystemInfo()
{
    try
    {
        AddLog(LogSeparator);
        AddLog("??? SÝSTEM BÝLGÝLERÝ");
        AddLog(LogSeparator);

        AddLog($"OS: {Environment.OSVersion.VersionString}");
        AddLog($"Ýþlemci: {Environment.ProcessorCount} çekirdek");
        AddLog($"RAM: {Environment.WorkingSet / 1024 / 1024} MB");
        AddLog($".NET: {Environment.Version}");

        AddLog(LogSeparator);
        AddLog("?? GPU DURUMU");
        AddLog(LogSeparator);

        if (ExecutionProviderHelper.IsGpuAvailable())
        {
            var providers = ExecutionProviderHelper.GetAvailableProviders();
            AddLog($"? GPU Kullanýlabilir");
            AddLog($"?? Desteklenen: {providers}");
        }
        else
        {
            AddLog($"?? GPU bulunamadý - CPU modunda çalýþýlacak");
        }

        AddLog(LogSeparator);
    }
    catch (Exception ex)
    {
        AddLog($"?? Sistem bilgisi alýnamadý: {ex.Message}");
    }
}
```

---

### 3?? LoadOnnxModel'i Güncelle (Ýsteðe Baðlý - Daha Güzel Log Ýçin)

Mevcut `LoadOnnxModel()` metodunu þu þekilde güncelle:

```csharp
private void LoadOnnxModel()
{
    try
    {
        AddLog("?? MODEL YÜKLEME");  // ? EKLE
        AddLog(LogSeparator);        // ? EKLE
        
        LoadPlateDetectionModel();
        LoadCharacterDetectionModel();
        
        AddLog(LogSeparator);        // ? EKLE
        AddLog("? Sistem hazýr, resim veya video seçebilirsiniz.");
    }
    catch (Exception ex)
    {
        HandleModelLoadError(ex);
    }
}
```

---

## ? Test

```bash
dotnet build
dotnet run
```

---

## ?? Beklenen Çýktý

### ? GPU Varsa:
```
[10:30:15] =====================================
[10:30:15] ??? SÝSTEM BÝLGÝLERÝ
[10:30:15] =====================================
[10:30:15] OS: Microsoft Windows NT 10.0.22631.0
[10:30:15] Ýþlemci: 16 çekirdek
[10:30:15] RAM: 2048 MB
[10:30:15] .NET: 9.0.0
[10:30:15] =====================================
[10:30:15] ?? GPU DURUMU
[10:30:15] =====================================
[10:30:15] ? GPU Kullanýlabilir
[10:30:15] ?? Desteklenen: CUDA (NVIDIA GPU), CPU
[10:30:15] =====================================
```

### ?? GPU Yoksa:
```
[10:30:15] =====================================
[10:30:15] ??? SÝSTEM BÝLGÝLERÝ
[10:30:15] =====================================
[10:30:15] ?? GPU bulunamadý - CPU modunda çalýþýlacak
```

---

## ?? Dosyalar

- ? `GPU_MANUAL_PATCH.md` - Detaylý rehber
- ? `ExecutionProviderHelper.cs` - GPU helper (mevcut)
- ?? `frmALPR.cs` - Manuel güncelleme gerekli

---

**Toplam Süre:** ~2 dakika  
**Zorluk:** ? Çok Kolay
