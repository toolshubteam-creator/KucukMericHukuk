# PROGRESS.md — Faz İlerleme Kayıtları

> Her faz tamamlandığında bu dosya güncellenir. Bir sonraki faza aktarılan notlar, bilinen sorunlar ve test sonuçları burada tutulur.

---

## Genel Plan

| Faz | İçerik | Süre | Durum |
| --- | --- | --- | --- |
| 0 | Hazırlık (içerik, marka, kararlar) | 3-5 gün | 🔄 Devam ediyor |
| 1 | Proje kurulumu & mimari | 1 hafta | 🔄 Devam ediyor |
| 2 | Yönetim paneli iskelet & temel modüller | 2 hafta | ⏳ Beklemede |
| 3 | İçerik yönetimi modülleri | 2 hafta | ⏳ Beklemede |
| 4 | Frontend tasarım & geliştirme | 3 hafta | ⏳ Beklemede |
| 5 | SEO, entegrasyon, güvenlik | 1 hafta | ⏳ Beklemede |
| 6 | Test, düzeltme, yayına alma | 1 hafta | ⏳ Beklemede |

**Toplam:** 10 hafta (+2 hafta tampon önerisi)

---

## FAZ 1 — Proje Kurulumu & Mimari — 🔄 DEVAM EDİYOR

**Başlama Tarihi:** 04.05.2026
**Hedef Bitiş:** [Tarih girilecek]

### Tamamlanan Adımlar

- ✅ **Adım 1 (1.A):** 5 katmanlı solution iskeleti
  - 6 proje (`Core`, `DataAccess`, `Business`, `Infrastructure`, `Web`, `Tests`)
  - N-Layer referans yapısı kuruldu (Web → DataAccess referansı YOK — CLAUDE.md kuralı)
  - `Directory.Build.props` ile merkezi proje ayarı (TargetFramework `net10.0`, Nullable, ImplicitUsings)
  - NuGet paketleri sabit sürümlerle kuruldu
  - Klasör iskelet `.gitkeep` ile korundu
  - Class library template'lerinin `Class1.cs` dosyaları silindi

- ✅ **Adım 1.B:** Güvenlik açığı paketleri yamalı sürümlere yükseltildi
  - `MailKit` 4.8.0 → **4.16.0** (NU1902 GHSA-9j88-vvj5-vhgr fix)
  - `MimeKit` 4.8.0 → **4.16.0** (NU1902 GHSA-g7hc-96xr-gvvx fix)
  - `System.Security.Cryptography.Xml` (transitive 9.0.0) → **10.0.7** explicit override (NU1903 GHSA-37gx-xxp4-5rgx + GHSA-w3x6-4m5h-cxqf fix)

- ✅ **Adım 1.C:** Mapping kütüphanesi değişikliği — AutoMapper → Mapster
  - **Sebep:** AutoMapper 13/14'te NU1903 (GHSA-rvv3-g6hj-g44x) açığı, fix ancak 15+ commercial Sponsorware sürümünde
  - `AutoMapper` paketi Business projesinden kaldırıldı
  - `Mapster` 10.0.7 + `Mapster.DependencyInjection` 10.0.7 (MIT, vuln-free) eklendi
  - CLAUDE.md teknoloji stack tablosu güncellendi

### Build Durumu

- `dotnet build`: **0 Uyarı / 0 Hata**

### Bilinen Sorunlar / Geçici Çözümler

- Yok

### Bir Sonraki Adıma Aktarılan Notlar

- Mapster için DI kaydı (`AddMapster` veya `services.AddSingleton<TypeAdapterConfig>` + `services.AddScoped<IMapper, ServiceMapper>`) Adım 2'de Business `DependencyInjection.cs` içinde yapılacak.
- `Mapping/` klasörü (Business altında) tarafsız bırakıldı; ileride Mapster `IRegister` implementasyonları buraya gelir.
- Web projesinde `Microsoft.EntityFrameworkCore.Design` paketi var (CLAUDE.md migration komutu `--startup-project src/KucukMericHukuk.Web` kullandığı için gerekli) — DbContext'e doğrudan dokunulmaz, sadece migration tooling.

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
