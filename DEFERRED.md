# DEFERRED.md — Ertelenen İşler

> Şu anda yapılmayıp ileri bir adıma/faza bırakılan teknik işler.
> Tamamlandıkça buradan silinir, PROGRESS.md'ye geçer.
>
> **Kural:** Her adım başında bu dosya okunur — o adımda kapatılabilecek
> madde var mı kontrol edilir. Her adım sonunda yeni ertelemeler eklenir,
> kapatılanlar silinir. Detay için WORKING_STYLE.md.

---

## Faz 3 → İçerik Yönetimi

- **SEO meta alanları UI**
  - Entity'lerde alanlar var (Faz 1)
  - Admin form bileşeni Faz 3 (her modülde tekrar kullanılan partial)

---

## Faz 4 → Frontend (sonraki adımlar)

- **Service'lerde sayfalama + kategorize gruplama**
  - Faz 4.4 başında karar: tek sayfa grid (8-15 hizmet için yeterli)
  - Tetik: hizmet sayısı 20+ olursa veya "Bireysel/Ticari Hukuk" gibi gruplama isteği gelirse
  - Implementation: Service entity'sine optional `ServiceCategory` (enum/entity) + Index'te accordion ya da sekme

- **Hero/header marka vurgusu — yeşil zemin alternatifleri değerlendirme**
  - Faz 4.2 sonrası gündem: kullanıcı hero/header'da yeşil arka plan istiyor mu sorusunu sordu
  - Mevcut karar: krem hero korundu (beyaz alan hakimiyeti spec'i, prestij dengesi)
  - Faz 4.9 son cila adımında 4 alternatif değerlendirilecek:
    - A) Header altına ince altın çizgi (minimal vurgu)
    - B) Hero'da decorative açık-yeşil filigran (%5 opacity arka plan)
    - C) Hero alt-banner: küçük yeşil şerit + slogan
    - D) Hero üstü altın eyebrow: "Küçükmeriç Hukuk Bürosu · Sakarya"
  - Diğer 16 sayfa render olduktan sonra hero karakteri netleşince ekran görüntüleri üzerinden tek seferde karar verilecek
  - Tetik: Faz 4.9 (404 + cookie consent + son cila adımı)

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

- **Token DRY refactor — admin + public ortak `tokens.css`**
  - Faz 4.1'de admin.css :root token'ları ve site.css :root token'ları aynı renk değerleriyle iki dosyada duplicate yazıldı
  - Refactor: ortak `wwwroot/css/tokens.css` her iki layout'ta `<link>` ile yüklenir
  - Trade-off: tutarlılık vs çift dosya değiştirme riski; şimdilik bilinçli duplicate
  - Tetik: Faz 5/6 cleanup turunda

- **Google Fonts self-host (Playfair Display + Inter)**
  - Faz 4.1'de Google Fonts CDN ile yüklendi (preconnect + display=swap)
  - Self-host avantaj: 1 daha az DNS, GDPR safer, indirme garantili boyut
  - Implementation: woff2 dosyalarını `wwwroot/fonts/` altına indir, @font-face ile bağla
  - Tetik: Faz 5/6 PageSpeed optimizasyon turunda

- **Lucide Icons CDN: SRI hash + sürüm pin**
  - Faz 4.2'de `unpkg.com/lucide@latest` CDN'inden SRI'siz yüklendi
  - Mevcut SRI deferral maddesine paralel: Faz 5 OWASP turunda SRI eklenecek
  - `@latest` yerine sabit sürüm pinleme (ör: `lucide@0.x.x`) aynı turun parçası

- **CDN SRI integrity hash — admin + public Bootstrap, Tabler, Choices.js, Quill, SweetAlert2**
  - Faz 4.1 başında durma noktası: admin layout'unda Bootstrap JS SRI'siz yükleniyor (Faz 3.3.1 kararı), public'te de aynı politikayla devam edildi (5.3.3 CDN, hash yok)
  - Diğer CDN'ler (Tabler 1.4.0, Choices.js 11.1.0, Quill 2.0.3, SweetAlert2) de SRI'siz yükleniyor
  - OWASP A06: Vulnerable Components — supply chain saldırısına karşı koruma
  - Implementation: tüm CDN <link>/<script> tag'lerine integrity="sha384-..." crossorigin="anonymous" ekle, hash'leri jsdelivr/CDN sağlayıcılarının SRI generator'ından üret
  - Tetik: Faz 5 OWASP Top 10 audit turunda — admin + public birlikte

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

- **Medya: Picker'da çoklu seçim**
  - Faz 3.2 başlangıç kararıyla MVP'de tek seçim
  - Ctrl/Shift ile çoklu seçim, Quill'e gallery insert için faydalı
  - Form alanları için anlamsız (tek input), galeri sayfasında toplu
    silme/restore aksiyonları için faydalı

- **Medya: Paralel upload + progress bar**
  - Faz 3.2'de client-side sıralı loop (basit, güvenli)
  - 5+ dosya yüklenirken UX yavaş kalıyor — `Promise.all` + concurrency
    limit (örn. 3) ile paralel
  - Server-side throttling gerekebilir (rate limit, MediaService
    isteğe bağlı kuyruk)

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

- **Test ortamı için IFileStorageService mock/in-memory varyantı**
  - Faz 3.2 raporunda gündeme geldi (madde DEFERRED'a 3.3'te yazıldı)
  - Mevcut durum: Integration testler gerçek `wwwroot/uploads/` altına
    dosya yazıyor (LocalFileStorageService gerçek WebRootPath kullanır)
  - Geçici çözüm: `.gitignore`'da `src/KucukMericHukuk.Web/wwwroot/uploads/`
    → repo'ya kaçmıyor
  - Risk: Test izolasyonu zayıf, paralel test çakışması mümkün, cleanup yok,
    her test koşusu disk biriktirir
  - Çözüm seçenekleri:
    - `InMemoryFileStorageService` implementasyonu, `WebApplicationFactory`'de
      DI override (test fixture'ında)
    - VEYA `TempPath` bazlı `LocalFileStorageService` variant + test sonrası
      cleanup hook (`IAsyncLifetime`)

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
