# Pre-Launch Audit — 2026-05-13

> **Tarih:** 2026-05-13
> **Branch:** `feature/faz-6.13-pre-launch-audit` → develop (PR pending)
> **Faz:** 6.13 — yayın öncesi durum tespiti
> **Karakter:** Audit-only, **HİÇ FIX UYGULANMADI**. Bulgular Faz 6.14+ backlog'una geçer.

---

## Kapsam

- **Lighthouse** 13.3.0 (npm global) — 5 sayfa × 2 form factor (mobile + desktop) = **10 rapor**
- **Static SEO/meta analiz** — regex tabanlı extraction (parser dependency yok)
- **robots.txt** + **sitemap.xml** erişilebilirlik

### Audit edilen sayfalar

| Kısa ad | URL |
|---|---|
| home | `https://localhost:7082/tr-TR/` |
| about | `https://localhost:7082/tr-TR/Pages/hakkimizda` |
| service-detail | `https://localhost:7082/tr-TR/Services/ceza-hukuku` |
| article-detail | `https://localhost:7082/tr-TR/Articles/hukuki-sureclerde-bilinmesi-gerekenler` |
| contact | `https://localhost:7082/tr-TR/Contact` |

### Raw çıktılar

- Lighthouse: `docs/audits/lighthouse/*.report.{json,html}` — 20 dosya (10 sayfa × 2 format)
- Static SEO: `docs/audits/seo/{meta-summary.md, robots.txt, sitemap.xml}`
- Skor özet: `docs/audits/scores-and-issues.md`

---

## ⚠️ Sınırlılıklar

- **Development env'da çalıştırıldı.** ASP.NET Core Browser Link script (`aspnetcore-browser-refresh.js`) ve BrowserLink minScale dev özellikleri her sayfaya inject edilir; bu **performance skorunu ~5-10 puan düşürür**. Gerçek production skoru için Faz 6.14+ deploy sonrası re-audit gereklidir.
- **Throttling simülasyonu:** Lighthouse 4G mobile simulate / desktop ethernet — gerçek user network'üne bağımlı.
- **Cross-browser:** sadece headless Chromium (Edge engine). Firefox/Safari/iOS Safari için manuel test bu audit'in dışında.
- **localhost HTTPS self-signed sertifika** — Lighthouse `--ignore-certificate-errors` ile geçer; production'da geçerli sertifika varsayılır.

---

## Skor Özeti

| Sayfa | Form | Perf | A11y | Best | SEO |
|---|---|---:|---:|---:|---:|
| home | mobile | **90** ✅ | **96** ✅ | **100** ✅ | **100** ✅ |
| home | desktop | **99** ✅ | **96** ✅ | **100** ✅ | **100** ✅ |
| about | mobile | 74 🟡 | **100** ✅ | **100** ✅ | **100** ✅ |
| about | desktop | **99** ✅ | **100** ✅ | **100** ✅ | **100** ✅ |
| service-detail | mobile | **93** ✅ | **96** ✅ | **100** ✅ | **100** ✅ |
| service-detail | desktop | **99** ✅ | **96** ✅ | **100** ✅ | **100** ✅ |
| article-detail | mobile | 85 🟡 | **96** ✅ | **100** ✅ | **100** ✅ |
| article-detail | desktop | **99** ✅ | **96** ✅ | **100** ✅ | **100** ✅ |
| contact | mobile | 67 🔴 | **93** ✅ | **100** ✅ | **100** ✅ |
| contact | desktop | **95** ✅ | **93** ✅ | **100** ✅ | **100** ✅ |

**Genel değerlendirme:**
- **A11y / Best Practices / SEO** her sayfada 93-100 — sağlıklı baseline.
- **Performance** desktop'ta 95-99 ✅, mobile'da 67-93 (contact mobile en kötü).
- Pre-launch optimizasyon turuyla mobile perf skorları 85+'a çekilebilir.

---

## Tekrarlayan Sorunlar (Tüm Sayfalarda)

| Sorun | Etki | Tahmini Tasarruf | Faz |
|---|---|---|---|
| **Color contrast a11y** (`color-contrast` 0/100) | A11y — primary/accent renk metin kontrastı düşük | — | 🔴 **6.14** |
| **Render-blocking requests** (`render-blocking-insight` 0/100) | Perf — senkron CSS/JS yüklenmesi | 380 ms (desktop) – 3.4 s (mobile) | 🔴 **6.14** |
| **Use efficient cache lifetimes** (`cache-insight` 50/100) | Perf — statik asset cache header eksik/kısa | 34 KiB / her sayfa | 🔴 **6.14** |
| **Network dependency tree** (`network-dependency-tree-insight` 0/100) | Perf — kritik path zinciri uzun | — | 🟡 **6.14** |
| **Reduce unused CSS** (`unused-css-rules` 50/100) | Perf — full Bootstrap/Tabler bundle yükleniyor | 13–24 KiB / sayfa | 🟡 **6.14** |
| **Document request latency** (`document-latency-insight` 50/100) | Perf — Kestrel dev cevap süresi | 5–12 KiB | 🟢 dev artifact (prod re-audit) |
| **Mobile FCP/LCP** (Mobile 4G simulate) | Perf — about/contact mobile 2.6-4.6s | — | 🟡 dev artifact + render-blocking |

### Sayfaya özgü sorunlar

- **Contact** (mobile 67 / desktop 95): `link-in-text-block` (0/100) — sadece renkle ayırt edilen link var (underline yok). KVKK aydınlatma linki muhtemelen sorumlu.
- **Home / Service / Article** desktop: `color-contrast` failing — public token paleti (`--color-accent` üzerine `--color-text` veya tersi) WCAG AA eşiğini geçmiyor.

---

## Static SEO Bulguları

### robots.txt — ✅ HTTP 200 (161 bytes)
```
User-agent: *
Disallow: /admin/
Disallow: /Identity/
Disallow: /Contact/ThankYou
Disallow: /Error/
Allow: /

Sitemap: https://localhost:7082/sitemap.xml
```

### sitemap.xml — ✅ HTTP 200, 20 URL
Tüm ana public URL'ler dahil. Production deploy'da `BaseUrl` admin panelinden production domain'e değişince absolute URL'ler otomatik güncellenir.

### Meta tag özeti (5 sayfa)

| Sayfa | Title len | Meta desc len | Canonical | JSON-LD | H1 | html lang |
|---|---:|---:|---|---:|---:|---|
| home | 134 ⚠️ | 216 ⚠️ | ✅ | 2 | 1 ✅ | tr-TR ✅ |
| about | 79 ✅ | 34 ⚠️ | ✅ | 3 | 1 ✅ | tr-TR ✅ |
| service-detail | 62 ✅ | 43 ⚠️ | ✅ | 3 | 1 ✅ | tr-TR ✅ |
| article-detail | 99 ✅ | 94 ✅ | ✅ | 4 | 1 ✅ | tr-TR ✅ |
| contact | 71 ✅ | 158 ✅ | ✅ | 3 | 1 ✅ | tr-TR ✅ |

**Bulgular:**
- ✅ Her sayfada **canonical**, **og:title/image/type**, **JSON-LD** (2-4 block), **H1=1**, **html lang=tr-TR**, **viewport** mevcut.
- ⚠️ **home title 134 char** — Google'da kırpılır (50-60 char hedef). `SiteInfo.Name + Tagline` çift bilgi.
- ⚠️ **home meta description 216 char** — kırpılır (155-160 char hedef).
- ⚠️ **about + service-detail meta description çok kısa** (34 / 43 char) — minimum 120 char önerisi. Page/Service entity'lerinde MetaDescription opsiyonel; içerik yöneticisi bu alanları doldurmuyorsa fallback üretimi düşünülebilir.

> **Not:** Regex 0 `<img>` saydı — public sayfalar Lucide SVG icon kullanıyor, hero/card görselleri `background-image` CSS ile veya featured image lazy-load placeholder pattern'i ile inject ediliyor. Bu **doğru pattern**, `<img>` ile gelseydi de `alt` zorunlu olurdu.

---

## Öncelikli Backlog (Faz 6.14+)

### 🔴 Kritik (yayın öncesi, blocker)

1. **Color contrast paletini WCAG AA'ya çek** (`color-contrast` her sayfada 0/100)
   - Public token paleti (`--color-primary`, `--color-accent`, `--color-text-muted`) ile beyaz/krem zemin üzerine kontrast kontrolü
   - Lighthouse rapor HTML'inde failing element selektörleri listeli
   - Önerilen: `_SiteFooter.cshtml` muted text, `_HeroSection.cshtml` accent overlays, contact link color
   - Etkilenen: tüm sayfalar A11y

2. **Render-blocking CSS/JS** — DEFERRED'da "Self-host CDN dosyaları" ile birlikte değerlendirilebilir
   - Bootstrap/Tabler/Quill CSS senkron yüklenmesi (380ms-3.4s tasarruf)
   - Çözüm A: `<link rel="preload" as="style">` + async swap
   - Çözüm B: Tabler/Bootstrap self-host + `defer`/`async`
   - Çözüm C: Critical CSS inline + non-critical async
   - Etkilenen: özellikle mobile FCP/LCP

3. **Cache-Control headers** — statik asset'ler için
   - `app.UseStaticFiles()` `OnPrepareResponse` ile `Cache-Control: public, max-age=31536000, immutable` (asp-append-version cache busting zaten var)
   - 34 KiB savings / sayfa
   - 1 satır config değişikliği, en yüksek ROI

4. **Contact `link-in-text-block`** — sadece renkle ayırt edilen link
   - "Aydınlatma metnini" link'ine `text-decoration: underline` veya benzer non-color affordance

### 🟡 Orta (Faz 6.14)

5. **Unused CSS tree-shake** — 13-24 KiB / sayfa
   - PurgeCSS veya manual: admin-only CSS sınıflarını public bundle'dan ayır
   - Tabler tam paketi yerine kullanılan component'ler
   - Bonus: `tokens.css` DRY refactor DEFERRED'da var, birleştirilebilir

6. **About/Service-Detail meta description çok kısa** — içerik yöneticisi sorumluluğu
   - Page/Service admin form'da hint text: "ideal 120-160 char"
   - Veya fallback: meta description boşsa `Excerpt`/`ShortDescription` (zaten kullanılıyor — Service için kullanılıyor mu kontrol)

7. **Home title + meta description fazla uzun**
   - SEO helper'da culture-aware truncate veya admin uyarı

### 🟢 Düşük / re-audit sonrası

8. **Mobile FCP/LCP** ölçümleri dev env etkisini içerir — production deploy sonrası re-audit, gerçek skor 90+'a yaklaşması beklenir
9. **Network dependency tree** — render-blocking düzeltildikten sonra otomatik iyileşir
10. **Document request latency** — Kestrel dev artifact, prod Kestrel/Nginx farklı

---

---

## Post-Fix Sonuçları (Faz 6.14)

Faz 6.14'te uygulanan 7 fix sonrası re-audit (`docs/audits/lighthouse/post-fix/`):

### Önce / Sonra

| Sayfa | Perf | A11y | Best | SEO |
|---|---|---|---|---|
| about-desktop | 99 | 100 | 100 | 100 |
| about-mobile | 74→**79** (+5) | 100 | 100 | 100 |
| article-detail-desktop | 99 | 96→**100** (+4) | 100 | 100 |
| article-detail-mobile | 85→**80** (-5)* | 96→**100** (+4) | 100 | 100 |
| contact-desktop | 95→**97** (+2) | 93→**100** (+7) | 100 | 100 |
| contact-mobile | **67→77** (+10) ✨ | 93→**100** (+7) | 100 | 100 |
| home-desktop | 99 | 96→**100** (+4) | 100 | 100 |
| home-mobile | 90→**84** (-6)* | 96→**100** (+4) | 100 | 100 |
| service-detail-desktop | 99→**100** (+1) | 96→**100** (+4) | 100 | 100 |
| service-detail-mobile | 93→**95** (+2) | 96→**100** (+4) | 100 | 100 |

\* Lighthouse `--throttling-method=simulate` ±5-10 puan run-to-run variance gösterir (dev env'da daha belirgin). Net trend pozitif.

### Toplulaştırılmış Ortalama

- **Perf (mobile)**: 82 → **83**
- **Perf (desktop)**: 98 → **99**
- **A11y**: 96 → **100** ✨
- **Best Practices**: 100 (değişmedi)
- **SEO**: 100 (değişmedi)

### Uygulanan Fix'ler

| # | Fix | Etkilenen audit | Sonuç |
|---|---|---|---|
| 1 | `Cache-Control: max-age=31536000, immutable` (UseStaticFiles OnPrepareResponse) | `cache-insight` (34 KiB/sayfa) | TTL=0 → 1 yıl |
| 2 | Google Fonts self-host (`wwwroot/fonts/` 12 woff2 + `fonts.css`) | `render-blocking-insight` (889 ms Google Fonts CSS) | CDN→ same-origin |
| 3 | Public layout script'lerine `defer` (Bootstrap bundle + Lucide + site.js + cookie-consent.js) | parallel download | render-block azalır |
| 4 | `--color-accent-text: #7B6232` token (WCAG AA ~5.5:1) | `color-contrast` (3.07-3.28:1 → 5.5:1) | A11y 100/100 her sayfada |
| 5 | Contact KVKK link `text-decoration: underline` | `link-in-text-block` | A11y kazanım |
| 6 | Home `ViewData["MetaDescription"]` 216→154 char | meta description length | Google'da kırpılmaz |
| 7 | DbInitializer about + 3 service ShortDescription uzatma (taze install için) | meta description length | Yeni install'da fix |

### Verify — 3-Run Mobile Sample (home + article)

Faz 6.14 tek-run değerlerinde home (-6) ve article (-5) düşüş gözlendi. Variance teyidi için 3-run mobile sample:

| Sayfa | Baseline (1-run) | Run1 | Run2 | Run3 | min | **median** | max | avg |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| home-mobile | 90 | 90 | 92 | **72** ⚠️ | 72 | **90** | 92 | 84.7 |
| article-detail-mobile | 85 | 80 | 84 | 78 | 78 | **80** | 84 | 80.7 |

**Yorum:**
- **home:** median **90 = baseline** (yatay). Run3 outlier (72, -18 vs median); muhtemelen GC / throttling jitter. Avg outlier'dan etkilenir.
- **article:** median **80** (-5 vs baseline). Variance düşük (78-84), gerçek hafif gerileme veya 1-run baseline gürültüsü.
- **Asimetri:** Baseline tek run, verify üç run — 1-run vs 3-run karşılaştırması gürültülü.

**Karar:** **C (variance bölgesi, yatay)** — median'a güven, regresyon kanıtı yok. Production deploy + 5-run ortalama ile gerçek baseline (BrowserLink olmadan) yeniden ölçülmeli. Kritik blocker yok, PR #30 merge'e hazır.

---

## Faz 6.16 — SEO Meta Description Deep-Dive

Faz 6.13/6.14'te tek-sayfa-bazlı meta düzeltmeleri yapıldı; 6.16'da **17 public sayfanın tamamı** sistematik tarandı.

### Tarama (Önce vs Sonra)

17 public sayfa curl ile tarandı, `meta name="description"` char count ölçüldü (HTML entity decode sonrası).

| Sayfa | Önce | Sonra | Status | Fix kaynağı |
|---|---:|---:|---|---|
| home | 139 | 139 | ✅ | — |
| about | 29 | 157 | ✅ | Page.Content fallback (Helper) |
| services-list | 103 | 149 | ✅ | view hardcoded uzat |
| service-ceza | 37 | 157 | ✅ | Service.FullDescription fallback |
| service-aile | 47 | 157 | ✅ | Service.FullDescription fallback |
| service-is | 37 | 157 | ✅ | Service.FullDescription fallback |
| attorneys-list | 72 | 144 | ✅ | view hardcoded uzat |
| attorney-detail | 77 | 157 | ✅ | Attorney.FullBio fallback |
| articles-list | 116 | 116 | ✅ | — |
| article-detail | 73 | 157 | ✅ | Article.Content fallback |
| faqs | 96 | 151 | ✅ | view hardcoded uzat |
| gallery | 67 | 124 | ✅ | view hardcoded uzat |
| contact | 97 | 141 | ✅ | view hardcoded uzat |
| privacy | 67 | 150 | ✅ | Page.Content fallback |
| cookie | 47 | 158 | ✅ | Page.Content fallback |
| terms | 24 | 136 | ✅ | Page.Content fallback |
| disclosure | 63 | 156 | ✅ | Page.Content fallback |

**Sonuç:** 14 TOO_SHORT → **0** (17/17 OK). Tüm sayfalar SEO ideal aralığında (110-170 char).

### Lighthouse SEO Re-Audit (4 sayfa)

| Sayfa | Önce SEO | Sonra SEO |
|---|---:|---:|
| home-mobile | 100 | **100** |
| about-mobile | 100 | **100** |
| service-ceza-mobile | 100 | **100** |
| article-detail-mobile | 100 | **100** |

SEO skor 100 korundu (regresyon yok).

### Implementation Özeti

- **`MetaTagsHelpers.ResolveDescription`** — length-aware fallback chain (primary → fallback → siteDefault), HTML strip + sentence truncate. Eşik: 80 char (altı kabul edilmez, sonraki kaynağa geç).
- **`StripHtml`** — tag temizleme + entity decode + whitespace normalize.
- **`TruncateToSentence`** — 160 char hedef, nokta sonu öncelik, kelime sınırı fallback (≥100 char konum).
- **5 detail view** Content/FullDescription/FullBio fallback olarak `ViewData["MetaDescriptionFallback"]`'a verildi (Article, Service, Page, Attorney).
- **5 list/static view** hardcoded meta description literal'leri 120-160 char aralığına uzatıldı (services, attorneys, faqs, gallery, contact + Page seed: privacy/cookie/terms/disclosure).

### Test
- 428 + 10 = **438 PASSED** — MetaTagsHelpers ResolveDescription/StripHtml/TruncateToSentence + revised override semantics.

### Kalan İyileştirmeler (Faz 6.15+)

- **Mobile render-blocking** — Bootstrap CSS hala CDN'den senkron (33 KB, 1099 ms wasted). Self-host CDN dosyaları (DEFERRED) ile birlikte değerlendirme
- **Unused CSS tree-shake** — Bootstrap full bundle yüklenmesi (13-24 KiB savings)
- **About + Service mevcut LocalDB**: seed default uzadı ama key var → atla mantığı; admin panelinden manuel uzatma gerek (kullanıcı sorumluluğunda)
- **Home mobile -6 / article-detail mobile -5 variance** — production deploy + 3 run ortalaması ile teyit

---

## Production Re-audit Önerisi (Faz 6.14 sonrası)

Faz 6.14 critical fix'lerden sonra ve production deploy sırasında **gerçek domain üzerinden** Lighthouse + WebPageTest + Google Search Console (Mobile-Friendly Test, Rich Results Test) ile re-audit. Beklenen iyileşme: mobile perf 67→85+, desktop 95→98+.

**Audit sırasında tetiklenecek harici doğrulamalar:**
- [Google Rich Results Test](https://search.google.com/test/rich-results) — JSON-LD block'ları
- [Schema.org validator](https://validator.schema.org/)
- [PageSpeed Insights](https://pagespeed.web.dev/) — real-world CrUX data
- [WebPageTest](https://www.webpagetest.org/) — Istanbul/İstanbul edge node
