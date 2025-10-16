# ?? GPU Test Ekleme Rehberi

## Constructor'a Eklenecek Metod

`frmALPR.cs` dosyasýndaki `#region Constructor` bölümünü þu þekilde güncelleyin:

```csharp
#region Constructor
public frmALPR()
{
    InitializeComponent();
    LogSystemInfo(); // ? YENÝ SATIR
    LoadOnnxModel();
    EnsurePlatesFolderExists();
}
#endregion
```

## Yeni Metod Ekle

`#region Model Loading` içine, `LoadOnnxModel()` metodundan **ÖNCE** þu metodu ekleyin:

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

        // Ýþletim Sistemi
        AddLog($"OS: {Environment.OSVersion.VersionString}");
        AddLog($"Ýþlemci: {Environment.ProcessorCount} çekirdek");
        AddLog($"RAM: {Environment.WorkingSet / 1024 / 1024} MB (Working Set)");
        AddLog($".NET: {Environment.Version}");

        // GPU Durumu
        AddLog(LogSeparator);
        AddLog("?? GPU DURUMU");
        AddLog(LogSeparator);

        if (ExecutionProviderHelper.IsGpuAvailable())
        {
            var providers = ExecutionProviderHelper.GetAvailableProviders();
            AddLog($"? GPU Kullanýlabilir");
            AddLog($"?? Desteklenen Provider'lar: {providers}");
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

## LoadOnnxModel Metodunu Güncelle

Mevcut `LoadOnnxModel()` metodunu þu þekilde güncelleyin:

```csharp
private void LoadOnnxModel()
{
    try
    {
        AddLog("MODEL YÜKLEME");
        AddLog(LogSeparator);
        
        LoadPlateDetectionModel();
        LoadCharacterDetectionModel();
        
        AddLog(LogSeparator);
        AddLog("? Sistem hazýr, resim veya video seçebilirsiniz.");
    }
    catch (Exception ex)
    {
        HandleModelLoadError(ex);
    }
}
```

---

## Beklenen Log Çýktýsý

### ? CUDA Varsa (NVIDIA GPU):
```
[10:30:15] =====================================
[10:30:15] ??? SÝSTEM BÝLGÝLERÝ
[10:30:15] =====================================
[10:30:15] OS: Microsoft Windows NT 10.0.22631.0
[10:30:15] Ýþlemci: 16 çekirdek
[10:30:15] RAM: 2048 MB (Working Set)
[10:30:15] .NET: 9.0.0
[10:30:15] =====================================
[10:30:15] ?? GPU DURUMU
[10:30:15] =====================================
[10:30:15] ? GPU Kullanýlabilir
[10:30:15] ?? Desteklenen Provider'lar: CUDA (NVIDIA GPU), DirectML (Windows GPU), CPU
[10:30:15] =====================================
[10:30:15] MODEL YÜKLEME
[10:30:15] =====================================
[10:30:15] ? GPU: CUDA (NVIDIA) kullanýlýyor
[10:30:15] ONNX (plaka tespiti) modeli yüklendi.
[10:30:15] ? GPU: CUDA (NVIDIA) kullanýlýyor
[10:30:15] ONNX (karakter tespiti) modeli yüklendi.
[10:30:15] =====================================
[10:30:15] ? Sistem hazýr, resim veya video seçebilirsiniz.
```

### ? DirectML Varsa (AMD/Intel GPU):
```
[10:30:15] =====================================
[10:30:15] ??? SÝSTEM BÝLGÝLERÝ
[10:30:15] =====================================
[10:30:15] OS: Microsoft Windows NT 10.0.22631.0
[10:30:15] Ýþlemci: 8 çekirdek
[10:30:15] RAM: 1536 MB (Working Set)
[10:30:15] .NET: 9.0.0
[10:30:15] =====================================
[10:30:15] ?? GPU DURUMU
[10:30:15] =====================================
[10:30:15] ? GPU Kullanýlabilir
[10:30:15] ?? Desteklenen Provider'lar: DirectML (Windows GPU), CPU
[10:30:15] =====================================
[10:30:15] MODEL YÜKLEME
[10:30:15] =====================================
[10:30:15] ? GPU: DirectML (Windows) kullanýlýyor
[10:30:15] ONNX (plaka tespiti) modeli yüklendi.
[10:30:15] ? GPU: DirectML (Windows) kullanýlýyor
[10:30:15] ONNX (karakter tespiti) modeli yüklendi.
[10:30:15] =====================================
[10:30:15] ? Sistem hazýr, resim veya video seçebilirsiniz.
```

### ?? GPU Yoksa (Sadece CPU):
```
[10:30:15] =====================================
[10:30:15] ??? SÝSTEM BÝLGÝLERÝ
[10:30:15] =====================================
[10:30:15] OS: Microsoft Windows NT 10.0.22631.0
[10:30:15] Ýþlemci: 4 çekirdek
[10:30:15] RAM: 1024 MB (Working Set)
[10:30:15] .NET: 9.0.0
[10:30:15] =====================================
[10:30:15] ?? GPU DURUMU
[10:30:15] =====================================
[10:30:15] ?? GPU bulunamadý - CPU modunda çalýþýlacak
[10:30:15] =====================================
[10:30:15] MODEL YÜKLEME
[10:30:15] =====================================
[10:30:15] ?? CPU kullanýlýyor (GPU bulunamadý veya kullanýlamýyor)
[10:30:15] ONNX (plaka tespiti) modeli yüklendi.
[10:30:15] ?? CPU kullanýlýyor (GPU bulunamadý veya kullanýlamýyor)
[10:30:15] ONNX (karakter tespiti) modeli yüklendi.
[10:30:15] =====================================
[10:30:15] ? Sistem hazýr, resim veya video seçebilirsiniz.
```

---

## Hýzlý Test

Build edip çalýþtýrýn:
```bash
dotnet build
dotnet run
```

Program açýldýðýnda log penceresinde sistem ve GPU bilgilerini göreceksiniz.

---

## Sorun Giderme

### "ExecutionProviderHelper bulunamadý" Hatasý
Emin olun ki:
1. `ExecutionProviderHelper.cs` dosyasý `ALPR/Detection/` klasöründe mevcut
2. Namespace doðru: `ALPR.Detection`
3. Build baþarýlý

### GPU Algýlanmýyor
1. GPU sürücüleriniz güncel mi?
2. CUDA/DirectML yüklü mü?
3. Task Manager ? Performance ? GPU kullanýmý var mý?

---

**Versiyon:** 2.1  
**Son Güncelleme:** 2024-01-XX  
**Yazar:** AI Assistant
