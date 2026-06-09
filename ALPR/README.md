# ALPR - Automatic License Plate Recognition System

ALPR is a real-time license plate recognition application that detects and reads plates from images and videos using .NET, OpenCV and ONNX models.

## Features ✨

- Image processing: JPG, PNG, BMP, TIFF
- High accuracy using ONNX deep learning models
- Multi-plate detection in a single image
- OCR integration for automatic plate text recognition
- Real-time video processing with frame skipping and FPS indicator
- Parallel processing (multi-threaded) for performance
- Automatic logging of detected plates

## Screenshots 📷

Main UI with detected plate (example):

![Main Screenshot](images/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%202026-06-09%20132935.png)

Example license plate detection (image mode):

![Plate Screenshot](images/videoCapture.png)

## Technologies 🛠️

- Framework & Language: .NET 9.0, C# 13
- UI: Windows Forms
- AI/ML: ONNX Runtime, YOLOv8 (recommended), custom OCR models
- Image processing: OpenCvSharp, System.Drawing
- NuGet packages:

```
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.17.0" />
<PackageReference Include="OpenCvSharp4" Version="4.9.0" />
<PackageReference Include="OpenCvSharp4.Extensions" Version="4.9.0" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.9.0" />
```

## Installation ⚙️

Requirements:

- Windows 10/11 (64-bit)
- .NET 9.0 SDK or Runtime
- Visual Studio 2022 (optional)
- At least 4GB RAM (GPU optional for acceleration)

Steps:

```bash
git clone https://github.com/yourusername/ALPR.git
cd ALPR
```

Place the required ONNX model files in the project folder:

- `LicencePlateDetection.onnx`    # plate detection model
- `PlateLetterExtraction.onnx`    # character recognition model

Build:

```bash
dotnet restore
dotnet build --configuration Release
```

Run:

```bash
dotnet run --project ALPR/ALPR.csproj
```

Or open the solution in Visual Studio and run with F5.

## Usage 🧭

Image mode:

1. Click `Resim Seç` (Select Image)
2. Choose an image to process
3. Configure settings: Confidence threshold (recommended: 0.60), NMS (recommended: 0.45)
4. Start processing and review logs

Video mode:

1. Click `Video Seç` (Select Video)
2. Choose your video file (MP4, AVI, MKV, MOV)
3. Set `Frame Atla` (Frame Skip): 0 = every frame, 2 = every 3rd frame (recommended), 5 = every 6th frame
4. Click `Başlat` (Start) and monitor FPS and detections

## Configuration and Tuning 🔧

- Confidence threshold — controls detection sensitivity
- NMS threshold — filters overlapping detections
- Frame skip — trade-off between speed and accuracy

Recommended presets:

- High quality: Confidence 0.70, NMS 0.45, Frame Skip 0 (FPS ~5–8)
- Balanced: Confidence 0.60, NMS 0.45, Frame Skip 2 (FPS ~15–20)
- Fast: Confidence 0.50, NMS 0.50, Frame Skip 5 (FPS ~25–35)

## Performance Benchmarks 📈

Single image processing (approximate):

- 640x480, 1 plate — ~45ms (22 FPS)
- 1280x720, 2 plates — ~75ms (13 FPS)
- 1920x1080, 3 plates — ~120ms (8 FPS)

Video processing (1280x720) examples:

- Frame skip 0: CPU ~85%, RAM ~450MB — FPS 8–10
- Frame skip 2: CPU ~60%, RAM ~400MB — FPS 18–20
- Frame skip 5: CPU ~40%, RAM ~350MB — FPS 30–35

## Project Structure 📁

```
ALPR/
  Detection/
    LicensePlateDetector.cs
    PlateCharDetector.cs
    OcrStitcher.cs
  frmALPR.cs
  Program.cs
  ALPR.csproj
  LicencePlateDetection.onnx
  PlateLetterExtraction.onnx
```

## API Summary 📚

`LicensePlateDetector` class:

```csharp
public sealed class LicensePlateDetector : IDisposable
{
    public LicensePlateDetector(string modelPath);
    public DetectionResult Detect(Bitmap originalImage, float confidenceThreshold, bool enableNms, float nmsThreshold);
}
```

`PlateCharDetector` class:

```csharp
public sealed class PlateCharDetector : IDisposable
{
    public PlateCharDetector(string modelPath, bool swapRB = false);
    public CharacterDetectionResult Detect(Bitmap roiBitmap, float confidenceThreshold, bool enableNms, float nmsThreshold);
}
```

`OcrStitcher` helper:

```csharp
public static class OcrStitcher
{
    public static string Stitch(IReadOnlyList<PlateCharDetection> predictions, string readingDirection = "left_to_right", float? tolerancePx = null);
}
```

## Development & Contribution 🚀

Add a feature branch, implement, test, and open a pull request:

```bash
git checkout -b feature/my-feature
# implement changes
git commit -m "feat: add my feature"
git push origin feature/my-feature
# Open a PR from your fork or branch
```

Commit message conventions:

- `feat:` new features
- `fix:` bug fixes
- `docs:` documentation
- `style:` formatting
- `refactor:` refactoring
- `perf:` performance improvements
- `test:` adding tests

## Known Issues ⚠️

- GPU support is not enabled by default — to enable use `Microsoft.ML.OnnxRuntime.Gpu` package
- Webcam support is not yet available (planned for v2.0)
- Some video codecs may not be supported — convert with FFmpeg if needed

## Roadmap 🛣️

- v2.0: Webcam real-time support, GPU acceleration, plate database, REST API
- v2.1: Multi-language OCR, video recording, auto model updates
- v3.0: Model training UI, batch processing, analytics

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Acknowledgements

- OpenCV
- ONNX Runtime
- YOLOv8
