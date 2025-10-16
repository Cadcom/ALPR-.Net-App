# ?? frmALPR.cs Manuel Güncelleme Rehberi

## ? ADIM 1: Constructor Güncelleme

**Satýr 41-46** arasýndaki kodu þu þekilde deðiþtirin:

### ÖNCE:
```csharp
#region Constructor
public frmALPR()
{
    InitializeComponent();
    LoadOnnxModel();
    EnsurePlatesFolderExists();
}
#endregion
```

### SONRA:
```csharp
#region Constructor
public frmALPR()
{
    InitializeComponent();
    LogSystemInfo(); // ? YENÝ SATIR EKLE
    LoadOnnxModel();
    EnsurePlatesFolderExists();
}
#endregion
```

---

## ? ADIM 2: LogSystemInfo Metodu Ekle

**Satýr 49**'dan hemen önce (yani `private void LoadOnnxModel()` methodundan önce) þu metodu ekleyin:

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
        AddLog($"RAM: {Environment.WorkingSet / 1024 / 1024} MB");
        AddLog($".NET: {Environment.Version}");

        // GPU Durumu
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

## ? ADIM 3: LoadOnnxModel Güncelleme

**Satýr 50-62** arasýndaki kodu þu þekilde güncelleyin:

### ÖNCE:
```csharp
private void LoadOnnxModel()
{
    try
    {
        LoadPlateDetectionModel();
        LoadCharacterDetectionModel();
        AddLog("Sistem hazýr, resim veya video seçebilirsiniz.");
    }
    catch (Exception ex)
    {
        HandleModelLoadError(ex);
    }
}
```

### SONRA:
```csharp
private void LoadOnnxModel()
{
    try
    {
        AddLog("?? MODEL YÜKLEME");  // ? YENÝ SATIR
        AddLog(LogSeparator);        // ? YENÝ SATIR
        
        LoadPlateDetectionModel();
        LoadCharacterDetectionModel();
        
        AddLog(LogSeparator);        // ? YENÝ SATIR
        AddLog("? Sistem hazýr, resim veya video seçebilirsiniz.");
    }
    catch (Exception ex)
    {
        HandleModelLoadError(ex);
    }
}
```

---

## ? ADIM 4: LoadPlateDetectionModel & LoadCharacterDetectionModel Güncelleme (ÝSTEÐE BAÐLI)

Daha güzel log mesajlarý için emoji ekleyebilirsiniz:

### LoadPlateDetectionModel:
```csharp
private void LoadPlateDetectionModel()
{
    if (File.Exists(_modelPath))
    {
        _detector = new LicensePlateDetector(_modelPath);
        AddLog("? Plaka tespiti modeli yüklendi.");  // ? ekle
    }
    else
    {
        AddLog($"? Plaka tespiti modeli bulunamadý: {_modelPath}");  // ? ekle
    }
}
```

### LoadCharacterDetectionModel:
```csharp
private void LoadCharacterDetectionModel()
{
    if (File.Exists(_charModelPath))
    {
        _charDetector = new PlateCharDetector(_charModelPath);
        AddLog("? Karakter tespiti modeli yüklendi.");  // ? ekle
    }
    else
    {
        AddLog($"? Karakter tespiti modeli bulunamadý: {_charModelPath}");  // ? ekle
    }
}
```

---

## ?? Test

Build ve çalýþtýrýn:
```bash
dotnet build
dotnet run
```

Program açýldýðýnda logda göreceksiniz:
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
[10:30:15] ?? MODEL YÜKLEME
[10:30:15] =====================================
[10:30:15] ? Plaka tespiti modeli yüklendi.
[10:30:15] ? Karakter tespiti modeli yüklendi.
[10:30:15] =====================================
[10:30:15] ? Sistem hazýr, resim veya video seçebilirsiniz.
```

---

**Toplam Deðiþiklik:** 3 adým (1 satýr ekleme + 1 metod ekleme + birkaç satýr güncelleme)
