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

## Faz 4 → Frontend (Faz 6'ya ertelenen)

- **Faz 4.10 / v0.4.1 — Bütüncül tasarım cila turu**
  - Faz 4 tüm sayfalar tamamlandıktan sonra kullanıcı tarafından genel tasarım gözden geçirme talep edildi (09.05.2026)
  - Kapsam: tasarım tutarlılığı, mikro etkileşimler, tipografi ince ayar, mobile UX
  - Tetik: kullanıcı isteğiyle, Faz 6 yayın öncesi cila turu

- **Browser Link dev-time uyarıları temizliği** (Faz 6)
  - Faz 4.8 console'unda tespit edildi: "Unload event listeners deprecated" + Cookie HTTPS uyarıları
  - Browser Link özelliğinden kaynaklanır, production'da yok
  - Çözüm: launchSettings.json'da hot reload toggle veya Browser Link kapat
  - Tetik: dev-time gürültü rahatsız ederse

- **Çok dilli StatusCode middleware** (Faz 6)
  - Faz 4.9'da `UseStatusCodePagesWithReExecute("/tr-TR/Error/{0}")` sabit Türkçe culture
  - Çok dilli destek genişlerse middleware culture-aware yeniden yazılmalı (RouteData'dan culture okuma + fallback)
  - Tetik: İngilizce/diğer dil destekleri eklendiğinde

---

## Faz 6'ya Aktarılan (Faz 5 boyunca eklendi)

- **Token DRY refactor — admin + public ortak `tokens.css`**
  - Faz 4.1'de admin.css :root token'ları ve site.css :root token'ları aynı renk değerleriyle iki dosyada duplicate yazıldı
  - Refactor: ortak `wwwroot/css/tokens.css` her iki layout'ta `<link>` ile yüklenir
  - Trade-off: tutarlılık vs çift dosya değiştirme riski; şimdilik bilinçli duplicate
  - Tetik: Faz 5/6 cleanup turunda

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

- **Production Turnstile key'leri** (Faz 0 / yayın hazırlığı)
  - Faz 5.6'da appsettings'te demo key'ler (always pass) — `1x00000000000000000000AA` + `1x0000000000000000000000000000000AA`
  - Müşteri Cloudflare hesabında domain (kucukmerichukuk.av.tr) ekleyip gerçek SiteKey + SecretKey alacak
  - SecretKey commit edilmez — appsettings.Production.json veya environment variable
  - README'de prod kurulum adımına eklenmeli (Faz 6)
  - Tetik: Yayına alma adımı

- **Turnstile JS pin/SRI istisna** (kalıcı not)
  - challenges.cloudflare.com/turnstile/v0/api.js Cloudflare server-maintained, otomatik update
  - SRI hash bozulur (her güncellemede deploy fail), pin yapılmaz (Cloudflare resmi pratiği)
  - Faz 6.17 sonrası diğer vendor JS/CSS hepsi self-host (wwwroot/lib/, libman); Turnstile tek CDN istisnası
  - sri-check.{sh,ps1} URL listesine EKLENMEZ

- **CSP style-src 'unsafe-inline' hardening** (Faz 6+ / OWASP)
  - Faz 6.20'de script-src nonce'landı ('unsafe-inline' kaldırıldı); style-src bilinçli kapsam dışı bırakıldı
  - style-src'de hâlâ 'unsafe-inline' var — inline style= attribute'ları + olası inline <style> blokları taranmadı
  - Bootstrap/Tabler/SweetAlert2/Quill runtime'da inline style enjekte eder (popper, modal, tooltip pozisyonlama) — nonce'lamak kırılgan, kapsamlı görsel regresyon testi gerekir
  - Çözüm seçenekleri: per-request nonce style-src'ye de uygula (vendor uyumu test edilmeli) VEYA 'unsafe-hashes' + bilinen hash'ler
  - Tetik: Penetration test sonucu veya production OWASP audit ikinci tur

- **Distributed RateLimit** (Faz 6+ / scale)
  - Faz 5.7'de in-memory limiter (tek instance bağımlı)
  - Çoklu instance deploy'da (load balancer arkasında) limit per-instance — gerçek limit 5*N olur
  - Çözüm: Redis-backed RateLimiter (StackExchange.Redis + custom partition store)
  - Tetik: Production'da çoklu instance ihtiyacı

- **CSP Report-URI / report-to** (Faz 6+ / monitoring)
  - Faz 5.7'de CSP enforce mode (block); violation log'u yok
  - Production'da csp-report-only header eklenip /api/csp-report endpoint kurulabilir
  - Violation analytics → Sentry/Datadog
  - Tetik: Production CSP tuning

- **HSTS preload list** (Faz 6 / yayın)
  - Faz 5.7'de HSTS aktif (UseHsts default) ama "preload" directive yok
  - Browser preload listesine domain eklenirse ilk request bile HTTPS olur
  - Şart: Domain HTTPS-only, includeSubDomains, max-age >= 1 yıl, hstspreload.org'a kayıt
  - Tetik: Müşteri canlıya alma sonrası

- **ContactMessage retention policy** (Faz 6+ / KVKK)
  - Mesajlar süresiz saklanıyor; KVKK uyumluluk için retention (6 ay/1 yıl sonra otomatik anonimleştir veya sil)
  - Hangfire/scheduled job pattern
  - Tetik: KVKK denetimi veya yasal danışmanlık talebi

---

## Faz 6 → Yayına Alma

- **Müşteri içerik tamamlanınca meta description revize** (Yayın öncesi son tur)
  - Faz 6.16'da generic/örnek meta description'lar set edildi (Page seed + view literal'leri)
  - Müşteri gerçek içerik geldiğinde her sayfa için anahtar kelime optimize meta description yazılmalı
  - `MetaTagsHelpers.ResolveDescription` length-aware fallback sigorta: explicit boş/kısa olsa bile Excerpt/Content'ten otomatik çıkar
  - Mevcut LocalDB'de Page/Service/Article entity'lerinde kısa MetaDescription değerleri varsa admin panelden uzatma gerekir (seed key-bazlı idempotent, otomatik update etmez)
  - Tetik: Müşteri içerik teslim turundan sonra


- **Service detay JSON-LD** (Faz 5.4 / Faz 6)
  - Faz 5.2'de Article + Person + FAQPage işlendi; Service detay için LegalService alt-tipi (örn. ProfessionalService) düşünülebilir
  - Service detay zaten LegalService'in bir parçası; ayrı schema marjinal ek değer
  - Tetik: Faz 5.4 breadcrumb turunda yeniden değerlendir

- **Admin topbar dropdown click bug** (Faz 6+ / UI cleanup)
  - Faz 6.8 manuel teyit sırasında tespit edildi: sağ üst kullanıcı avatarına tıklamada dropdown açılmıyor
  - Veri toplandı: Bootstrap yüklü, instance kayıtlı, manuel `dd.show()` çalışıyor, ama doğal click event element'e ulaşmıyor (document capture listener bile yakalamıyor)
  - Denenen fix'ler: CSP `connect-src` genişletme (Bootstrap source map fetch için), `<a href="#">` → `<button type="button">` migration — ikisi de çözmedi
  - Kök sebep belirsiz: muhtemelen Tabler theme + Bootstrap entegrasyon detayı, browser-level event capture problemi, veya başka subtle bir konflikt
  - Geçici çözüm: Sidebar'a Profilim + Çıkış Yap item'ları eklendi (`_AdminSidebar.cshtml`, dropdown bypass) — kullanıcı pratik olarak logout'a erişebilir
  - Tetik: Faz 7 / UI cleanup turu veya başka bir admin sayfasında benzer dropdown gerektiğinde

- **Dashboard widget genişletme — kalan widget'lar** (Spec madde 5.1, Faz 7)
  - Faz 6.21'de eklendi: "Son Makaleler" + "Site Özeti" widget'ları (`IDashboardService` + 2 DTO + view card)
  - KALAN widget'lar (404 sayısı, aktivite logları, ziyaretçi özeti) altyapı gerektiriyor —
    bkz. **Faz 7 → Admin Zenginleştirme** (Grup A: 404 Takibi + Aktivite Logu; Grup B: ziyaretçi/GA4)
  - Faz 6.21 keşfi netleştirdi: bu widget'lar "UI işi" değil; ilgili entity/middleware/audit
    altyapısı kurulduktan sonra dashboard'a kart eklemek marjinal kalır

---

## Faz 7 → Admin Zenginleştirme (yayın sonrası)

> Kaynak: 6.22-keşif turu (Hafriyat panel referans incelemesi). Hafriyat ile
> Küçükmeriç aynı framework ailesi ama farklı mimari (Hafriyat tek-proje,
> Business katmanı/soft-delete/i18n yok) — kod drop-in kopyalanamaz, her
> özellik Küçükmeriç katmanlarına re-home edilir (~%30 şema/kavram taşınır,
> ~%70 yeniden yazım).

### Grup A — İç araçlar (Hafriyat'ta kısmen var, dış bağımlılık yok)

- **404 Takibi** — Hafriyat'ta YOK, sıfırdan
  - NotFoundLog entity + migration + 404 logging middleware + repo/UoW +
    Business servis + admin liste/filtre + "tek-tık 301 kur" akışı
  - Tahmini: ~2-3 alt-adım

- **Aktivite Logu** — Hafriyat'ta scaffold var ama interceptor wire EDİLMEMİŞ (çalışmıyor)
  - AuditLog entity + SaveChangesInterceptor (Küçükmeriç AppDbContext'e wire) +
    filtre/pagination + admin liste
  - Hafriyat interceptor mantığı iyi referans (eski/yeni JSON capture)
  - Serilog'un da yapılandırılması gerekebilir (şu an referanslı ama config yok)
  - Tahmini: ~2 alt-adım

- **Aboneler** — Hafriyat'ta var, çalışıyor, basit (en kolay adapte)
  - Subscriber entity + repo/UoW + Business servis + Result<T> + FluentValidation +
    public abone formu (Turnstile/honeypot pattern) + admin liste + token ile iptal
  - Tahmini: ~1-2 alt-adım

### Grup B — Dış API entegrasyonu (Hafriyat'ta HİÇ YOK — sıfırdan + müşteri-bağımlı)

- **GA4 Data API widget** — Google.Apis.AnalyticsData.v1beta + OAuth/service account
- **Search Console API widget** — Google.Apis.SearchConsole.v1
- **PageSpeed Insights widget** — PageSpeed API key + per-URL job
- Ortak blocker: Google Cloud projesi, OAuth client veya service account,
  API key/quota — müşteri tarafı kurulum gerektirir
- Hafriyat'ta adapte edilecek kaynak kod SIFIR — tamamı yeni entegrasyon
- Tahmini: büyük, ayrı bir entegrasyon paketi

### Grup C — Yönlendirmeler (Küçükmeriç'te HİÇ YOK — sıfırdan, Hafriyat iyi referans)

- **Redirect modülü** — Küçükmeriç'te redirect özelliği hiç yok
  - Redirect entity + SlugHistory entity + RedirectMiddleware (manuel tablo +
    otomatik slug history) + admin CRUD + döngü kontrolü (AJAX loop-check)
  - Hafriyat'ta tam çalışır halde — middleware + loop-check mantığı düz adapte edilebilir
  - 404 Takibi (Grup A) ile entegre: 404 → tek-tık 301 kur akışı
  - Tahmini: ~2-3 alt-adım

### Notlar

- Grup C'deki diğer kalemler (Site Ayarları, Dashboard, Mesajlar/Medya/Galeri):
  Küçükmeriç zaten eşdeğer/üstün — yapılacak bir şey yok
- Tetik: Faz 6 tamamlanıp site yayına çıktıktan sonra; Faz 7 başında bu
  bölüm sıralı alt-adım planına dönüştürülür

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

- **PROGRESS.md Faz 6 tablosu senkron borcu** (Faz 6 kapanışı / 6.26)
  - PROGRESS.md "Tamamlanan Adımlar" tablosu 6.17/#33'te duruyor — 6.18-6.22
    satırları işlenmemiş (6.18 belge senkron, 6.19 turnstile, 6.20 csp nonce,
    6.21 dashboard widget, 6.22 randevu modülü)
  - Tek tek eklemek yerine Faz 6 kapanışında (6.26) toplu doc-sync turunda
    tablo güncellenir
  - CLAUDE.md §11 + DEFERRED.md güncel — sadece PROGRESS.md tablosu geride
  - Tetik: Faz 6 kapanış adımı (6.26)
