# ALPR - Automatic License Plate Recognition System

ALPR is a real-time license plate recognition application that detects and reads plates from images and videos using .NET, OpenCV and ONNX models.

## Features

- Image processing: JPG, PNG, BMP, TIFF
- High accuracy using ONNX deep learning models
- Multi-plate detection in a single image
- OCR integration for automatic plate text recognition
- Real-time video processing with frame skipping and FPS indicator
- Parallel processing (multi-threaded) for performance
- Automatic logging of detected plates

## Screenshots

Main UI with detected plate (example):

![Main Screenshot](../images/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%202026-06-09%20132935.png)

Example license plate detection (image mode):

![Plate Screenshot](../images/videoCapture.png)

## Quick Start

1. Build and run the project in the `ALPR` folder using the instructions in the repository root.
2. Models expected in the repo `models/` folder at project runtime.

See the repository root README for full instructions and details.
