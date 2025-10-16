using OpenCvSharp;
using System.Drawing;

namespace ALPR.Visualization
{
    public static class DBNetVisualizer
    {
        private static readonly Color[] ModelBorderColors = new Color[]
        {
            Color.FromArgb(200, 178, 34, 34),
            Color.FromArgb(200, 34, 139, 34),
            Color.FromArgb(200, 25, 25, 112),
            Color.FromArgb(200, 255, 69, 0),
        };

        public static Bitmap VisualizeMultipleModels(Bitmap originalImage, List<ModelVisualizationData> modelResults)
        {
            var result = new Bitmap(originalImage);
            using var g = Graphics.FromImage(result);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            for (int i = 0; i < modelResults.Count; i++)
            {
                DrawCharacterBoxes(g, originalImage, modelResults[i], i % ModelBorderColors.Length);
            }
            return result;
        }

        private static void DrawCharacterBoxes(Graphics g, Bitmap original, ModelVisualizationData data, int colorIndex)
        {
            if (data.BinaryMap == null || data.BinaryMap.Length == 0) return;
            var th = Math.Max(0.2f, data.Threshold * 0.5f);
            var rects = CharacterSegmenter.SegmentCharactersFromImage(original, data.BinaryMap, data.MapWidth, data.MapHeight, th);
            using var pen = new Pen(ModelBorderColors[colorIndex], 2);
            for (int i = 0; i < Math.Min(rects.Count, 8); i++)
            {
                var r = rects[i]; if (r.Width < 10 || r.Height < 15) continue; g.DrawRectangle(pen, r);
                using var font = new Font("Arial", 8, FontStyle.Bold);
                using var brush = new SolidBrush(ModelBorderColors[colorIndex]);
                g.DrawString((i + 1).ToString(), font, brush, r.X + 2, r.Y + 2);
            }
        }
    }

    public class ModelVisualizationData
    {
        public string ModelName { get; set; } = string.Empty;
        public float[]? BinaryMap { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public float Threshold { get; set; }
        public double TextCoverage { get; set; }
        public int ContourCount { get; set; }
        public long InferenceTime { get; set; }
    }
}
