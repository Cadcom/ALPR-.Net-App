#!/usr/bin/env python3
import argparse
import os
import time
import numpy as np
from PIL import Image
import onnxruntime as ort
import torch
from strhub.data.utils import Tokenizer

def preprocess(image_path, img_size=(32, 128)):
    """Resmi modelin beklediği formata sokar.
    
    UYARI: Eğitimdeki preprocessing adımları ile birebir eşleşmelidir.
    Eğitimde T.Resize(img_size, T.InterpolationMode.BICUBIC) ve T.Normalize(0.5, 0.5) kullanılmış.
    """
    img = Image.open(image_path).convert('RGB')
    # PIL resize (width, height) bekler. img_size ise (height, width) formatındadır.
    img = img.resize((img_size[1], img_size[0]), Image.BICUBIC)
    
    # [0, 255] -> [0.0, 1.0]
    img_data = np.array(img).astype(np.float32) / 255.0
    
    # Normalize: (x - 0.5) / 0.5 -> [-1.0, 1.0]
    img_data = (img_data - 0.5) / 0.5
    
    # HWC to CHW
    img_data = np.transpose(img_data, (2, 0, 1))
    
    # Batch boyutu ekle: [1, C, H, W]
    img_data = np.expand_dims(img_data, axis=0)
    return img_data

def main():
    parser = argparse.ArgumentParser(description='ParSeq ONNX Tekil Resim İnferansı')
    parser.add_argument('--image', type=str, default='real_samples\\Q35319.jpg', help='Tahmin edilecek resmin yolu')
    parser.add_argument('--model', type=str, default='ozel_model_fp16.onnx', help='ONNX model dosyasının yolu')
    parser.add_argument('--charset', type=str, default='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ', help='Eğitimde kullanılan charset')
    
    args = parser.parse_args()
    
    # Temel kontroller. Bir mühendis bunları asla atlamaz!
    if not os.path.exists(args.image):
        print(f"HATA: Belirtilen yol bulunamadı: {args.image}")
        return
        
    if not os.path.exists(args.model):
        print(f"HATA: Belirtilen model bulunamadı: {args.model}")
        return

    # (Rastgele resim seçme mantığı kullanıcı isteğiyle kaldırıldı)

    # Tokenizer kurulumu. Charset'in training ile aynı olması KRİTİKTİR.
    print(f"[i] Tokenizer başlatılıyor. Charset: {args.charset}")
    tokenizer = Tokenizer(args.charset)
    
    # ONNX Runtime Session
    # GPU desteğini kontrol et
    available_providers = ort.get_available_providers()
    providers = []
    if 'CUDAExecutionProvider' in available_providers:
        providers.append('CUDAExecutionProvider')
    providers.append('CPUExecutionProvider')
    
    print(f"[i] Aktif ONNX Sağlayıcıları: {providers}")
    
    try:
        session_opts = ort.SessionOptions()
        session_opts.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_BASIC
        session = ort.InferenceSession(args.model, providers=providers, sess_options=session_opts)
    except Exception as e:
        print(f"HATA: Model yüklenirken sorun oluştu: {e}")
        print("İPUCU: FP16 modeller CPU üzerinde çalışırken bazı kısıtlamalara takılabilir. GPU veya FP32 deneyin.")
        return
        
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name
    
    # Resmi işle
    img_data = preprocess(args.image)
    
    # Modelin beklediği veri tipini kontrol et (FP16 desteği için)
    input_type = session.get_inputs()[0].type
    if input_type == 'tensor(float16)':
        print("[i] Model FP16 girdisi bekliyor. Veri tipi dönüştürülüyor...")
        img_data = img_data.astype(np.float16)
        
    print("Python Input Tensor shape:", img_data.shape)
    print("Python Input Tensor first 10 values:", img_data.flatten()[:10])
        
    # İnferans
    print("[i] Model ısıtılıyor (Warm-up)...")
    for _ in range(5):
        session.run([output_name], {input_name: img_data})
        
    print("[i] Gerçek ölçüm yapılıyor (10 iterasyon)...")
    start_time = time.time()
    iterations = 10
    for _ in range(iterations):
        logits = session.run([output_name], {input_name: img_data})[0]
    infer_time = ((time.time() - start_time) / iterations) * 1000  # ms
    print(f"[i] Logits shape: {logits.shape}")
    
    # Post-process (Logits -> Softmax -> Decoding)
    # Tokenizer kütüphanede Torch Tensor beklediği için numpy'ı dönüştürüyoruz.
    logits_torch = torch.from_numpy(logits)
    probs = logits_torch.softmax(-1)
    
    # Boyut kontrolü ve otomatik düzeltme (Mühendislik budur!)
    vocab_size = logits.shape[2]
    if len(tokenizer) != vocab_size:
        print(f"\n[!] UYARI: Modelin çıkış boyutu ({vocab_size}) ile Tokenizer boyutu ({len(tokenizer)}) uyuşmuyor!")
        print(f"[!] Muhtemelen yanlış charset kullanıyorsun. Çökme olmasın diye geçici bir charset oluşturuluyor.")
        
        # 95'lik model için (92 char + 3 special)
        dummy_charset = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`"
        if len(dummy_charset) + 3 == vocab_size:
            target_charset = dummy_charset
        else:
            # Genel durum için doldurma
            all_chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"
            target_charset = all_chars[:vocab_size - 3] if vocab_size > 3 else "A"
            
        tokenizer = Tokenizer(target_charset)
        print(f"[!] Geçici Charset atandı. Doğru sonuçlar için modelin asıl charsetini belirtmelisin.")
    
    # Greedy decoding
    pred, p = tokenizer.decode(probs)
    
    # Sonuçları yazdır
    print("\n" + "="*40)
    print(f"RESİM  : {args.image}")
    print(f"SONUÇ  : {pred[0]}")
    print(f"GÜVEN  : {p[0].mean().item():.4f}")
    print(f"SÜRE   : {infer_time:.2f} ms")
    print("="*40)

if __name__ == '__main__':
    main()
