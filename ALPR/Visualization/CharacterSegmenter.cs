using OpenCvSharp;
using System.Drawing;

namespace ALPR.Visualization
{
    public static class CharacterSegmenter
    {
        // DBNet map'ten ROI çýkar, karakterleri orijinal görüntü üstünde böl
        public static List<Rectangle> SegmentCharactersFromImage(Bitmap originalImage, float[] binaryMap, int mapWidth, int mapHeight, float threshold)
        {
            var rois = ExtractTextRegionsFromMap(binaryMap, mapWidth, mapHeight, originalImage.Width, originalImage.Height, threshold);
            var results = new List<Rectangle>();

            using var src = BitmapToMat(originalImage);
            foreach (var roi in rois.OrderByDescending(r => r.Width * r.Height).Take(2))
            {
                var r = new OpenCvSharp.Rect(roi.X, roi.Y, Math.Min(roi.Width, originalImage.Width - roi.X), Math.Min(roi.Height, originalImage.Height - roi.Y));
                if (r.Width <= 0 || r.Height <= 0) continue;
                using var roiMat = new Mat(src, r);
                using var gray = new Mat();
                Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);
                using var bin = new Mat();
                Cv2.Threshold(gray, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                if (Cv2.Mean(bin).Val0 < 128) Cv2.BitwiseNot(bin, bin);

                // Vertical projection
                int w = bin.Width, h = bin.Height; var proj = new int[w];
                for (int x = 0; x < w; x++) { int cnt = 0; for (int y = 0; y < h; y++) if (bin.At<byte>(y, x) > 0) cnt++; proj[x] = cnt; }
                int thr = Math.Max(2, (int)Math.Round(h * 0.12)); bool inChar = false; int start = 0; var chars = new List<Rectangle>();
                for (int x = 0; x < w; x++)
                {
                    if (!inChar && proj[x] > thr) { inChar = true; start = x; }
                    else if (inChar && (proj[x] <= thr || x == w - 1))
                    {
                        int end = (proj[x] <= thr) ? x : w; int cw = end - start;
                        if (cw >= Math.Max(3, w / 80)) chars.Add(new Rectangle(r.X + start, r.Y, cw, r.Height));
                        inChar = false;
                    }
                }

                // Fallback: connected components
                if (chars.Count < 3)
                {
                    using var labels = new Mat(); using var stats = new Mat(); using var cent = new Mat();
                    int n = Cv2.ConnectedComponentsWithStats(bin, labels, stats, cent);
                    for (int i = 1; i < n; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int lx = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int ly = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int lw = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int lh = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                        if (area < 50 || lw < 4 || lh < 10) continue;
                        chars.Add(new Rectangle(r.X + lx, r.Y + ly, lw, lh));
                    }
                }

                results.AddRange(chars);
            }

            results = results.Where(rc => rc.Width > 3 && rc.Height > 10).OrderBy(rc => rc.X).ToList();
            if (results.Count > 8) results = results.Take(8).ToList();
            return results;
        }

        private static List<Rectangle> ExtractTextRegionsFromMap(float[] map, int mapW, int mapH, int imgW, int imgH, float threshold)
        {
            var rois = new List<Rectangle>();
            using var mat32 = new Mat(mapH, mapW, MatType.CV_32F);
            for (int y = 0; y < mapH; y++)
                for (int x = 0; x < mapW; x++)
                    mat32.Set(y, x, Math.Clamp(map[y * mapW + x], 0f, 1f));
            using var mat8 = new Mat(); mat32.ConvertTo(mat8, MatType.CV_8U, 255.0);
            using var bin = new Mat(); int t = (int)Math.Round(Math.Clamp(threshold, 0.3f, 0.9f) * 255f);
            Cv2.Threshold(mat8, bin, t, 255, ThresholdTypes.Binary);
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            using var closed = new Mat(); Cv2.MorphologyEx(bin, closed, MorphTypes.Close, kernel, iterations: 1);
            Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            if (contours.Length == 0) { rois.Add(new Rectangle(0, 0, imgW, imgH)); return rois; }
            double sx = (double)imgW / mapW; double sy = (double)imgH / mapH;
            foreach (var c in contours)
            {
                var r = Cv2.BoundingRect(c);
                if (r.Width < mapW * 0.05 || r.Height < mapH * 0.05) continue;
                rois.Add(new Rectangle((int)Math.Round(r.X * sx), (int)Math.Round(r.Y * sy), (int)Math.Round(r.Width * sx), (int)Math.Round(r.Height * sy)));
            }
            return rois;
        }

        private static Mat BitmapToMat(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try { var mat = Mat.FromPixelData(bmp.Height, bmp.Width, MatType.CV_8UC3, data.Scan0, data.Stride); return mat.Clone(); }
            finally { bmp.UnlockBits(data); }
        }

        // Eski API: harita üzerinde doðrudan segmentasyon + grid fallback
        public static List<Rectangle> SegmentCharacters(float[] binaryMap, int mapWidth, int mapHeight, float threshold)
        {
            var characters = new List<Rectangle>();
            try
            {
                using var mat32 = new Mat(mapHeight, mapWidth, MatType.CV_32F);
                for (int y = 0; y < mapHeight; y++)
                    for (int x = 0; x < mapWidth; x++)
                        mat32.Set(y, x, Math.Clamp(binaryMap[y * mapWidth + x], 0f, 1f));
                using var mat8 = new Mat(); mat32.ConvertTo(mat8, MatType.CV_8U, 255.0);
                using var binary = new Mat(); int thVal = Math.Max(110, (int)Math.Round(Math.Clamp(threshold, 0.2f, 0.7f) * 255f));
                Cv2.Threshold(mat8, binary, thVal, 255, ThresholdTypes.Binary);
                using var kernel3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                using var closed = new Mat(); Cv2.MorphologyEx(binary, closed, MorphTypes.Close, kernel3, iterations: 1);
                using var thinKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 2));
                using var cleaned = new Mat(); Cv2.Erode(closed, cleaned, thinKernel, iterations: 1);

                characters = ExtractUsingContours(cleaned, preferTall: true);
                if (characters.Count < 4)
                {
                    var proj = ExtractUsingHorizontalProjection(cleaned);
                    if (proj.Count > characters.Count) characters = proj;
                }
                characters = MergeOverlapping(characters);
                characters = NormalizeToSeven(characters, mapWidth, mapHeight);
                if (characters.Count < 3) characters = CreateGridSegmentation(mapWidth, mapHeight);
            }
            catch { characters = CreateGridSegmentation(mapWidth, mapHeight); }

            characters.Sort((a, b) => a.X.CompareTo(b.X));
            if (characters.Count > 8) characters = characters.Take(8).ToList();
            return characters;
        }

        private static List<Rectangle> ExtractUsingHorizontalProjection(Mat binaryMat)
        {
            var res = new List<Rectangle>(); int w = binaryMat.Width, h = binaryMat.Height; var proj = new int[w];
            for (int x = 0; x < w; x++) { int c = 0; for (int y = 0; y < h; y++) if (binaryMat.At<byte>(y, x) > 0) c++; proj[x] = c; }
            int maxP = proj.Max(); double avg = proj.Average(); int minH = Math.Max(1, (int)Math.Round(Math.Min(maxP * 0.35, Math.Max(3, avg))));
            bool inChar = false; int s = 0; for (int x = 0; x < w; x++) { if (proj[x] > minH && !inChar) { s = x; inChar = true; } else if ((proj[x] <= minH || x == w - 1) && inChar) { int r = (proj[x] <= minH) ? x : w; int cw = r - s; if (cw >= Math.Max(2, w / 50)) { var b = FindVerticalBounds(binaryMat, s, r); if (b.height >= Math.Max(4, h / 4)) res.Add(new Rectangle(s, b.top, cw, b.height)); } inChar = false; } }
            return res;
        }

        private static List<Rectangle> ExtractUsingContours(Mat binaryMat, bool preferTall)
        {
            var res = new List<Rectangle>();
            try
            {
                Cv2.FindContours(binaryMat, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                int w = binaryMat.Width, h = binaryMat.Height; double minArea = (w * h) * 0.001;
                foreach (var c in contours)
                {
                    var r = Cv2.BoundingRect(c); double area = r.Width * r.Height; if (area < minArea) continue; float aspect = r.Width / (float)r.Height;
                    if (preferTall) { if (aspect < 0.12f || aspect > 0.9f) continue; if (r.Height < h * 0.45 || r.Height > h * 0.98) continue; if (r.Width > w * 0.45) continue; }
                    else { if (!IsValidCharacterSize(r, w, h)) continue; }
                    res.Add(new Rectangle(r.X, r.Y, r.Width, r.Height));
                }
            }
            catch { }
            res.Sort((a, b) => a.X.CompareTo(b.X)); return res;
        }

        private static List<Rectangle> MergeOverlapping(List<Rectangle> rects)
        {
            if (rects.Count == 0) return rects; rects = rects.OrderBy(r => r.X).ToList(); var merged = new List<Rectangle>(); var cur = rects[0];
            for (int i = 1; i < rects.Count; i++) { var r = rects[i]; bool ox = r.X <= cur.Right && r.Right >= cur.X; bool sy = Math.Abs(r.Y - cur.Y) < Math.Max(4, cur.Height * 0.2); if (ox && sy) { cur = Rectangle.Union(cur, r); } else { merged.Add(cur); cur = r; } } merged.Add(cur); return merged;
        }

        private static List<Rectangle> NormalizeToSeven(List<Rectangle> rects, int width, int height)
        {
            if (rects.Count == 7) return rects; if (rects.Count > 7) { while (rects.Count > 7) { int bi = -1; int bg = int.MaxValue; for (int i = 0; i < rects.Count - 1; i++) { int g = rects[i + 1].X - rects[i].Right; if (g < bg) { bg = g; bi = i; } } if (bi >= 0) { rects[bi] = Rectangle.Union(rects[bi], rects[bi + 1]); rects.RemoveAt(bi + 1); } else break; } return rects; }
            if (rects.Count > 0) { rects = rects.OrderBy(r => r.X).ToList(); int tw = rects.Last().Right - rects.First().X; int cw = Math.Max(1, tw / 7); int sx = rects.First().X; int y = rects.Select(r => r.Y).OrderBy(v => v).Skip(rects.Count / 2).First(); int h = rects.Select(r => r.Height).OrderBy(v => v).Skip(rects.Count / 2).First(); var norm = new List<Rectangle>(); for (int i = 0; i < 7; i++) { int x = sx + i * cw; int w = (i == 6) ? (rects.Last().Right - x) : cw; norm.Add(new Rectangle(Math.Clamp(x, 0, width - 1), Math.Clamp(y, 0, height - 1), Math.Min(w, width - x), Math.Min(h, height))); } return norm; }
            return rects;
        }

        private static (int top, int height) FindVerticalBounds(Mat binaryMat, int leftX, int rightX)
        {
            int h = binaryMat.Height; int top = h, bottom = 0; for (int y = 0; y < h; y++) for (int x = leftX; x < rightX && x < binaryMat.Width; x++) if (binaryMat.At<byte>(y, x) > 0) { top = Math.Min(top, y); bottom = Math.Max(bottom, y); } return top < h ? (top, bottom - top + 1) : (0, h);
        }

        private static bool IsValidCharacterSize(OpenCvSharp.Rect rect, int imageWidth, int imageHeight)
        {
            if (rect.Width < 2 || rect.Height < imageHeight / 10) return false; if (rect.Width > imageWidth * 0.5 || rect.Height > imageHeight * 0.98) return false; float aspect = rect.Width / (float)rect.Height; if (aspect < 0.15f || aspect > 1.2f) return false; return true;
        }

        // Grid fallback (7 sütun)
        private static List<Rectangle> CreateGridSegmentation(int width, int height)
        {
            var list = new List<Rectangle>(); int cw = Math.Max(1, width / 7);
            for (int i = 0; i < 7; i++) { int x = i * cw; int w = (i == 6) ? (width - x) : cw; list.Add(new Rectangle(x, 0, Math.Max(1, w), Math.Max(1, height))); }
            return list;
        }
    }
}
