# DEFERRED.md — Ertelenen İşler

> Şu anda yapılmayıp ileri bir adıma/faza bırakılan teknik işler.
> Tamamlandıkça buradan silinir, PROGRESS.md'ye geçer.
>
> **Kural:** Her adım başında bu dosya okunur — o adımda kapatılabilecek
> madde var mı kontrol edilir. Her adım sonunda yeni ertelemeler eklenir,
> kapatılanlar silinir. Detay için WORKING_STYLE.md.

---

## Faz 2 → İleri Adımlar

### 2.10 — Faz 2 Cleanup

- **FluentValidation client-side adapter**
  - Şu an: server-side validation only
  - Hedef: `AddFluentValidationClientsideAdapters` ekle, mevcut validator'lar
    jQuery unobtrusive'e otomatik yansır
  - Trade-off: ekstra paket vs ekstra round-trip
  - Faz 2.4a kararı

- **HTTPS profil zorunlu kuralı CLAUDE.md'ye**
  - Cookie SecurePolicy=Always nedeniyle dev'de HTTP profilinde login bozuk
  - Faz 2.3 raporundan

- **Sidebar ViewComponent refactor**
  - Şu an statik partial (Faz 2.1)
  - Aktif menü vurgulaması + role-based filter ihtiyacı doğdukça

- **Pre-commit hook: AddFluentValidationAutoValidation çağrı kontrolü**
  - 2.4a kararı bypass edilmesin
  - .git/hooks veya husky benzeri
  - Düşük öncelik (code review yeterli olabilir)

- **CategoryService.RestoreAsync parent IsDeleted kontrolü**
  - Şu an child Restore edilince parent soft-deleted ise orphan parent
    referansı kalır (FK doğru ama UI/UX kafa karıştırıcı, Faz 2.8a)
  - Çözüm: Restore'da parent.IsDeleted=true ise warning + Restore engellensin
    VEYA cascade restore. 2.10'da değerlendir.

- **PageService Translation full-replace stratejisi gözden geçirilsin**
  - Şu an `UpdateAsync`'te `page.Translations.Clear()` + `Add(...)`
  - Alternatif: id-bazlı merge (var olanı update, yenisini insert, kayıpları sil)
  - Trade-off: full-replace EF tracking açısından basit, az satır;
    merge daha "SQL-friendly" ama karmaşık
  - 2.10 cleanup'ta veya Service/Attorney/Article modüllerinde performans
    sorunu çıkarsa revize

---

## Faz 3 → İçerik Yönetimi

- **Article modülü** (Faz 2 dışına alındı)
  - Quill editör + kategori/tag + kapak görseli
  - Article'a özel ErrorCodes slot'u 2.4a'da hazır
  - SluggedEntityType.Article 2.4b'de hazır

- **Medya yöneticisi** (Faz 3'e ertelendi)
  - File upload, galeri, WebP dönüşümü, klasör yapısı
  - ImageSharp/SkiaSharp paketi

- **SEO meta alanları UI**
  - Entity'lerde alanlar var (Faz 1)
  - Admin form bileşeni Faz 3 (her modülde tekrar kullanılan partial)

- **`<img>` tag'ı HtmlSanitizer whitelist'e**
  - Şu an HtmlSanitizer whitelist'inde `<img>` yok (Faz 2.5c-i)
  - Medya yöneticisi entegrasyonu sırasında (Faz 3): `src` + `alt` attribute,
    scheme genişletmesi (`data:` dikkatle — XSS riski)

---

## Faz 5 → Güvenlik & SEO

- **Email confirmation** (Identity)
  - Faz 1'de kapalı bırakıldı, seed user EmailConfirmed=true ile geçiyor

- **AspNetCoreRateLimit** middleware
  - Brute-force ek koruma (Identity lockout zaten var)

- **Global exception middleware**
  - Beklenmedik exception'ları yakala, log + 500 sayfası
  - 2.4a kararı: custom exception'lar service'te yakalanır, üst exception'lar
    middleware'e düşer

- **OWASP Top 10 audit**

- **Sitemap.xml otomatik üretici** (Infrastructure)

- **robots.txt dinamik yönetim**

- **SlugHelper minimum uzunluk fallback (SEO koruma)**
  - Sorun: Bozuk encoding'li input (ör. yanlış UTF-8) SlugHelper'a girince
    anlamsız kısa slug üretebilir (örn. "av-test-avukat-fd" — son "-fd"
    bozuk byte'lardan)
  - Browser'da müşteri kullanımında nadir, ama API/script entegrasyonu
    veya kopya-yapıştır sırasında tetiklenebilir
  - SEO açısından: anlamsız slug Google'da kötü URL kalitesi ve user
    perception zayıf
  - Çözüm önerisi: SlugHelper'a minimum uzunluk kuralı (örn. 3-5 karakter
    altındaysa "untitled-{entity}-{id}" gibi fallback)
  - Veya: bozuk Unicode tespiti (Slugify öncesi normalize başarısızsa
    reject + Result.Failure)
  - Faz 5 kapsamında, üretim öncesi audit'te değerlendirilir

---

## Faz 6 → Yayına Alma

- **Production seed credentials**
  - `Seed__AdminPassword` env var ile farklı + güçlü değer
  - User Secrets sadece dev

- **LocalDB → SQL Server** geçişi
  - Connection string env var

- **Lockout reset UI**
  - Şu an manuel SQL gerekli (LockoutEnd=NULL, AccessFailedCount=0)
  - Faz 2.10 kullanıcı yönetimi modülünde olabilir — karar gerek

---

## Belirsiz Zamanlama

- **Soft-delete + Slug çakışması**
  - Silinmiş kayıt slug'ı yeniden kullanılabilir
  - Restore senaryosunda unique index ihlali → restore patlar
  - Production'da sorun olursa `IgnoreQueryFilters()` eklenir
  - Faz 6 öncesi gözden geçir

- **AppClaimTypes constants sınıfı**
  - Şu an "FullName" claim adı string sabit (Faz 2.3)
  - Yeni claim tipleri eklendikçe `Core/Constants/AppClaimTypes.cs` oluşturulur

- **SluggedEntityType enum dosya organizasyonu**
  - Şu an `Core/Interfaces/Services/ISlugService.cs` içinde tanımlı (Faz 2.4b)
  - Başka tüketici (örn. URL routing, Sitemap üretici) bağımsız ihtiyaç
    duyarsa ayrı dosyaya taşınır: `Core/Common/SluggedEntityType.cs`
  - Şu an taşımaya gerek yok — single-tenant interface tarafı
