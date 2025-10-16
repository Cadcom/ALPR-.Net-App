using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALPR.Detection
{
    /// <summary>
    /// OCR sonuçlarýný sýralayýp birleþtiren optimize edilmiþ yardýmcý sýnýf.
    /// - Ülke baðýmsýz: Eðik/perspektif plakalar için plaka eksenini PCA ile tahmin eder.
    /// - 1 veya 2 satýrý y' eksenindeki en büyük boþluða göre ayýrýr.
    /// - Her satýrý x' eksenine göre (soldan-saða) sýralar ve satýrlarý yukarýdan-aþaðýya birleþtirir.
    /// </summary>
    public static class OcrStitcher
    {
        private const string LeftToRight = "left_to_right";
        private const string TopToBottom = "top_to_bottom";
        private const double RowGapHeightFactor = 0.4; // Daha yüksek eþik: boþluk > 0.4h ise 2 satýr varsay

        // Debug modu için static field
        public static bool EnableDebug { get; set; } = false;
        public static Action<string>? DebugLogger { get; set; }

        public static string Stitch(
            IReadOnlyList<PlateCharDetection> predictions,
            string readingDirection = LeftToRight,
            float? tolerancePx = null)
        {
            if (predictions == null || predictions.Count == 0)
                return string.Empty;

            return readingDirection.ToLowerInvariant() switch
            {
                LeftToRight => StitchPlateAligned(predictions),
                TopToBottom => StitchTopToBottomPlateAligned(predictions),
                _ => throw new ArgumentException(
                    $"Desteklenmeyen okuma yönü: '{readingDirection}'. 'left_to_right' veya 'top_to_bottom' kullanýn.",
                    nameof(readingDirection))
            };
        }

        private static string StitchPlateAligned(IReadOnlyList<PlateCharDetection> preds)
        {
            if (preds.Count == 1)
                return preds[0].Class ?? string.Empty;

            var avgH = preds.Average(p => (double)p.Height);
            LogDebug($"?? Karakter sayýsý: {preds.Count}, Ortalama yükseklik: {avgH:F2}px");

            // Orijinal koordinatlarý logla
            if (EnableDebug)
            {
                LogDebug("?? Orijinal karakterler (X, Y):");
                for (int i = 0; i < preds.Count; i++)
                {
                    var p = preds[i];
                    var cx = p.X + p.Width / 2.0;
                    var cy = p.Y + p.Height / 2.0;
                    LogDebug($"  [{i}] '{p.Class}' @ ({cx:F1}, {cy:F1}) - size: {p.Width}x{p.Height}");
                }
            }

            // Önce orijinal Y koordinatlarýna göre basit satýr gruplamasi yapalým
            var simpleRowGrouping = GroupByOriginalY(preds, avgH);
            
            if (simpleRowGrouping.Count == 1)
            {
                LogDebug("?? Tek satýr tespit edildi (basit yöntem)");
                var singleRow = simpleRowGrouping[0];
                var line = singleRow
                    .OrderBy(p => p.X + p.Width / 2.0) // Soldan saða sýrala
                    .Select(p => p.Class ?? string.Empty)
                    .ToList();
                
                var result = string.Concat(line);
                LogDebug($"? Sonuç (tek satýr): '{result}' - Sýralama: {string.Join(" ", line.Select((c, i) => $"[{i}]{c}"))}");
                return result;
            }
            else if (simpleRowGrouping.Count == 2)
            {
                LogDebug("?? Ýki satýr tespit edildi (basit yöntem)");
                
                // Üst ve alt satýrlarý Y koordinatýna göre belirle
                var topRow = simpleRowGrouping[0];
                var bottomRow = simpleRowGrouping[1];
                
                // Her satýrý X koordinatýna göre sýrala
                var row1Sorted = topRow
                    .OrderBy(p => p.X + p.Width / 2.0)
                    .Select(p => p.Class ?? string.Empty);
                    
                var row2Sorted = bottomRow
                    .OrderBy(p => p.X + p.Width / 2.0)
                    .Select(p => p.Class ?? string.Empty);

                LogDebug($"  Satýr 1 ({topRow.Count} karakter): {string.Join("", row1Sorted)}");
                LogDebug($"  Satýr 2 ({bottomRow.Count} karakter): {string.Join("", row2Sorted)}");

                var result = string.Concat(row1Sorted) + string.Concat(row2Sorted);
                LogDebug($"? Sonuç (iki satýr): '{result}'");
                return result;
            }

            // Eðer basit yöntem iþe yaramazsa, PCA yöntemini kullan
            LogDebug("?? Basit yöntem iþe yaramadý, PCA yöntemine geçiliyor...");
            return StitchWithPCA(preds, avgH);
        }

        private static List<List<PlateCharDetection>> GroupByOriginalY(IReadOnlyList<PlateCharDetection> preds, double avgH)
        {
            // Y koordinatlarýna göre sýrala
            var sortedByY = preds.OrderBy(p => p.Y + p.Height / 2.0).ToList();
            
            LogDebug("?? Orijinal Y koordinatlarýna göre satýr gruplamasi:");
            foreach (var p in sortedByY)
            {
                LogDebug($"  '{p.Class}' Y={p.Y + p.Height / 2.0:F1}");
            }

            var groups = new List<List<PlateCharDetection>>();
            var currentGroup = new List<PlateCharDetection> { sortedByY[0] };
            
            double threshold = avgH * 0.6; // Y farký bu deðerden fazlaysa yeni satýr
            LogDebug($"?? Y farký eþiði: {threshold:F2}px (avgH * 0.6)");

            for (int i = 1; i < sortedByY.Count; i++)
            {
                var prev = sortedByY[i - 1];
                var curr = sortedByY[i];
                
                var prevY = prev.Y + prev.Height / 2.0;
                var currY = curr.Y + curr.Height / 2.0;
                var yDiff = currY - prevY;
                
                LogDebug($"  Y farký '{prev.Class}' ? '{curr.Class}': {yDiff:F2}px");
                
                if (yDiff > threshold)
                {
                    LogDebug($"    ? Yeni satýr baþlýyor (fark > {threshold:F2})");
                    groups.Add(currentGroup);
                    currentGroup = new List<PlateCharDetection> { curr };
                }
                else
                {
                    LogDebug($"    ? Ayný satýrda devam (fark ? {threshold:F2})");
                    currentGroup.Add(curr);
                }
            }
            
            groups.Add(currentGroup);
            
            LogDebug($"?? {groups.Count} satýr grubu oluþturuldu:");
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var chars = string.Join("", group.Select(p => p.Class));
                var avgY = group.Average(p => p.Y + p.Height / 2.0);
                LogDebug($"  Grup {i + 1}: '{chars}' (Ort. Y: {avgY:F1})");
            }
            
            return groups;
        }

        private static string StitchWithPCA(IReadOnlyList<PlateCharDetection> preds, double avgH)
        {
            // PCA ile plaka yönünü (x' ekseni) bul
            var mean = GetMeanCenter(preds);
            LogDebug($"?? Merkez nokta: ({mean.X:F2}, {mean.Y:F2})");

            var axis = GetPrincipalAxis(preds, mean); // aday x'
            var perp = new Vector2(-axis.Y, axis.X);   // aday y'
            LogDebug($"?? Ýlk PCA eksenleri - axis: ({axis.X:F4}, {axis.Y:F4}), perp: ({perp.X:F4}, {perp.Y:F4})");

            // x' daha yatay olsun: gerekirse eksenleri takas et
            if (Math.Abs(perp.X) > Math.Abs(axis.X))
            {
                axis = perp;
                perp = new Vector2(-axis.Y, axis.X);
                LogDebug("?? Eksenler takasý yapýldý (yatay eksen için)");
            }

            // Soldan-saða artan x' garantisi (saða doðru pozitif)
            if (axis.X < 0)
            {
                axis = new Vector2(-axis.X, -axis.Y);
                perp = new Vector2(-perp.X, -perp.Y);
                LogDebug("???? X ekseni çevrildi (soldan-saða pozitif için)");
            }

            // Karakter merkezlerini plaka-aligned koordinatlarýna projekte et
            var pts = preds.Select(p => new PlatePoint(p, Project(p, mean, axis, perp))).ToList();

            // Y' ekseninin yönünü belirle - DOÐRUDAN ORÝJÝNAL Y ile KARÞILAÞTIR
            var minOriginalY = preds.Min(p => p.Y + p.Height / 2.0);
            var maxOriginalY = preds.Max(p => p.Y + p.Height / 2.0);
            
            // En üstteki ve en alttaki karakterleri bul
            var topChar = preds.OrderBy(p => p.Y + p.Height / 2.0).First();
            var bottomChar = preds.OrderBy(p => p.Y + p.Height / 2.0).Last();
            
            // Projekte edilmiþ Y' deðerlerini bul
            var topCharYp = pts.First(pt => pt.Detection == topChar).Yp;
            var bottomCharYp = pts.First(pt => pt.Detection == bottomChar).Yp;
            
            LogDebug($"?? Y kontrolü: Top '{topChar.Class}' Y={topChar.Y + topChar.Height / 2.0:F1} ? Y'={topCharYp:F2}");
            LogDebug($"?? Y kontrolü: Bottom '{bottomChar.Class}' Y={bottomChar.Y + bottomChar.Height / 2.0:F1} ? Y'={bottomCharYp:F2}");
            
            // Eðer üstteki karakterin Y' deðeri alttakinden BÜYÜKSE, Y eksenini çevir
            if (topCharYp > bottomCharYp)
            {
                perp = new Vector2(-perp.X, -perp.Y);
                pts = preds.Select(p => new PlatePoint(p, Project(p, mean, axis, perp))).ToList();
                LogDebug($"???? Y ekseni çevrildi! (Top Y' {topCharYp:F2} > Bottom Y' {bottomCharYp:F2})");
            }
            else
            {
                LogDebug($"? Y ekseni yönü doðru (Top Y' {topCharYp:F2} < Bottom Y' {bottomCharYp:F2})");
            }

            LogDebug($"? Final eksenler - axis: ({axis.X:F4}, {axis.Y:F4}), perp: ({perp.X:F4}, {perp.Y:F4})");

            // Projekte edilmiþ koordinatlarý logla
            if (EnableDebug)
            {
                LogDebug("?? Projekte edilmiþ koordinatlar (X', Y'):");
                for (int i = 0; i < pts.Count; i++)
                {
                    var pt = pts[i];
                    LogDebug($"  [{i}] '{pt.Detection.Class}' @ (X'={pt.Xp:F2}, Y'={pt.Yp:F2})");
                }
            }

            // y' ekseninde en büyük boþluk ile 1/2 satýr kararýný ver
            var (twoRows, splitY) = DetectRows(pts, avgH);

            if (!twoRows)
            {
                LogDebug("?? Tek satýr tespit edildi");
                var line = pts
                    .OrderBy(pt => pt.Xp)
                    .ThenBy(pt => pt.Yp)
                    .Select(pt => pt.Detection.Class ?? string.Empty)
                    .ToList();
                
                var result = string.Concat(line);
                LogDebug($"? Sonuç (tek satýr): '{result}' - Sýralama: {string.Join(" ", line.Select((c, i) => $"[{i}]{c}"))}");
                return result;
            }
            else
            {
                LogDebug($"?? Ýki satýr tespit edildi! Split Y': {splitY:F2}");
                
                var row1List = pts.Where(pt => pt.Yp <= splitY).OrderBy(pt => pt.Xp).ThenBy(pt => pt.Yp).ToList();
                var row2List = pts.Where(pt => pt.Yp > splitY).OrderBy(pt => pt.Xp).ThenBy(pt => pt.Yp).ToList();

                LogDebug($"  Satýr 1 ({row1List.Count} karakter): Y' ? {splitY:F2}");
                foreach (var pt in row1List)
                {
                    LogDebug($"    '{pt.Detection.Class}' @ (X'={pt.Xp:F2}, Y'={pt.Yp:F2})");
                }

                LogDebug($"  Satýr 2 ({row2List.Count} karakter): Y' > {splitY:F2}");
                foreach (var pt in row2List)
                {
                    LogDebug($"    '{pt.Detection.Class}' @ (X'={pt.Xp:F2}, Y'={pt.Yp:F2})");
                }

                var row1 = row1List.Select(pt => pt.Detection.Class ?? string.Empty);
                var row2 = row2List.Select(pt => pt.Detection.Class ?? string.Empty);
                
                var result = string.Concat(row1) + string.Concat(row2);
                LogDebug($"? Sonuç (iki satýr): '{result}'");
                LogDebug($"   Satýr 1: '{string.Concat(row1)}'");
                LogDebug($"   Satýr 2: '{string.Concat(row2)}'");
                
                return result;
            }
        }

        private static string StitchTopToBottomPlateAligned(IReadOnlyList<PlateCharDetection> preds)
        {
            var mean = GetMeanCenter(preds);
            var axis = GetPrincipalAxis(preds, mean);
            var perp = new Vector2(-axis.Y, axis.X);

            // x' daha yatay olsun, saða pozitif
            if (Math.Abs(perp.X) > Math.Abs(axis.X))
            {
                axis = perp;
                perp = new Vector2(-axis.Y, axis.X);
            }
            if (axis.X < 0)
            {
                axis = new Vector2(-axis.X, -axis.Y);
                perp = new Vector2(-perp.X, -perp.Y);
            }

            var pts = preds.Select(p => new PlatePoint(p, Project(p, mean, axis, perp))).ToList();
            
            // Y ekseni yönü kontrolü - en üst ve en alt karakter
            var topChar = preds.OrderBy(p => p.Y + p.Height / 2.0).First();
            var bottomChar = preds.OrderBy(p => p.Y + p.Height / 2.0).Last();
            var topCharYp = pts.First(pt => pt.Detection == topChar).Yp;
            var bottomCharYp = pts.First(pt => pt.Detection == bottomChar).Yp;
            
            if (topCharYp > bottomCharYp)
            {
                perp = new Vector2(-perp.X, -perp.Y);
                pts = preds.Select(p => new PlatePoint(p, Project(p, mean, axis, perp))).ToList();
            }

            var ordered = pts
                .OrderBy(pt => pt.Yp)
                .ThenBy(pt => pt.Xp)
                .Select(pt => pt.Detection.Class ?? string.Empty);
            return string.Concat(ordered);
        }

        private static (bool twoRows, double splitY) DetectRows(List<PlatePoint> pts, double avgH)
        {
            var byY = pts.OrderBy(p => p.Yp).ToList();
            double maxGap = 0;
            int maxIdx = -1;

            LogDebug($"?? Satýr tespiti baþlýyor... Eþik: {RowGapHeightFactor * avgH:F2}px (avgH * {RowGapHeightFactor})");

            for (int i = 0; i < byY.Count - 1; i++)
            {
                var gap = byY[i + 1].Yp - byY[i].Yp;
                LogDebug($"  Gap[{i}?{i + 1}]: {gap:F2}px ('{byY[i].Detection.Class}' ? '{byY[i + 1].Detection.Class}')");
                
                if (gap > maxGap)
                {
                    maxGap = gap;
                    maxIdx = i;
                }
            }

            LogDebug($"?? En büyük gap: {maxGap:F2}px @ index {maxIdx}");

            if (maxGap > RowGapHeightFactor * avgH && maxIdx >= 0)
            {
                var split = (byY[maxIdx].Yp + byY[maxIdx + 1].Yp) / 2.0;
                LogDebug($"? Ýki satýr tespit edildi! Gap: {maxGap:F2} > Eþik: {RowGapHeightFactor * avgH:F2}");
                return (true, split);
            }
            
            LogDebug($"? Tek satýr: Gap {maxGap:F2} ? Eþik {RowGapHeightFactor * avgH:F2}");
            return (false, 0);
        }

        private static (double X, double Y) GetMeanCenter(IReadOnlyList<PlateCharDetection> preds)
        {
            double sx = 0, sy = 0;
            int n = preds.Count;

            foreach (var p in preds)
            {
                sx += p.X + p.Width / 2.0;
                sy += p.Y + p.Height / 2.0;
            }
            return (sx / n, sy / n);
        }

        // En büyük özdeðere karþýlýk gelen 2x2 PCA yön vektörü (kapalý form)
        private static Vector2 GetPrincipalAxis(IReadOnlyList<PlateCharDetection> preds, (double X, double Y) mean)
        {
            double sxx = 0, syy = 0, sxy = 0;

            foreach (var p in preds)
            {
                var cx = p.X + p.Width / 2.0 - mean.X;
                var cy = p.Y + p.Height / 2.0 - mean.Y;
                sxx += cx * cx;
                syy += cy * cy;
                sxy += cx * cy;
            }

            var theta = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
            return new Vector2(Math.Cos(theta), Math.Sin(theta));
        }

        private static (double Xp, double Yp) Project(PlateCharDetection p, (double X, double Y) mean, Vector2 axis, Vector2 perp)
        {
            var cx = p.X + p.Width / 2.0 - mean.X;
            var cy = p.Y + p.Height / 2.0 - mean.Y;
            var xp = cx * axis.X + cy * axis.Y;
            var yp = cx * perp.X + cy * perp.Y;
            return (xp, yp);
        }

        private static void LogDebug(string message)
        {
            if (EnableDebug)
            {
                DebugLogger?.Invoke(message);
            }
        }

        private readonly record struct Vector2(double X, double Y);

        private readonly record struct PlatePoint(PlateCharDetection Detection, (double Xp, double Yp) Local)
        {
            public double Xp => Local.Xp;
            public double Yp => Local.Yp;
        }
    }
}