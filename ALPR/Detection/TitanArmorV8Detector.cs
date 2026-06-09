using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;

namespace ALPR.Detection
{
    public class PlateCharDetail
    {
        public char Character { get; set; }
        public float Confidence { get; set; }
    }

    public class TitanV8ModelResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }

        /// <summary>"Slot" ya da "CTC" — hangi head seçildi.</summary>
        public string ModelHead { get; set; } = string.Empty;

        /// <summary>Chooser karar nedeni — debug için faydalı.</summary>
        public string ChooserReason { get; set; } = string.Empty;

        public List<PlateCharDetail> Details { get; set; } = new List<PlateCharDetail>();

        public bool IsSecure => Details.Count > 0 && Details.All(x => x.Confidence >= TitanArmorV8Detector.SecureThreshold);
    }

    public class TitanArmorV8Detector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string[] _outputNames;

        private readonly CLAHE _clahe;

        // ── Model sabitleri ──────────────────────────────────────────────────────
        private const int ImgW = 192;
        private const int ImgH = 96;
        private const int MaxLabelLen = 12;
        private const int PadIdx = 0; // index 0 = boşluk / padding

        // ── Eşik değerleri ──────────────────────────────────────────────────────
        public const float SecureThreshold = 0.85f;

        // ALG11 fallback: neither_secure durumunda min_conf farkı eşiği
        private const float NeitherMinDiffThr = 0.09f;

        // ALG11: 2+ fark pozisyonunda diff-pozisyon ortalama farkı eşiği
        private const float CharMultiDiffThr = 0.05f;

        // ALG11: slot = ctc + 1 karakter bucket'ı için ek karakter eşiği
        private const float StrongInsertThr = 0.88f;
        private const float MediumInsertThr = 0.65f;

        // ALG11: küçük fark tie-break / override eşikleri
        private const float TieBreakAggSlotThr = 0.015f;
        private const float CharPosCtcAggOverrideThr = -0.03f;
        private const float SlotPlus1StrongConfDiffFloor = -0.08f;
        private const float SlotPlus1MediumConfDiffFloor = -0.04f;
        private const float CtcHighConfThr = 0.88f;

        // ── ALG11 YENİ EŞİKLER ──────────────────────────────────────────────────
        // [DEĞİŞİKLİK 1] disagree_char_pos_slot kolu:
        //   pos_diff >= CharPosSlotThresh → slot (yüksek char güveni)
        //   pos_diff < CharPosSlotThresh AND conf_diff >= AggExceptionThr → slot (aggregate üstünlük)
        //   diğer durumda → CTC (küçük sinyal → CTC %90 güvenilir)
        private const float CharPosSlotThresh = 0.15f;
        private const float AggExceptionThr = 0.05f;

        // [DEĞİŞİKLİK 2] ctc_secure_only + length-aware kural:
        //   ctc_secure && !slot_secure && slot_len > ctc_len && slot_min >= SecureLenSlotMinThr → slot
        //   (CTC bazen son karakteri drop eder ve "secure" görünür; slot_min yeterliyse slot tercih et)
        private const float SecureLenSlotMinThr = 0.68f;

        // ── Kelime dağarcığı ─────────────────────────────────────────────────────
        // İndeks 0 = pad (boşluk), 1-10 = rakamlar, 11-36 = harfler
        private const string Vocab = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // ── Dahili decode sonucu ─────────────────────────────────────────────────
        private sealed class DecodeResult
        {
            public string Text { get; init; } = string.Empty;
            public float AvgConf { get; init; }
            public float MinConf { get; init; }
            public bool IsSecure { get; init; }
            public bool InternalPad { get; init; }
            public IReadOnlyList<float> CharConfs { get; init; } = Array.Empty<float>();
            public List<PlateCharDetail> Details { get; init; } = new();
        }

        // ────────────────────────────────────────────────────────────────────────

        public TitanArmorV8Detector(string modelPath, bool useGpu = false)
        {
            var options = useGpu
                ? ExecutionProviderHelper.CreateOptimizedSessionOptions(true)
                : new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                };

            _session = new InferenceSession(modelPath, options);
            _inputName = _session.InputMetadata.Keys.First();
            _outputNames = _session.OutputMetadata.Keys.ToArray();
            _clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8));
        }

        public TitanV8ModelResult Predict(Mat inputMat)
        {
            var input = Preprocess(inputMat);

            using var inputVal = OrtValue.CreateTensorValueFromMemory(
                input, new long[] { 1, ImgH, ImgW, 1 });

            using var outputs = _session.Run(
                new RunOptions(),
                new Dictionary<string, OrtValue> { [_inputName] = inputVal },
                _outputNames);

            // Model outputs'un her zaman en az 2 çıktısı var (slot_head, ctc_head)
            // Ama adlar değişebilir, bu yüzden index ile erişiyoruz
            if (outputs.Count < 2)
                throw new InvalidOperationException($"Model expected 2 outputs, got {outputs.Count}");

            var slotOutput = FindOutputByHead(outputs, _outputNames, "slot");
            var ctcOutput = FindOutputByHead(outputs, _outputNames, "ctc");

            var slotShape = slotOutput.GetTensorTypeAndShape().Shape;
            var ctcShape = ctcOutput.GetTensorTypeAndShape().Shape;
            ValidateShape(slotShape, "slot");
            ValidateShape(ctcShape, "ctc");

            int slotSeqLen = Math.Min((int)slotShape[1], MaxLabelLen);
            int slotVocab = (int)slotShape[2];
            int ctcSeqLen = (int)ctcShape[1];
            int ctcVocab = (int)ctcShape[2];

            var slotSpan = slotOutput.GetTensorDataAsSpan<float>();
            var ctcSpan = ctcOutput.GetTensorDataAsSpan<float>();

            var slot = DecodeSlot(slotSpan, slotSeqLen, slotVocab);
            var ctc = DecodeCTC(ctcSpan, ctcSeqLen, ctcVocab);

            return ChoosePrediction(slot, ctc);
        }

        private static OrtValue FindOutputByHead(IDisposableReadOnlyCollection<OrtValue> outputs, string[] outputNames, string headName)
        {
            int byNameIndex = Array.FindIndex(outputNames, n =>
                n.Contains(headName, StringComparison.OrdinalIgnoreCase));
            if (byNameIndex >= 0 && byNameIndex < outputs.Count)
                return outputs[byNameIndex];

            // İsim yoksa shape'ten tahmin:
            // slot: genelde seq kısa (12-16), ctc: seq daha uzun (örn. 48/64/96/128)
            int candidateIdx = -1;
            long bestScore = long.MinValue;
            for (int i = 0; i < outputs.Count; i++)
            {
                var shape = outputs[i].GetTensorTypeAndShape().Shape;
                if (shape.Length < 3)
                    continue;

                long seq = shape[1];
                long score = headName.Equals("slot", StringComparison.OrdinalIgnoreCase) ? -seq : seq;
                if (score > bestScore)
                {
                    bestScore = score;
                    candidateIdx = i;
                }
            }

            if (candidateIdx < 0)
                throw new InvalidOperationException($"Could not locate '{headName}' output.");

            return outputs[candidateIdx];
        }

        private static void ValidateShape(long[] shape, string head)
        {
            if (shape.Length < 3 || shape[1] <= 0 || shape[2] <= 0)
                throw new InvalidOperationException($"Invalid {head} output shape: [{string.Join(", ", shape)}]");
        }

        // ── Ön işlem ─────────────────────────────────────────────────────────────

        private float[] Preprocess(Mat img)
        {
            using var gray = new Mat();
            if (img.Channels() == 3)
                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
            else
                img.CopyTo(gray);

            using var clahed = new Mat();
            //using var clahe = Cv2.CreateCLAHE(2.5, new OpenCvSharp.Size(8, 8));
            _clahe.Apply(gray, clahed);

            double ratio = Math.Min((double)ImgW / clahed.Cols, (double)ImgH / clahed.Rows);
            int newW = Math.Max(1, (int)Math.Round(clahed.Cols * ratio));
            int newH = Math.Max(1, (int)Math.Round(clahed.Rows * ratio));

            var interp = ratio < 1.0 ? InterpolationFlags.Area : InterpolationFlags.Linear;
            using var resized = new Mat();
            Cv2.Resize(clahed, resized, new OpenCvSharp.Size(newW, newH), 0, 0, interp);

            int top = (ImgH - newH) / 2;
            int bottom = ImgH - newH - top;
            int left = (ImgW - newW) / 2;
            int right = ImgW - newW - left;

            using var padded = new Mat();
            Cv2.CopyMakeBorder(resized, padded, top, bottom, left, right,
                               BorderTypes.Constant, Scalar.Black);

            using var floatMat = new Mat();
            padded.ConvertTo(floatMat, MatType.CV_32FC1, 1.0 / 255.0);

            var tensor = new float[ImgH * ImgW];
            var indexer = floatMat.GetGenericIndexer<float>();
            int idx = 0;
            for (int y = 0; y < ImgH; y++)
                for (int x = 0; x < ImgW; x++)
                    tensor[idx++] = indexer[y, x];

            return tensor;
        }

        // ── Slot decode ──────────────────────────────────────────────────────────
        //
        // VocabularyProjection → activation="softmax"
        // Çıktı doğrudan olasılık [0,1], exp() GEREKMİYOR.
        // Shape: [1, MaxLabelLen, SlotVocab]
        //
        private DecodeResult DecodeSlot(ReadOnlySpan<float> data, int maxLabelLen, int slotVocab)
        {
            var ids = new int[maxLabelLen];
            var tokenConf = new float[maxLabelLen];

            for (int s = 0; s < maxLabelLen; s++)
            {
                int bestIdx = 0;
                int rowOffset = s * slotVocab;
                float bestProb = data[rowOffset];
                for (int v = 1; v < slotVocab; v++)
                {
                    float p = data[rowOffset + v];
                    if (p > bestProb) { bestProb = p; bestIdx = v; }
                }
                ids[s] = bestIdx;
                tokenConf[s] = bestProb; // softmax → doğrudan olasılık
            }

            // İlk pad-olmayan token'ı bul
            int first = -1;
            for (int i = 0; i < maxLabelLen; i++)
                if (ids[i] != PadIdx) { first = i; break; }

            if (first < 0)
            {
                return new DecodeResult
                {
                    Text = string.Empty,
                    AvgConf = 0f,
                    MinConf = 0f,
                    IsSecure = false,
                    InternalPad = false,
                    CharConfs = Array.Empty<float>(),
                    Details = new()
                };
            }

            // Metin bölgesinin sonunu (cutoff) bul ve internal_pad'i tespit et
            int cutoff = maxLabelLen;
            bool internalPad = false;

            for (int i = first; i < maxLabelLen; i++)
            {
                if (ids[i] == PadIdx)
                {
                    cutoff = i;
                    // cutoff'tan sonra pad-olmayan token var mı?
                    for (int j = i + 1; j < maxLabelLen; j++)
                        if (ids[j] != PadIdx) { internalPad = true; break; }
                    break;
                }
            }

            var charConfs = new List<float>(cutoff - first);
            var details = new List<PlateCharDetail>(cutoff - first);
            var sb = new System.Text.StringBuilder(cutoff - first);

            for (int i = first; i < cutoff; i++)
            {
                int vocabIdx = ids[i];
                if (vocabIdx < 0 || vocabIdx >= Vocab.Length)
                    continue;

                char c = Vocab[vocabIdx];
                float cf = tokenConf[i];
                sb.Append(c);
                charConfs.Add(cf);
                details.Add(new PlateCharDetail { Character = c, Confidence = cf });
            }

            float avgConf = charConfs.Count > 0 ? charConfs.Average() : 0f;
            float minConf = charConfs.Count > 0 ? charConfs.Min() : 0f;
            bool isSecure = charConfs.Count > 0 && charConfs.All(c => c >= SecureThreshold);

            return new DecodeResult
            {
                Text = sb.ToString(),
                AvgConf = avgConf,
                MinConf = minConf,
                IsSecure = isSecure,
                InternalPad = internalPad,
                CharConfs = charConfs,
                Details = details
            };
        }

        // ── CTC decode ───────────────────────────────────────────────────────────
        //
        // LogSoftmaxHead → log_softmax
        // exp() ZORUNLU, aksi takdirde confidence değerleri negatif olur.
        // Shape: [1, seqLen, CtcVocab], blank = CtcBlank = 37
        //
        private DecodeResult DecodeCTC(ReadOnlySpan<float> logData, int seqLen, int ctcVocab)
        {
            int ctcBlank = ctcVocab - 1;

            // log_softmax → softmax
            var probs = new float[seqLen * ctcVocab];
            for (int t = 0; t < seqLen; t++)
            {
                float sum = 0f;
                for (int v = 0; v < ctcVocab; v++)
                {
                    float p = MathF.Exp(logData[t * ctcVocab + v]);
                    probs[t * ctcVocab + v] = p;
                    sum += p;
                }
                float inv = 1f / MathF.Max(sum, 1e-8f);
                for (int v = 0; v < ctcVocab; v++) probs[t * ctcVocab + v] *= inv;
            }

            // Greedy best-path
            var path = new int[seqLen];
            for (int t = 0; t < seqLen; t++)
            {
                int bi = 0;
                float bp = probs[t * ctcVocab];
                for (int v = 1; v < ctcVocab; v++)
                {
                    if (probs[t * ctcVocab + v] > bp) { bp = probs[t * ctcVocab + v]; bi = v; }
                }
                path[t] = bi;
            }

            // CTC collapse: tekrar eden aynı token = tek token, blank atla
            var charConfs = new List<float>();
            var details = new List<PlateCharDetail>();
            var sb = new System.Text.StringBuilder();

            int pos = 0;
            while (pos < seqLen)
            {
                int idx = path[pos];
                if (idx == ctcBlank) { pos++; continue; }

                int start = pos;
                while (pos < seqLen && path[pos] == idx) pos++;

                // Bu token'ın span boyunca ortalama olasılığı
                float avg = 0f;
                for (int t = start; t < pos; t++) avg += probs[t * ctcVocab + idx];
                avg /= (pos - start);

                if (idx < Vocab.Length)
                {
                    char c = Vocab[idx];
                    sb.Append(c);
                    charConfs.Add(avg);
                    details.Add(new PlateCharDetail { Character = c, Confidence = avg });
                }
            }

            float avgConf = charConfs.Count > 0 ? charConfs.Average() : 0f;
            float minConf = charConfs.Count > 0 ? charConfs.Min() : 0f;
            bool isSecure = charConfs.Count > 0 && charConfs.All(c => c >= SecureThreshold);

            return new DecodeResult
            {
                Text = sb.ToString(),
                AvgConf = avgConf,
                MinConf = minConf,
                IsSecure = isSecure,
                InternalPad = false, // CTC'nin internal pad kavramı yok
                CharConfs = charConfs,
                Details = details
            };
        }

        // ── ALG11 (ADV_T15_A05_SEC068): choose_prediction ──────────────────────
        //
        // Python tarafındaki hibrit chooser ile tam hizalıdır.
        //
        // Simülasyon özeti (44,306 örnek, audit_meta_best_stage_3.csv):
        //   - ALG10E : 44,147 doğru | %99.6411 plate acc | chooser wrong: 159
        //   - ALG11  : 44,160 doğru | %99.6705 plate acc | chooser wrong: 146
        //   - Net    : +13 plaka kurtarma | Fix: 16 | Regression: 3
        //
        // ALG10E'ye göre iki değişiklik:
        //
        //   [DEĞİŞİKLİK 1] disagree_char_pos_slot kolu yeniden yazıldı (adım 6a):
        //     Eskisi : pos_diff >= 0 → slot (confDiff < -0.03 hariç)
        //     Yenisi :
        //       pos_diff >= CharPosSlotThresh(0.15) → slot  (güçlü char sinyali)
        //       pos_diff <  CharPosSlotThresh AND confDiff >= AggExceptionThr(0.05) → slot
        //                                                   (aggregate açıkça slot üstün)
        //       diğer → CTC  (küçük char + zayıf agg → CTC %90+ güvenilir)
        //
        //   [DEĞİŞİKLİK 2] Yeni kural: ctc_secure_only + length-aware (adım 7, fallback öncesi):
        //     ctc_secure && !slot_secure && slot_len > ctc_len && slot_min >= 0.68 → slot
        //     (CTC'nin son karakter drop hatası; slot_min yeterliyse slot daha doğru)
        //     Etki: +4 fix, 0 regression — tam güvenli kural.
        //
        private static TitanV8ModelResult ChoosePrediction(DecodeResult slot, DecodeResult ctc)
        {
            bool slotEmpty = string.IsNullOrEmpty(slot.Text);
            bool ctcEmpty = string.IsNullOrEmpty(ctc.Text);

            // ── 1) İkisi de boş ─────────────────────────────────────────────────
            if (slotEmpty && ctcEmpty)
                return MakeResult(slot, "both_empty", "Slot");

            // ── 2) Biri boş ──────────────────────────────────────────────────────
            if (slotEmpty) return MakeResult(ctc, "slot_empty", "CTC");
            if (ctcEmpty) return MakeResult(slot, "ctc_empty", "Slot");

            // ── 3) Slot internal pad → CTC ───────────────────────────────────────
            if (slot.InternalPad)
                return MakeResult(ctc, "slot_internal_pad", "CTC");

            // ── 4) Aynı metin ────────────────────────────────────────────────────
            if (slot.Text == ctc.Text)
            {
                if (slot.IsSecure && !ctc.IsSecure) return MakeResult(slot, "agree_slot_secure", "Slot");
                if (ctc.IsSecure && !slot.IsSecure) return MakeResult(ctc, "agree_ctc_secure", "CTC");
                return MakeResult(slot, "agree_slot_default", "Slot");
            }

            float confDiff = slot.AvgConf - ctc.AvgConf;
            float minDiff = slot.MinConf - ctc.MinConf;

            // ── 5) slot = ctc + 1 karakter (insertion-aware) ────────────────────
            if (slot.Text.Length == ctc.Text.Length + 1)
            {
                int insPos = SingleCharInsertionPos(slot.Text, ctc.Text);
                if (insPos >= 0 && insPos < slot.CharConfs.Count)
                {
                    float insConf = slot.CharConfs[insPos];

                    if (insConf >= StrongInsertThr && confDiff >= SlotPlus1StrongConfDiffFloor)
                        return MakeResult(slot, "disagree_slot_plus1_strong_insert", "Slot");

                    if (!ctc.IsSecure && insConf >= MediumInsertThr && confDiff >= SlotPlus1MediumConfDiffFloor)
                        return MakeResult(slot, "disagree_slot_plus1_medium_insert", "Slot");
                }
            }

            // ── 6) Eşit uzunluk ve char_confs mevcut → pozisyon bazlı karar ────
            if (slot.Text.Length == ctc.Text.Length
                && slot.CharConfs.Count == slot.Text.Length
                && ctc.CharConfs.Count == ctc.Text.Length
                && slot.Text.Length > 0)
            {
                var diffPos = new List<int>();
                for (int i = 0; i < slot.Text.Length; i++)
                    if (slot.Text[i] != ctc.Text[i]) diffPos.Add(i);

                // ── 6a) Tam 1 karakter fark ──────────────────────────────────────
                if (diffPos.Count == 1)
                {
                    int i = diffPos[0];
                    float posConfDiff = slot.CharConfs[i] - ctc.CharConfs[i];

                    // Tie-break: negatif fark çok küçük ama aggregate slot daha iyiyse → slot
                    if (posConfDiff >= -0.02f && posConfDiff < 0f && confDiff > TieBreakAggSlotThr)
                        return MakeResult(slot, "disagree_char_pos_slot_tie_break", "Slot");

                    if (posConfDiff >= 0f)
                    {
                        // CTC aggregate açıkça daha iyi → CTC kazan (her zaman)
                        if (confDiff < CharPosCtcAggOverrideThr)
                            return MakeResult(ctc, "disagree_char_pos_ctc_agg_override", "CTC");

                        // [ALG11 DEĞİŞİKLİK 1] ──────────────────────────────────
                        // Yüksek char güveni → slot yeterince güçlü
                        if (posConfDiff >= CharPosSlotThresh)
                            return MakeResult(slot, "disagree_char_pos_slot", "Slot");

                        // Aggregate fark büyükse küçük char farkını geç, yine de slot tercih et
                        if (confDiff >= AggExceptionThr)
                            return MakeResult(slot, "disagree_char_pos_slot_agg_override", "Slot");

                        // Hem char farkı küçük hem aggregate fark küçük →
                        // CTC karakter düzeyinde %90 güvenilir, onu seç
                        return MakeResult(ctc, "disagree_char_pos_ctc_low_margin", "CTC");
                    }

                    // CTC char-level güvenilir → CTC
                    return MakeResult(ctc, "disagree_char_pos_ctc", "CTC");
                }

                // ── 6b) 2+ karakter fark ─────────────────────────────────────────
                if (diffPos.Count >= 2)
                {
                    float slotDiffMean = (float)diffPos.Average(i => (double)slot.CharConfs[i]);
                    float ctcDiffMean = (float)diffPos.Average(i => (double)ctc.CharConfs[i]);
                    float diffPosGap = slotDiffMean - ctcDiffMean;

                    if (MathF.Abs(diffPosGap) >= CharMultiDiffThr)
                    {
                        return diffPosGap >= 0f
                            ? MakeResult(slot, "disagree_char_multi_slot", "Slot")
                            : MakeResult(ctc, "disagree_char_multi_ctc", "CTC");
                    }
                }
            }

            // ── 7) Fallback: güvenlik + confidence bazlı karar ──────────────────

            // [ALG11 DEĞİŞİKLİK 2] ctc_secure_only + length-aware düzeltme:
            // CTC secure, slot secure değil, ama slot daha uzun ve slot_min yeterliyse → slot.
            // CTC zaman zaman son karakteri drop edip "secure" görünür;
            // slot_min >= 0.68 bu drop'un sahte olduğuna güçlü bir sinyaldir.
            if (!slot.IsSecure && ctc.IsSecure
                && slot.Text.Length > ctc.Text.Length
                && slot.MinConf >= SecureLenSlotMinThr)
            {
                return MakeResult(slot, "disagree_ctc_secure_slot_longer_min_ok", "Slot");
            }

            if (slot.IsSecure && !ctc.IsSecure)
                return MakeResult(slot, "disagree_slot_secure_only", "Slot");

            if (slot.IsSecure && ctc.IsSecure)
            {
                return confDiff >= 0f
                    ? MakeResult(slot, "disagree_both_secure_slot", "Slot")
                    : MakeResult(ctc, "disagree_both_secure_ctc", "CTC");
            }

            if (!slot.IsSecure && ctc.IsSecure)
                return MakeResult(ctc, "disagree_ctc_secure_only", "CTC");

            // Neither secure
            if (minDiff >= NeitherMinDiffThr)
                return MakeResult(slot, "disagree_neither_slot_min", "Slot");

            if (confDiff >= 0f)
                return MakeResult(slot, "disagree_neither_slot_conf", "Slot");

            if (ctc.AvgConf > CtcHighConfThr && confDiff < CharPosCtcAggOverrideThr)
                return MakeResult(ctc, "disagree_neither_ctc_conf", "CTC");

            return MakeResult(ctc, "disagree_ctc_default", "CTC");
        }

        private static int SingleCharInsertionPos(string longer, string shorter)
        {
            if (string.IsNullOrEmpty(longer) || shorter is null) return -1;
            if (longer.Length != shorter.Length + 1) return -1;

            int i = 0, j = 0;
            bool usedSkip = false;

            while (i < longer.Length && j < shorter.Length)
            {
                if (longer[i] == shorter[j])
                {
                    i++;
                    j++;
                    continue;
                }

                if (usedSkip)
                    return -1;

                usedSkip = true;
                i++;
            }

            // Eğer hiç skip kullanılmadıysa ek karakter en sondadır.
            return usedSkip ? i - 1 : longer.Length - 1;
        }

        private static TitanV8ModelResult MakeResult(DecodeResult src, string reason, string head)
        {
            return new TitanV8ModelResult
            {
                Text = src.Text,
                Confidence = src.AvgConf,
                ModelHead = head,
                ChooserReason = reason,
                Details = src.Details
            };
        }

        public void Dispose()
        {
            _session?.Dispose();
            _clahe?.Dispose();
        }
    }
}