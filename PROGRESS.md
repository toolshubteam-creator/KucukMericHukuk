# PROGRESS.md — Faz İlerleme Kayıtları

> Her faz tamamlandığında bu dosya güncellenir. Bir sonraki faza aktarılan notlar, bilinen sorunlar ve test sonuçları burada tutulur.

---

## Genel Plan

| Faz | İçerik | Süre | Durum |
| --- | --- | --- | --- |
| 0 | Hazırlık (içerik, marka, kararlar) | 3-5 gün | 🔄 Devam ediyor |
| 1 | Proje kurulumu & mimari | 1 hafta | ⏳ Beklemede |
| 2 | Yönetim paneli iskelet & temel modüller | 2 hafta | ⏳ Beklemede |
| 3 | İçerik yönetimi modülleri | 2 hafta | ⏳ Beklemede |
| 4 | Frontend tasarım & geliştirme | 3 hafta | ⏳ Beklemede |
| 5 | SEO, entegrasyon, güvenlik | 1 hafta | ⏳ Beklemede |
| 6 | Test, düzeltme, yayına alma | 1 hafta | ⏳ Beklemede |

**Toplam:** 10 hafta (+2 hafta tampon önerisi)

---

## FAZ 0 — Hazırlık — 🔄 DEVAM EDİYOR

**Başlama Tarihi:** [Tarih girilecek]
**Hedef Bitiş:** [Tarih girilecek]

### Karar Verilenler

- [x] Renk paleti: Seçenek A — Koyu orman yeşili (#0F2A23) + Şampanya altın (#C9A961) + Krem (#FAF7F2)
- [x] .NET sürümü: .NET 10 (STS)
- [x] Domain uzantısı: .av.tr
- [x] Çok dilli destek: Şu an Türkçe, altyapı çok dile hazır
- [ ] Hosting tercihi: [Yurt içi / Yurt dışı + sağlayıcı]
- [ ] Randevu sistemi: [Form tabanlı / Takvim entegrasyonlu]
- [ ] CDN kullanımı: [Cloudflare / Yok]

### Müşteriden Beklenen İçerikler

Detay için `docs/Musteri-Icerik-Toplama-Formu.docx` dosyasına bakınız.

- [ ] Logo (vektör tercihi)
- [ ] Avukat bilgileri ve fotoğrafları
- [ ] Ofis fotoğrafları
- [ ] Hizmet alanı açıklamaları
- [ ] İletişim bilgileri ve harita konumu
- [ ] Sosyal medya hesapları
- [ ] (Varsa) hazır makaleler
- [ ] KVKK aydınlatma metni taslağı

### Teknik Hazırlık

- [ ] GitHub repository oluşturulacak
- [ ] Sunucu / hosting kararı verilecek
- [ ] SSL sertifikası planlanacak
- [ ] Domain transferi/kaydı (.av.tr için baro levha kaydı gerekir)

### Bir Sonraki Faza Aktarılan Notlar

- TBB reklam yasağı kuralları gözetilerek "Referanslar" sayfası içeriği hazırlanmalı.
- KVKK için iletişim formunda aydınlatma onayı zorunlu olacak; tasarımda yer ayrılmalı.

---

## ŞABLON: FAZ X — [Faz Adı] — [DURUM]

**Tamamlanma Tarihi:** GG.AA.YYYY

### Yapılanlar

- [Tamamlanan iş 1]
- [Tamamlanan iş 2]

### Bilinen Sorunlar / Geçici Çözümler

- [Sorun ve geçici çözüm]

### Bir Sonraki Faza Aktarılan Notlar

- [Dikkat edilmesi gereken nokta]
- [İyileştirme önerisi]

### Test Sonuçları

- PageSpeed (Mobil): XX / 100
- PageSpeed (Masaüstü): XX / 100
- Kritik Hatalar: Yok / [Açıklama]
- Tarayıcı uyumluluğu: Chrome ✅ / Firefox ✅ / Safari ✅ / Edge ✅
