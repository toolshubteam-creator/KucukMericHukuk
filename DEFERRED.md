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

- **`IsUniqueConstraintViolation` helper refactor (PageService)**
  - Şu an `InnerException.Message` string kontrolü ("UNIQUE", "duplicate")
  - 2.10'da `SqlException.Number` (2627/2601) kontrolüne çevrilecek
  - Dosya: `Business/Services/PageService.cs`

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

- **Create.cshtml + Edit.cshtml ortak `_PageForm` / `_TagForm` / `_ServiceForm` / `_CategoryForm` partial refactor**
  - Page (Faz 2.5c-i), Tag (Faz 2.6b), Service (Faz 2.7b) ve Category
    (Faz 2.8b) için form body iki view'da kopya
  - 2.10'da partial'a alın, sadece outer container + breadcrumb +
    submit label farklı olur

- **`Web/DependencyInjection.cs` oluşturulacak**
  - Şu an `Program.cs`'te direkt `TypeAdapterConfig.GlobalSettings.Scan` çağrısı
  - `AddWeb()` extension'ına taşı, Mapster scan + ileride filter conventions

- **MVC implicit-required vs FluentValidation duplicate mesaj**
  - Non-nullable string property'lere ASP.NET Core implicit `[Required]`
    uyguluyor → "The X field is required." mesajı. FluentValidation aynı kuralı
    daha açıklayıcı Türkçe mesajla veriyor — 2 mesaj birden gözüküyor (Faz 2.5c-i)
  - Çözüm: ya `MvcOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`
    ya da DTO property'lerini nullable yap. 2.10'da karar verilir.

- **Page+Service controller'ları `FirstError.Message` kullanımına geç**
  - Şu an `PagesController` ve `ServicesController` Delete/Restore/HardDelete'te
    sabit mesaj ("silinirken bir sorun oluştu") `TempData["Error"]`'a yazıyor;
    service'in user-friendly mesajı (örn. HasChildren) ignore ediliyor
  - Category 2.8c'de iyileştirme yapıldı: `result.FirstError?.Message`
    kullanılıyor (mesajlar service'in Result.Failure'ından sızıyor)
  - 2.10'da Page+Service controller'larını da bu pattern'e hizala

- **Controller log seviyesi: expected business outcome'lar `LogWarning` olsun**
  - Şu an Page+Service Delete/Restore/HardDelete'te `LogError` kullanılıyor
  - Category 2.8c'de `LogWarning` kullanıldı (HasChildren expected outcome,
    Page/Service için de Translation full-replace race vb. expected outcomes)
  - 2.10'da Page+Service controller'larını da `LogWarning`'e hizala

### 2.6-2.9 Modülleri için ön kontrol

- **5 repository'ye eksik admin metodlarının eklenmesi**
  - ✅ Tag tarafı 2.6a'da kapatıldı
  - ✅ Service tarafı 2.7a'da kapatıldı (3 metod + AttorneyRepository'ye
    AttorneysExistAsync + GetByIdsAsync)
  - ✅ Category tarafı 2.8a'da kapatıldı (3 admin metod + 2 hiyerarşi
    guard: GetDescendantIdsAsync, HasChildrenAsync)
  - Attorney tarafı 2.9a'da kapatılacak
  - Entity-spesifik unique kontrol metodu (varsa, örn. ServiceKey)

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
