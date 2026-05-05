# DEFERRED.md — Ertelenen İşler

> Şu anda yapılmayıp ileri bir adıma/faza bırakılan teknik işler.
> Tamamlandıkça buradan silinir, PROGRESS.md'ye geçer.
>
> **Kural:** Her adım başında bu dosya okunur — o adımda kapatılabilecek
> madde var mı kontrol edilir. Her adım sonunda yeni ertelemeler eklenir,
> kapatılanlar silinir. Detay için WORKING_STYLE.md.

---

## Faz 2 → İleri Adımlar

### 2.5c — Page UI'a entegre edilecekler

- **Quill 2.x WYSIWYG editör** Page Content alanına entegre edilecek
  - Tetik: 2.5c (Create/Edit view)
  - Bağlam: 2.5a service Content'i string olarak alır, editör tipi UI tercihi

- **HtmlSanitizer.NET** entegrasyonu
  - Tetik: 2.5c, Quill çıktısı server-side sanitize edilecek
  - CLAUDE.md güvenlik kuralı (Bölüm 8)

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

- **`IsUniqueConstraintViolation` helper refactor (PageService)**
  - Şu an `InnerException.Message` string kontrolü ("UNIQUE", "duplicate")
  - 2.10'da `SqlException.Number` (2627/2601) kontrolüne çevrilecek
  - Dosya: `Business/Services/PageService.cs`

- **PageService Translation full-replace stratejisi gözden geçirilsin**
  - Şu an `UpdateAsync`'te `page.Translations.Clear()` + `Add(...)`
  - Alternatif: id-bazlı merge (var olanı update, yenisini insert, kayıpları sil)
  - Trade-off: full-replace EF tracking açısından basit, az satır;
    merge daha "SQL-friendly" ama karmaşık
  - 2.10 cleanup'ta veya Service/Attorney/Article modüllerinde performans
    sorunu çıkarsa revize

### 2.6-2.9 Modülleri için ön kontrol

- **Service/Attorney/Category/Tag entity'lerinde `IsActive` + `DisplayOrder`
  var mı kontrolü**
  - Faz 1 PROGRESS.md raporundan: Article'da Status/IsFeatured var ama
    Page'de yoktu (Faz 2.5a'da eklendi)
  - Service/Attorney/Category/Tag entity'leri Faz 1'de tanımlandı
  - 2.6 başlamadan önce her entity'nin alan listesi kontrol edilecek
  - Eksik alan varsa Page örneğindeki gibi (Faz 2.5a) entity güncelleme +
    migration + DTO genişletmesi yapılır

- **5 repository'ye eksik admin metodlarının eklenmesi**
  - `GetByIdWithTranslationsAsync`
  - `GetAdminPagedAsync(keyword, languageCode, page, pageSize, includeDeleted, ct)`
  - `GetByIdIncludingDeletedAsync`
  - Entity-spesifik unique kontrol metodu (varsa, örn. ServiceKey)
  - Tetik: 2.6 (Service modülü) başlarken Page repository pattern'i kopyalanır

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
