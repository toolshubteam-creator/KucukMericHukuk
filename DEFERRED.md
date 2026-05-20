# DEFERRED.md — Ertelenen İşler

> Şu anda yapılmayıp ileri bir adıma/faza bırakılan teknik işler.
> Tamamlandıkça buradan silinir, PROGRESS.md'ye geçer.
>
> **Kural:** Her adım başında bu dosya okunur — o adımda kapatılabilecek
> madde var mı kontrol edilir. Her adım sonunda yeni ertelemeler eklenir,
> kapatılanlar silinir. Detay için WORKING_STYLE.md.

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

- **Faz 4.10 / v0.4.1 — Bütüncül tasarım cila turu** (kısmen yapıldı — Faz 6.23)
  - Faz 4 tüm sayfalar tamamlandıktan sonra kullanıcı tarafından genel tasarım gözden geçirme talep edildi (09.05.2026)
  - **Faz 6.23'te yapıldı (C1-C5):** token DRY (ortak `tokens.css`), ölü CSS temizliği,
    hard-coded değerler → token, buton tutarlılığı, tipografi hiyerarşisi (ortak H1 ölçeği)
  - **KAPSAM DIŞI kalan — ayrı mini-cila turuna / Faz 7'ye:**
    * Kart stili tutarsızlığı (envanter 7.x — hover gölge/border/padding sapması;
      attorney-card'ın border + gölge yokluğu KASITLI tasarım olabilir, teyit gerek)
    * Responsive breakpoint hizalama (envanter 8.1 — 600px/480px → Bootstrap 576px;
      geniş görsel regresyon yüzeyi, ayrı dikkatli tur + kapsamlı mobile kontrol gerek)
    * rgba shadow tokenization (envanter 4.3 — shadow token sistemi tasarımı gerektirir)
    * Kart başlık font-size (envanter 6.3 — service-card 1.5rem vs diğerleri 1.25rem;
      kasıtlı vurgu olabilir, kullanıcı teyidi bekliyor)
  - Tetik: ayrı mini-cila turu veya Faz 7

- **Menü navigasyon fontu — logo/hero ile uyum** (Faz 6 mini-tur / cila kalanı)
  - Logo + hero başlık Playfair Display (serif); menü başlıkları Inter (sans-serif)
  - Spec'e teknik olarak uygun (Başlık=Playfair, Metin=Inter) ama logo ile menü aynı
    yatay hizada olduğundan göz "uyumsuz" okuyor — kullanıcı 6.23 teyidinde fark etti
  - Öznel tasarım kararı — FAZ 1 envanteri ölçülemez olduğu için yakalamadı
  - Çözüm yönü: dene-gör-seç — menü için 2-3 alternatif (Playfair'e geçir / Inter'de
    letter-spacing+weight ile karakter ver / olduğu gibi bırak), yan yana görülüp seçilir
  - Tetik: Faz 6 içinde mini-tur (örn. 6.24 sonrası) — kart 7.x / responsive 8.1 / shadow 4.3
    ile birlikte "cila kalanı" turunda toplanabilir

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

- **hstspreload.org submit** (Faz 6.26 deploy adımı)
  - Kod hazır (Faz 6.25): `AddHsts` → `Preload = true` + `IncludeSubDomains = true` + `MaxAge = 1 yıl` (preload list minimum)
  - Müşteri yayına çıkıp domain HTTPS-only kesinleşince hstspreload.org'a manuel submit
  - Şart: Domain HTTPS-only + tüm subdomain'ler HTTPS, geri-alma birkaç ay sürer
  - Tetik: Production deploy + SSL kesin

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

---

## 404 Takibi — UA/Bot Filtresi (opsiyonel iyileştirme)

- **Şu an:** TÜM 404 hit'leri kaydediliyor (bot/crawler dahil). NotFoundLoggingMiddleware (Faz 7.3.2a) UA filtresi UYGULAMIYOR — varsayılan kararı: site sahibi eski URL tarayan Google/Bing crawler'larını da görmek isteyebilir (redirect kurma kararına bilgi).
- **Sorun çıkarsa:** bot trafiği aggregate tabloyu şişirebilir. Aggregate zaten URL başına tek satır tutuyor (HitCount artar), ama yine de farklı URL'leri tarayan crawler binlerce satır oluşturabilir.
- **Çözüm yolları (gerekirse):**
  - User-Agent regex filtresi (örn. `bot|crawler|spider|googlebot|bingbot`) — appsettings veya admin Site Ayarları'nda toggle
  - 30 günlük TTL otomatik temizlik job — admin "Temizle" tuşunu manuel iş yapıyor şu an
- **Tetik:** Admin "404 Kayıtları" sayfasında gürültü rahatsız ederse, veya 7.4 Redirect modülünden sonra "hangi URL gerçekten kullanıcı, hangisi crawler" ayrımı önem kazanırsa.

## SmtpEmailSender Connection Reuse (Bülten Optimizasyonu) — Faz 7.2b-2'den ertelendi

- **Sorun:** Mevcut `SmtpEmailSender.SendAsync` her çağrıda yeni `SmtpClient` açıp `ConnectAsync` + `AuthenticateAsync` + `SendAsync` + `DisconnectAsync` yapıyor. Her email için ayrı TCP + TLS handshake.
- **Etki:** Küçük abone listesi (≤100) için kabul edilebilir; büyük listede (1000+) her mail başına ~500ms+ overhead → toplam gönderim süresi orantısız büyür, SMTP sunucusu rate limit'lerini zorlar.
- **Çözüm:** `IEmailBatchSender` arayüzü — tek connection açar, MailKit `SendAsync` pipelining ile abone başına mesaj gönderir, batch sonu disconnect. `NewsletterService.ProcessJobAsync` batch context'inde bu sender'ı kullanır.
- **Tahmini:** ~1 PR, Infrastructure/Email (yeni sender + IDisposable lifecycle) + ProcessJobAsync entegrasyonu + test (mock batch context).
- **Tetik:** Abone listesi 200+ büyüdüğünde veya performans turu. Şu an aktif abone ~4 — sorun yok.
- **Kaynak:** 7.2b-2 raporu (Claude Code işaretledi)

---

## CLAUDE.md ↔ AGENTS.md Senkron Borcu

- **AGENTS.md** (Faz 8'de eklendi) `CLAUDE.md`'nin Codex/AI-agent kopyası — ~%99 duplike içerik (sadece başlık + §3 dosya adı + §12 hitap farkı).
- Her mimari karar / bağımlılık değişikliğinde **iki dosya birlikte** güncellenmeli; yoksa zamanla ayrışır.
- **Tetik:** İleride `CLAUDE.md` değişikliğinin `AGENTS.md`'ye yansımadığı fark edilirse veya tek kaynağa sentezleme (ortak include / sembolik bağ) zamanı geldiğinde.

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
