# DEFERRED.md — Ertelenen İşler

> Şu anda yapılmayıp ileri bir adıma/faza bırakılan teknik işler.
> Tamamlandıkça buradan silinir, PROGRESS.md'ye geçer.
>
> **Kural:** Her adım başında bu dosya okunur — o adımda kapatılabilecek
> madde var mı kontrol edilir. Her adım sonunda yeni ertelemeler eklenir,
> kapatılanlar silinir. Detay için WORKING_STYLE.md.

---

## Faz 3 → İçerik Yönetimi

- **Article modülü** (Faz 2 dışına alındı)
  - Quill editör + kategori/tag + kapak görseli
  - Article'a özel ErrorCodes slot'u 2.4a'da hazır
  - SluggedEntityType.Article 2.4b'de hazır
  - **Status akışı (Faz 3 başlangıç kararı):** Published'da PublishedAt
    otomatik atanır (manuel girilirse korunur). Transition serbest
    (Draft↔Published↔Archived). Scheduled publishing YOK — Faz 5'e ertelendi.
  - **Yetki modeli (Faz 3 başlangıç kararı):** Faz 3'te tüm Article CRUD
    `[Authorize(Roles = "Admin")]`. Service katmanında AuthorId ataması
    yapılır. Editor/Author rol bazlı guard Faz 5'e ertelendi.

- **Medya admin UI** (Faz 3.2'ye ertelendi)
  - Altyapı (entity, repository, IFileStorageService, SkiaSharpProcessor,
    MediaService + Validator, Mapster mapping, EF migration) Faz 3.1'de
    tamamlandı (PROGRESS.md commit hash kaydı).
  - Kalan: admin galeri sayfası (DataTables), upload modal, image picker
    modal (Quill + form alanları için), MediaController + ViewModeller +
    SweetAlert2 confirm akışları.
  - HtmlSanitizer `<img>` whitelist'e eklenmesi 3.3'te (medya picker
    Quill'e bağlanırken).
  - Kütüphane kararı: SkiaSharp 3.x + SkiaSharp.NativeAssets.Linux (MIT
    lisans, Microsoft destekli). ImageSharp 3.x reddedildi (Six Labors
    Split License — ticari kurumsal site için belirsiz lisanslama,
    Faz 3.1 başlangıç kararı).
  - **MVP kapsamı (Faz 3 başlangıç kararı):** flat yapı + otomatik
    `wwwroot/uploads/{yyyy}/{MM}/` tarih klasörü, WebP dönüşümü,
    thumbnail (300px), SHA256 dedup, admin galeri (DataTables),
    image picker modal (Quill + form alanları için), tek alt-text alanı.
  - **Kapsam dışı (Faz 5'e ertelendi):** manuel klasör oluşturma/taşıma,
    responsive variant (1x/2x srcset), title/figcaption alanları.

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

- **Translation full-replace stratejisi gözden geçirme**
  - Şu an Update'te `Translations.Clear() + Add(...)` pattern, audit
    trail bozar (Translation Id'leri yenilenir, CreatedAt güncellenir)
  - Faz 2.10c'de değerlendirildi, Faz 5 audit aşamasına ertelendi
  - Çözüm: Diff-based update (LanguageCode bazlı find/update/delete/add)
  - 5 service etkilenir, kapsamlı test gerekir

- **Pre-commit hook: AddFluentValidationAutoValidation çağrı kontrolü**
  - 2.4a kararı bypass edilmesin
  - Husky/shell script ile commit öncesi DTO'larda FluentValidator
    kuralı eksik mi check
  - Faz 2.10c'de değerlendirildi, Faz 5 üretim öncesi tooling olarak
    ertelendi
  - Düşük öncelik (code review yeterli olabilir)

- **Article: Editor/Author rol bazlı yetkilendirme**
  - Faz 3 başlangıç kararıyla ertelendi (3 başında: B seçeneği)
  - Editor rolündeki kullanıcı sadece `Article.AuthorId == currentUserId`
    olan kayıtları görür/düzenler/siler
  - Implementation: IAuthorizationService + custom AuthorizationHandler
    (`ArticleAuthorRequirement`) + service katmanında guard
  - Integration test gerekir (3 senaryo: kendi makalesi, başkasının
    makalesi, admin tüm makaleler)
  - Faz 3'te tüm Article CRUD `[Authorize(Roles = "Admin")]` — service'te
    AuthorId ataması yapılır, yetki guard'ı sonradan eklenir (breaking değil)

- **Article: Scheduled publishing (zamanlanmış yayın)**
  - Faz 3 başlangıç kararıyla ertelendi (Status akışı sadeleştirildi)
  - Senaryo: `PublishedAt > Now && Status = Published` → frontend gizler,
    zamanı geldiğinde görünür hale gelir
  - Gerekli bileşenler: frontend filtresi (Faz 4 ile birlikte),
    admin "Zamanlanmış" sekmesi, opsiyonel background service (cron)
  - Faz 3'te yok — Status manuel, anlık: Draft / Published (PublishedAt=Now) / Archived

- **Medya: Manuel klasör yönetimi**
  - Faz 3 başlangıç kararıyla ertelendi (MVP kapsamı dışı)
  - Kullanıcı tarafından klasör oluşturma, taşıma, yeniden adlandırma
  - Implementation: `MediaFolder` entity + `MediaFile.FolderId` (nullable FK)
  - Restore senaryosu karmaşık (klasör silinmiş + dosya restore edilirse?)
  - İhtiyaç doğarsa Faz 5'te eklenir — mevcut dosyalar `FolderId = null`
    kalır (root), breaking migration değil

- **Medya: Responsive variant + SEO genişletmesi**
  - Faz 3 başlangıç kararıyla ertelendi (MVP kapsamı dışı)
  - 1x / 2x varyant üretimi (`srcset` desteği için)
  - `title` ve `figcaption` alanları (alt-text MVP'de var)
  - Faz 5 SEO turunda Schema.org ImageObject ile birlikte değerlendirilir
  - Implementation: ImageSharp ile ek varyantlar, `MediaFile.Variants`
    navigation property veya hesaplanan dosya yolu pattern'i

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
