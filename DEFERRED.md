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

- **Admin topbar dropdown click bug** (Faz 6+ / UI cleanup)
  - Faz 6.8 manuel teyit sırasında tespit edildi: sağ üst kullanıcı avatarına tıklamada dropdown açılmıyor
  - Veri toplandı: Bootstrap yüklü, instance kayıtlı, manuel `dd.show()` çalışıyor, ama doğal click event element'e ulaşmıyor (document capture listener bile yakalamıyor)
  - Denenen fix'ler: CSP `connect-src` genişletme (Bootstrap source map fetch için), `<a href="#">` → `<button type="button">` migration — ikisi de çözmedi
  - Kök sebep belirsiz: muhtemelen Tabler theme + Bootstrap entegrasyon detayı, browser-level event capture problemi, veya başka subtle bir konflikt
  - Geçici çözüm: Sidebar'a Profilim + Çıkış Yap item'ları eklendi (`_AdminSidebar.cshtml`, dropdown bypass) — kullanıcı pratik olarak logout'a erişebilir
  - **Faz 6.24 (15.05.2026) teşhis turu — kök sebep HÂLÂ belirsiz, ek kanıtlar:**
    * Bootstrap engine sağlam: `bootstrap.Dropdown.VERSION` 5.3.3, manuel `toggle()` çalışıyor (dropdown açılıyor, `show` class ekleniyor)
    * Doğal click sırasında toggle element'ine `focus` class'ı geliyor ama `show` eklenmiyor
    * Document'a kayıtlı 20+ click listener var: Bootstrap `event-handler.js` + `tabler.min.js`
    * **Hipotez TEST EDİLDİ:** `tabler.min.js` geçici devre dışı bırakıldı → dropdown hâlâ açılmadı. Yani Tabler JS dropdown delegation çakışması DEĞİL.
    * **Yan kazanım:** Tabler JS olmadan da admin tab geçişi, modal, SweetAlert, sidebar toggle çalışıyor — Bootstrap kendi başına yeterli (gelecekte Tabler JS'i tamamen kaldırma değerlendirme konusu olabilir, ama bu maddenin kapsamı değil)
  - **Sonraki teşhis turu için ipuçları:**
    * Bootstrap'in iç delegation'ı (`event-handler.js:91` + `.js:103`) neden `show` eklemiyor — `Dropdown.prototype.toggle()` içinde bir guard mı (örn. `_isShown` state tutarsızlığı, `_parent` null mu)
    * Browser-level: `pointer-events`, `::before`/`::after` overlay, `z-index` örtüşmesi (Faz 6.24'te adım 10 atlandı, ama adım 9 hiç çalışmadığı için bu yön incelenmedi)
    * Bootstrap 5.3.3'te bilinen bir bug var mı (issue/changelog tarama)
  - Tetik: yeni bir admin sayfasında benzer dropdown gerektiğinde, veya cila kalanı turunda

- **Dashboard widget genişletme — kalan widget'lar** (Spec madde 5.1, Faz 7)
  - Faz 6.21'de eklendi: "Son Makaleler" + "Site Özeti" widget'ları (`IDashboardService` + 2 DTO + view card)
  - KALAN widget'lar (404 sayısı, aktivite logları, ziyaretçi özeti) altyapı gerektiriyor —
    bkz. **Faz 7 → Admin Zenginleştirme** (Grup A: 404 Takibi + Aktivite Logu; Grup B: ziyaretçi/GA4)
  - Faz 6.21 keşfi netleştirdi: bu widget'lar "UI işi" değil; ilgili entity/middleware/audit
    altyapısı kurulduktan sonra dashboard'a kart eklemek marjinal kalır

---

## Faz 7 → Admin Zenginleştirme (plan'a dönüştü — bkz. PROGRESS.md)

> **Bu bölüm artık aktif plan.** PROGRESS.md Faz 7 → Alt-Adım Planı tablosunda
> 8 alt-adıma bölündü (7.1 Aktivite Logu → 7.8 kapanış). PageSpeed kapsam dışı,
> Grup B (GA4 + Search Console) domain-bağımsız geliştirilir. Bu bölüm Faz 7
> sonu (7.8) kapanışta tamamen silinecek; o güne kadar Hafriyat referans envanteri
> yapılaşırken kaynak olarak burada kalıyor.

> Kaynak: 6.22-keşif turu (Hafriyat panel referans incelemesi). Hafriyat ile
> Küçükmeriç aynı framework ailesi ama farklı mimari (Hafriyat tek-proje,
> Business katmanı/soft-delete/i18n yok) — kod drop-in kopyalanamaz, her
> özellik Küçükmeriç katmanlarına re-home edilir (~%30 şema/kavram taşınır,
> ~%70 yeniden yazım).

### Grup A — İç araçlar (Hafriyat'ta kısmen var, dış bağımlılık yok)

- **✅ 404 Takibi** — Faz 7.3'te tamamlandı (18.05.2026)
  - 7.3.1: NotFoundLog entity (aggregate Url + HitCount + LastSeenAt) + race-safe upsert repo + migration
  - 7.3.2a: NotFoundLoggingMiddleware (UseStatusCodePagesWithReExecute'ten ÖNCE, IStatusCodeReExecuteFeature.OriginalPath ile orijinal URL yakalama, filtreler: asset/admin/Error/GET-only, 850+ truncate)
  - 7.3.2b: NotFoundService + admin "404 Kayıtları" liste (filtre/sort) + Temizle (tek + tümü, hard-delete)
  - 17 yeni test (7 repo + 9 integration + 5 service), 569 total PASSED
  - "Tek-tık 301 kur" akışı 7.4.3b'de NotFoundLog → Redirect köprüsüyle kapatıldı

- **✅ Aktivite Logu** — Faz 7.1'de tamamlandı (15.05.2026)
  - AuditLog entity + AuditSaveChangesInterceptor + ICurrentUserAccessor + admin liste/detay
  - Ignore listesi: Identity, AuditLog, ContactMessage/Appointment/Subscriber (public formlar)
  - Modified delta, soft-delete→Deleted, restore→Restored mantığı çalışıyor
  - 14 yeni test (6 unit + 8 integration), 488 total PASSED

- **✅ Aboneler+Bülten** — Tam tamamlandı (Faz 7.2a + 7.2b-1 + 7.2b-2, 16.05.2026)
  - 7.2a: Subscriber entity + public abone formu (footer band, Turnstile/honeypot/KVKK/rate-limit) + admin liste/sil + token ile iptal
  - 7.2b-1: NewsletterJob entity + Article.NewsletterSentAt flag + mail HTML şablonu + admin "Bülten" modülü (bekleyen liste + önizleme + Pending job oluşturma)
  - 7.2b-2: NewsletterService.ProcessJobAsync batch motor + NewsletterDispatcher (Task.Run + IServiceScopeFactory) + admin "Gönder" buton + SMTP rate limit + abone-bazlı hata toleransı + Article.NewsletterSentAt Completed'da set, Failed'da set ETMEZ

### Grup B — Dış API entegrasyonu (Hafriyat'ta HİÇ YOK — sıfırdan + müşteri-bağımlı)

- **✅ Ortak Google altyapı** — Faz 7.5'te tamamlandı (19.05.2026): Google.Apis.Auth + `IGoogleApiClient` + credential okuma + SiteSettings Google Entegrasyonu tab
- **GA4 Data API widget** — Faz 7.6; Property ID boş-state + veri çekme
- **Search Console API widget** — Faz 7.7; Site URL boş-state + veri çekme
- **PageSpeed Insights widget** — Faz 7 kapsamı dışı (kullanıcı kararı; PROGRESS.md karar notu)
- Ortak blocker: Google Cloud projesi ve service account yetkileri — müşteri tarafı kurulum gerektirir
- Hafriyat'ta adapte edilecek kaynak kod SIFIR — tamamı yeni entegrasyon
- 7.6/7.7 için Google API specific NuGet paketleri ilgili adımda eklenir (`Google.Apis.AnalyticsData.v1beta`, `Google.Apis.SearchConsole.v1`)

### Grup C — Yönlendirmeler (Küçükmeriç'te HİÇ YOK — sıfırdan, Hafriyat iyi referans)

- **✅ Redirect modülü** — Faz 7.4'te tamamlandı (19.05.2026)
  - ✅ 7.4.1 (18.05.2026): Redirect + SlugHistory entity + repo + migration
  - ✅ 7.4.2 (18.05.2026): RedirectMiddleware (pipeline NotFoundLogging'den önce) + IMemoryCache + POC
  - ✅ 7.4.3a (18.05.2026): SlugHistoryService + 6 servis update kanca (Article pilot + Page/Service/Attorney/Category/Tag) + RedirectController admin CRUD + insert-time cycle validation (max 10 hop) + AJAX loop-check + cache invalidation + Yönlendirmeler birleşik liste (Manuel + SlugHistory Tür kolonu)
  - ✅ 7.4.3b (18.05.2026): NotFoundLog → "Redirect Kur" tek-tık köprü (7.3↔7.4 birleşme tamam) — SweetAlert2 input modal, başarıda 404 satırı çözüldü silinir
  - ✅ 7.4.4 (19.05.2026): Faz 7.4 kapanış — DEFERRED + PROGRESS + CLAUDE senkron; `SluggedEntityType` taşıma teyidi kapatıldı

### Notlar

- Grup C'deki diğer kalemler (Site Ayarları, Dashboard, Mesajlar/Medya/Galeri):
  Küçükmeriç zaten eşdeğer/üstün — yapılacak bir şey yok
- Tetik: Faz 6 tamamlanıp site yayına çıktıktan sonra; Faz 7 başında bu
  bölüm sıralı alt-adım planına dönüştürülür

---

## 404 Takibi — UA/Bot Filtresi (opsiyonel iyileştirme)

- **Şu an:** TÜM 404 hit'leri kaydediliyor (bot/crawler dahil). NotFoundLoggingMiddleware (Faz 7.3.2a) UA filtresi UYGULAMIYOR — varsayılan kararı: site sahibi eski URL tarayan Google/Bing crawler'larını da görmek isteyebilir (redirect kurma kararına bilgi).
- **Sorun çıkarsa:** bot trafiği aggregate tabloyu şişirebilir. Aggregate zaten URL başına tek satır tutuyor (HitCount artar), ama yine de farklı URL'leri tarayan crawler binlerce satır oluşturabilir.
- **Çözüm yolları (gerekirse):**
  - User-Agent regex filtresi (örn. `bot|crawler|spider|googlebot|bingbot`) — appsettings veya admin Site Ayarları'nda toggle
  - 30 günlük TTL otomatik temizlik job — admin "Temizle" tuşunu manuel iş yapıyor şu an
- **Tetik:** Admin "404 Kayıtları" sayfasında gürültü rahatsız ederse, veya 7.4 Redirect modülünden sonra "hangi URL gerçekten kullanıcı, hangisi crawler" ayrımı önem kazanırsa.

---

## Route Dili Tutarsızlığı (TR/EN karışık)

- **Sorun:** Public route'lar çoğunlukla İngilizce (`/Contact`, `/Appointment`, `/Articles`, `/Services`, `/Attorneys`, `/Pages`...) ama Referanslar + Galeri Türkçe route kullanıyor. Kullanıcı `/tr-TR/` kültüründe ama URL segmentleri dil karışık — tutarsız.
- **ÖNCE KEŞİF GEREK:** neden 2 sayfa TR diğerleri EN — kasıt mı (örn. SEO için belirli sayfalar TR) yoksa tutarsızlık mı (Faz 4 routing kararı). DEFERRED'da veya kod yorumunda kasıt notu olabilir — kontrol edilmeli.
- **SEO etkisi:** Route değişirse eski URL'ler kırılır → 301 redirect gerekir. Faz 7.4 Redirect modülü ile entegre düşünülmeli (canonical URL'ler + slug history).
- **Tahmini:** önce keşif (~30 dk), sonra karar — kapsam keşfe bağlı. Tüm İngilizce'ye/Türkçe'ye taşıma ise ~5-10 dosya + 301 mapping.
- **Tetik:** 7.2b sonrası ayrı keşif adımı (veya kullanıcı önceliklendirir).

---

## SmtpEmailSender Connection Reuse (Bülten Optimizasyonu) — Faz 7.2b-2'den ertelendi

- **Sorun:** Mevcut `SmtpEmailSender.SendAsync` her çağrıda yeni `SmtpClient` açıp `ConnectAsync` + `AuthenticateAsync` + `SendAsync` + `DisconnectAsync` yapıyor. Her email için ayrı TCP + TLS handshake.
- **Etki:** Küçük abone listesi (≤100) için kabul edilebilir; büyük listede (1000+) her mail başına ~500ms+ overhead → toplam gönderim süresi orantısız büyür, SMTP sunucusu rate limit'lerini zorlar.
- **Çözüm:** `IEmailBatchSender` arayüzü — tek connection açar, MailKit `SendAsync` pipelining ile abone başına mesaj gönderir, batch sonu disconnect. `NewsletterService.ProcessJobAsync` batch context'inde bu sender'ı kullanır.
- **Tahmini:** ~1 PR, Infrastructure/Email (yeni sender + IDisposable lifecycle) + ProcessJobAsync entegrasyonu + test (mock batch context).
- **Tetik:** Abone listesi 200+ büyüdüğünde veya performans turu. Şu an aktif abone ~4 — sorun yok.
- **Kaynak:** 7.2b-2 raporu (Claude Code işaretledi)

---

## Turnstile JS Tek Yükleme (Centralize) — Faz 7.2a-fix'ten ertelendi

- **Sorun:** Turnstile JS şu an 3 farklı yerden yükleniyor:
  - `Views/Contact/Index.cshtml` (@section Scripts)
  - `Views/Appointment/Index.cshtml` (@section Scripts)
  - `Views/Shared/_SubscribeBand.cshtml` (her public sayfada render)
- Cloudflare `api.js` idempotent (`window.turnstile` set ediyor, double-init yok) ama **network request duplicate** — Contact/Appointment sayfalarında 2 kez yüklenme
- **Çözüm:** `_PublicLayout` body sonunda conditional (TurnstileOptions.Enabled) tek yükleme; Contact/Appointment + _SubscribeBand'dan @section Scripts/inline script kaldır
- **Tahmini:** ~4 dosya edit, ~10 satır, 1 PR (~30 dk)
- **Tetik:** 7.2a-fix sonrası ayrı küçük refactor (veya Faz 6.23 cila kalan turuyla birlikte)

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

- **PROGRESS.md Faz 6 tablosu senkron borcu** (Faz 6 kapanışı / 6.26)
  - PROGRESS.md "Tamamlanan Adımlar" tablosu 6.17/#33'te duruyor — 6.18-6.22
    satırları işlenmemiş (6.18 belge senkron, 6.19 turnstile, 6.20 csp nonce,
    6.21 dashboard widget, 6.22 randevu modülü)
  - Tek tek eklemek yerine Faz 6 kapanışında (6.26) toplu doc-sync turunda
    tablo güncellenir
  - CLAUDE.md §11 + DEFERRED.md güncel — sadece PROGRESS.md tablosu geride
  - Tetik: Faz 6 kapanış adımı (6.26)
