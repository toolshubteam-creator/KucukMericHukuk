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

- **Article AuthorId seed bağı (opsiyonel)**
  - Faz 4.5'te demo Article'lar AuthorId=null
  - Attorney detayında "Yazdığı makaleler" bölümü demo'da boş kalıyor (`if (UserId.HasValue)` guard atlar)
  - Müşteri admin panelinden makale yazınca AuthorId otomatik set olur (Faz 3.4 davranışı)
  - Demo seed güncellemesi opsiyonel — gerçek müşteri akışı zaten doğru çalışıyor
  - Tetik: gerek görülürse Faz 5/6, yoksa hiç

- **Service'lerde sayfalama + kategorize gruplama**
  - Faz 4.4 başında karar: tek sayfa grid (8-15 hizmet için yeterli)
  - Tetik: hizmet sayısı 20+ olursa veya "Bireysel/Ticari Hukuk" gibi gruplama isteği gelirse
  - Implementation: Service entity'sine optional `ServiceCategory` (enum/entity) + Index'te accordion ya da sekme

---

## Faz 4 → Frontend (Faz 5 / sonrasına ertelenen)

- **Faq admin CRUD** (Faz 5)
  - Faz 4.7'de SSS sayfası public-only oluşturuldu, admin tarafına Faqs eklenmedi
  - Müşteri SSS düzenleme talebinde Faz 5'te admin Area'ya FaqsController + form view + CRUD eklenir
  - Pattern: ServicesController/Admin (PageForm benzeri) örnek alınabilir

- **Galeri sayfası** (Faz 5)
  - Faz 4.7 daraltılmış scope kararıyla ertelendi
  - Yapılacaklar: MediaFile.IsPublic field eklemek (migration), admin tarafında "public" toggle, /Galeri public sayfası grid render

- **Referanslar/Testimonial entity ve sayfası** (Faz 5)
  - Faz 4.7 daraltılmış scope kararıyla ertelendi
  - Faz 4.2'deki _PrinciplesSection partial Ana Sayfa'da TBB-safe değer kartlarını gösteriyor
  - Faz 5'te Testimonial entity geldiğinde gerçek müvekkil yorumları (anonim, TBB-safe) eklenir

- **Cloudflare Turnstile spam koruması** (Faz 5)
  - Faz 4.8'de honeypot ile başlangıç anti-spam koyuldu (silent reject)
  - Cloudflare Turnstile (advanced spam koruması) Faz 5 OWASP turunda eklenecek
  - Site key + secret + form widget + server-side verify endpoint

- **ContactMessages admin liste UI** (Faz 5)
  - Faz 4.8'de mesajlar DB'ye yazılıyor + admin'e email bildirim gidiyor; admin liste UI henüz yok
  - Yapılacak: ContactMessages admin Area controller + liste view + okundu/yanıtlandı toggle + detay
  - Pattern: ServicesController/Admin (PageForm benzeri) örnek alınabilir

- **Faz 4.10 / v0.4.1 — Bütüncül tasarım cila turu**
  - Faz 4 tüm sayfalar tamamlandıktan sonra kullanıcı tarafından genel tasarım gözden geçirme talep edildi (09.05.2026)
  - Kapsam: tasarım tutarlılığı, mikro etkileşimler, tipografi ince ayar, mobile UX
  - Tetik: kullanıcı isteğiyle, Faz 5 öncesi mini cila turu

- **Browser Link dev-time uyarıları temizliği** (Faz 5)
  - Faz 4.8 console'unda tespit edildi: "Unload event listeners deprecated" + Cookie HTTPS uyarıları
  - Browser Link özelliğinden kaynaklanır, production'da yok
  - Çözüm: launchSettings.json'da hot reload toggle veya Browser Link kapat
  - Tetik: dev-time gürültü rahatsız ederse

- **Çok dilli StatusCode middleware** (Faz 6)
  - Faz 4.9'da `UseStatusCodePagesWithReExecute("/tr-TR/Error/{0}")` sabit Türkçe culture
  - Çok dilli destek genişlerse middleware culture-aware yeniden yazılmalı (RouteData'dan culture okuma + fallback)
  - Tetik: İngilizce/diğer dil destekleri eklendiğinde

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

- **Self-hosting CDN dosyaları** (Faz 6 / yayın hazırlığı)
  - Faz 5.5'te CDN + SRI ile gidildi — tedarik zinciri saldırılarına karşı korumalı, ama CDN downtime'a bağımlı
  - Yayın öncesi bootstrap.min.css + js + lucide.min.js + quill.snow.css + quill.js wwwroot/lib/ altına self-host edilebilir
  - asp-append-version cache busting ile birlikte
  - Tetik: Production öncesi karar veya CDN downtime yaşanırsa

- **CI'da SRI doğrulama** (Faz 6+ / CI pipeline)
  - Faz 5.5'te scripts/sri-check.{sh,ps1} manuel çalıştırılır
  - GitHub Actions workflow: PR'da scripts/sri-check.sh çalıştır, layout'taki integrity attribute'larıyla karşılaştır, uyuşmazlık varsa fail
  - Bootstrap/Lucide/Quill sürümü güncellenirken hash güncellenmeyi unutursa production'da kırılma riski → CI bunu yakalar
  - Tetik: Faz 6 CI/CD kurulumu

- **robots.txt admin yönetimi** (Faz 6 / SiteSettings entity)
  - Faz 5.3'te robots.txt SitemapService.BuildRobotsTxt() ile hardcoded
  - Spec mad. 5.1: "robots.txt içeriği admin panelinden düzenlenebilir"
  - Faz 6'da SiteSettings entity geldiğinde DB'den okunur (key: "robots.txt"); Service tek satır değişir
  - Tetik: Faz 6 yayın hazırlığı veya müşteri talebi

- **Sitemap caching** (Faz 6 / performans)
  - Faz 5.3'te request-time generation (DB hit her istekte: 4 query)
  - Crawler trafiği yüksekleşirse 1h MemoryCache eklenir
  - Tetik: Google Search Console'da crawl hatası veya yüksek crawl rate

- **Sitemap index** (Faz 6+ / scale)
  - Faz 5.3'te tek sitemap.xml (limit 50.000 URL — yıllarca yetişir)
  - 50.000+ URL'e ulaşılırsa article-sitemap.xml + service-sitemap.xml + ... bölünür
  - Tetik: URL count >10.000 (erken uyarı)

- **Hreflang + multi-language sitemap** (Faz 6+ / lokalizasyon)
  - Faz 5.3'te tek dil (tr-TR), URL'lerde culture prefix dahil
  - Multi-language eklenirse her URL için xhtml:link rel="alternate" hreflang="..." entry
  - Tetik: İkinci dil eklenmesi (Faz 6+ ürün kararı)

- **Breadcrumb için CMS-yönetilebilir hiyerarşi** (Faz 6+)
  - Faz 5.4'te breadcrumb hierarchies hardcoded view'larda ("Hizmet Alanlarımız", "Avukatlarımız" vb.)
  - Müşteri terminoloji değişikliği talebinde 5+ view düzenlenmesi gerekir
  - SiteSettings entity geldiğinde label'ler key-value'dan okunabilir
  - Tetik: Müşteri terminoloji değişikliği talebi

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

- **SiteSettings entity** (Faz 6)
  - Faz 5.1'de SiteInfoOptions appsettings'tan okunuyor; müşterinin admin panelinden yönetmesi mümkün değil
  - Müşteri site adı, OG image, GA4/GTM kodları, sosyal medya linklerini panelden yönetmek isterse SiteSettings entity gerek (key-value veya structured single-row)
  - Pattern: PageConfiguration benzeri admin Form view + key-value liste; Options pattern korunur, SiteInfoOptions DB'den hydrate edilir
  - Tetik: Faz 6 yayın hazırlığı veya müşteri talebi

- **MetaTagsHelpers birim test** (Faz 5.10 / Faz 6)
  - Suffix mantığı (ana sayfa istisnası), OG image absolute URL, canonical override, robots default
  - Şu an view-level entegrasyonla manuel doğrulanıyor; CI safe-net için unit test eklenebilir
  - Tetik: regresyon yaşanırsa veya Faz 5 sonu cleanup

- **Müşteri iletişim bilgileri** (Faz 0 → SiteInfo doldurma)
  - Faz 5.2'de SiteInfoOptions'a Telephone, Email, StreetAddress, PostalCode, Latitude/Longitude alanları eklendi
  - Müşteri verince appsettings.json + appsettings.Development.json'a yazılır
  - LegalService schema bu alanlarla zenginleşir; şu an boş alanlar schema'da render edilmez
  - Tetik: Faz 0 müşteri içerik teslimatı

- **Service detay JSON-LD** (Faz 5.4 / Faz 6)
  - Faz 5.2'de Article + Person + FAQPage işlendi; Service detay için LegalService alt-tipi (örn. ProfessionalService) düşünülebilir
  - Service detay zaten LegalService'in bir parçası; ayrı schema marjinal ek değer
  - Tetik: Faz 5.4 breadcrumb turunda yeniden değerlendir



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
