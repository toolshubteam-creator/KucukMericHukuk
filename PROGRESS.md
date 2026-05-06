# PROGRESS.md — Faz İlerleme Kayıtları

> Her faz tamamlandığında bu dosya güncellenir. Bir sonraki faza aktarılan notlar, bilinen sorunlar ve test sonuçları burada tutulur.

---

## Genel Plan

| Faz | İçerik | Süre | Durum |
| --- | --- | --- | --- |
| 0 | Hazırlık (içerik, marka, kararlar) | 3-5 gün | 🔄 Devam ediyor |
| 1 | Proje kurulumu & mimari | 1 hafta | ✅ Tamamlandı |
| 2 | Yönetim paneli iskelet & temel modüller | 2 hafta | ✅ Tamamlandı |
| 3 | İçerik yönetimi modülleri | 2 hafta | ⏳ Beklemede |
| 4 | Frontend tasarım & geliştirme | 3 hafta | ⏳ Beklemede |
| 5 | SEO, entegrasyon, güvenlik | 1 hafta | ⏳ Beklemede |
| 6 | Test, düzeltme, yayına alma | 1 hafta | ⏳ Beklemede |

**Toplam:** 10 hafta (+2 hafta tampon önerisi)

---

## FAZ 2 — Yönetim Paneli (Admin CMS) — ✅ TAMAMLANDI

**Başlama Tarihi:** 05.05.2026
**Tamamlanma Tarihi:** 06.05.2026
**Tag:** `v0.2.0`

### Tamamlanan Adımlar

| Adım | Açıklama | Commit |
| --- | --- | --- |
| 2.1 | Admin Area iskeleti + Tabler.io 1.4.0 entegrasyonu | `d10e3b6` |
| 2.2 | Cookie auth + Login/Logout + AntiForgery | `08f9a35` |
| 2.3 | DbInitializer + 3 rol seed + ilk admin + Claims factory | `2255a87` |
| 2.4a | Result<T> + Error + FluentValidation altyapısı | `c0cc332` |
| 2.4b | Slug helper + SlugService + 6 repo SlugExistsAsync | `7fa445a` |
| 2.5a | PageService + Validator + birim testler | `17bb4cb` |
| 2.5b | PagesController + Index/Details (read-only) | `418568b` |
| 2.5c-i | Page form akışı + Quill + HtmlSanitizer | `3f21faa` |
| 2.5c-ii | Page destructive akışlar + SweetAlert2 | `1640f28` |
| 2.5e | Tag IsActive eklenmesi + 4 config HasDefaultValue | `314d437` |
| 2.6a | TagService + Validator + birim testler | `27f1936` |
| 2.6b | Tag UI + ArticleCount projection | `a9a38ab` |
| 2.7a | ServiceService + Validator + M:N attorney sync + birim testler | `ae34ba7` |
| 2.7b | Service UI + Attorney checkbox list + Quill | `4fc18db` |
| 2.7c | Service destructive akışlar | `cab0d62` |
| 2.8a | CategoryService + Validator + hierarchy guards + birim testler | `0900b25` |
| 2.8b | Category UI + parent dropdown indent + circular guard | `eb700e3` |
| 2.8c | Category destructive akışlar + HasChildren error UI | `1b136ac` |
| 2.9a | AttorneyService + UserId guard + M:N service sync + bio sanitize | `201e1dd` |
| 2.9b | Attorney admin UI + user picker + foto preview | `3cd5488` |
| 2.9b-fix | ServiceIds/AttorneyIds validation UI feedback | `3f33066` |
| 2.9c | Attorney destructive akışlar | `dec1c7d` |
| 2.10a | 5 modül _Form partial + IsUniqueConstraintViolation extension + Web DI | `9da8e9d` |
| 2.10b | Page+Service controller hizalama + MVC implicit-required suppress | `7f553b8` |
| 2.10b-tag-fix | TagsController LogWarning + FirstError.Message hizalama | `c2abe61` |
| 2.10c | UI polish: client-side validation + sidebar active + Quill partial + Category restore guard | `47c9d40` |
| 2.10c-fix | 5 modül FormViewModel validator (client-side validation aktif) | `67c2a3a` |
| 2.11 | Integration testler (anonymous-auth-CRUD round-trip, SQLite in-memory) | `27f8f09` |

### Yapılanlar

**Altyapı (2.1–2.4)**

- Admin Area + Tabler 1.4.0 + 401/403/404 sayfaları
- Cookie auth + Login/Logout + AntiForgery + AppUserClaimsPrincipalFactory (FullName claim)
- DbInitializer + 3 rol (Admin/Editor/Author) + admin user seed (configuration'dan, idempotent)
- `Result<T>` pattern + `Error` + `ErrorCodes` + `BusinessException`/`NotFoundException`
- FluentValidation manuel registration (`AddBusiness` scan, AutoValidation YOK)
- `SlugHelper` (Türkçe transliteration) + `ISlugService` + 6 repository `SlugExistsAsync`

**Modüller (2.5–2.9) — 5 modül full CRUD**

- **Page** modülü: statik sayfa içerikleri + Quill editor + HtmlSanitizer
- **Tag** modülü: makale etiketleri + ArticleCount projection
- **Service** modülü: hizmet alanları + M:N Attorneys + Quill
- **Category** modülü: hierarchy + parent dropdown indent + circular/self-parent guard
- **Attorney** modülü: profil + UserId guard + M:N Services + foto preview + ProfileImage placeholder

  Her modülde:
  - Read (Index/Details) + Write (Create/Edit) + Destructive (Delete/Restore/HardDelete)
  - Çoklu dil çevirileri (tr-TR mevcut, çoklu dil altyapı hazır)
  - SweetAlert2 confirm dialogları
  - Server-side ve client-side FluentValidation
  - HtmlSanitizer ile XSS koruması (Quill alanları)

**Cleanup ve Polish (2.10)**

- 2.10a: 5 modül `_<X>Form` partial refactor (−318 net satır)
- 2.10a: `IsUniqueConstraintViolation` extension method (5 service kopyası → ortak helper)
- 2.10a: `Web/DependencyInjection.cs` (`AddWebMappings` + ileride `AddWebValidators`)
- 2.10b: Page+Service controller hizalama (`FirstError.Message` + `LogWarning`)
- 2.10b: MVC implicit-required suppress (FluentValidation tek mesaj kaynak)
- 2.10b-tag-fix: TagsController da aynı pattern'e hizalandı (5 modül uniformity)
- 2.10c: FluentValidation client-side adapter
- 2.10c: Sidebar aktif state (`IsActiveController` HtmlHelper extension)
- 2.10c: Quill `_QuillStyles` + `_QuillScripts` partial (CDN tek noktadan)
- 2.10c: Category Restore parent IsDeleted guard + `Category.ParentDeleted` error code
- 2.10c-fix: 5 modül FormViewModel validator (client-side validation aktif: `data-val-required`, `data-val-maxlength`, `data-val-regex` attribute'ları üretilir)

**Integration Test (2.11)**

- Yeni proje: `tests/KucukMericHukuk.IntegrationTests`
- `WebApplicationFactory<Program>` + SQLite in-memory + admin user seed
- 13 test: Anonymous access redirects (7), Login flow (3), Page CRUD round-trip (3)
- Cookie `SecurePolicy.SameAsRequest` test override (production `Always` korundu)
- Türkçe encoding tuzağı tespit edildi → CLAUDE.md'ye not eklendi

### İstatistikler

- **Toplam commit (Faz 2):** 29 (feature + fix + refactor + docs)
- **Toplam test:** 180 PASSED, 0 failed (167 birim + 13 integration)
- **Build durumu:** 0 error, 0 warning
- **Yeni proje sayısı:** 1 (IntegrationTests)
- **Yeni dosya:** ~120, **Modified:** ~80
- **Net kod satırı:** Refactor sonrası ~9000+ satır C# + Razor

### Bilinen Sorunlar / Geçici Çözümler

- **FluentValidation `When()` conditional kurallar HTML attribute üretmez**: Attorney `Email` gibi koşullu kurallar client-side data-val-* almaz; server-side fallback ile çalışır.
- **`SetValidator` (RuleForEach Translations) `Translations[0]...` index-bazlı**: çoklu dil eklendiğinde tekrar değerlendirilmeli.
- **FormViewModelValidator kuralları InputValidator'lardan kopya**: DRY ihlali kabul edildi (kural değişirse 2 yerde güncelle); Faz 5'te Mapster mapping'li alternatif değerlendirilebilir.
- **`Cookie.SecurePolicy = Always` test ortamında HTTP cookie engelliyor**: test factory'de `SameAsRequest` PostConfigure override (production kodu DOKUNULMAZ).
- **Soft-deleted parent translation slug'ı `SlugExistsAsync` tarafından görülmez** (Faz 1.3'ten beri DEFERRED'da takipte): silinmiş kayıt slug'ı yeniden kullanılabilir, restore'da unique index ihlali riski; production'a yakın bir noktada gözden geçirilecek.

### Bir Sonraki Faza Aktarılan Notlar

- **Faz 3 başlangıcı**: Article modülü (Quill + Kategori + Tag + Kapak) — `SluggedEntityType.Article` 2.4b'de hazır, `ErrorCodes.Article` slot'u 2.4a'da hazır.
- **Medya yöneticisi gerek**: Service.FeaturedImage + Attorney.ProfileImageUrl image picker dahil — şu an manuel URL.
- **HtmlSanitizer whitelist'e `<img>` ekle** (Faz 3 medya entegrasyonu için).
- **Faz 3'te Article SEO meta UI tasarımı** dikkate alınmalı.

### Faz 5'e Taşınan İşler (DEFERRED)

- Translation full-replace stratejisi gözden geçirme (audit trail bozulmuyor mu)
- Pre-commit hook `AddFluentValidationAutoValidation` çağrı kontrolü (husky/shell)
- SlugHelper minimum uzunluk fallback (SEO koruma — bozuk encoding'li input)
- Sidebar aktif state Area-aware genişletme (controller adı çakışması)
- Email confirmation, AspNetCoreRateLimit, Global exception middleware
- OWASP Top 10 audit, Sitemap.xml, robots.txt
- Production seed credentials (env var), LocalDB → SQL Server, Lockout reset UI

### Test Sonuçları

- **Birim testler**: 167/167 PASSED (Business katmanı + Core common + Mapster mapping)
- **Integration testler**: 13/13 PASSED (HTTP round-trip + auth + AntiForgery)
- **PageSpeed**: Faz 6'da ölçülecek
- **Tarayıcı görsel test**: Faz 6'da ekran görüntüleriyle dokümante edilecek

---

## FAZ 1 — Proje Kurulumu & Mimari — ✅ TAMAMLANDI

**Başlama Tarihi:** 04.05.2026
**Tamamlanma Tarihi:** 05.05.2026
**Süre:** ~1 gün (planlanan 1 hafta — yoğun çalışma ile hızlandırıldı)

### Özet

| Adım | Açıklama | Commit |
| --- | --- | --- |
| 1.1 | 5 katmanlı solution iskeleti | `9b48888` |
| 1.B/1.C | Vulnerability fix + AutoMapper → Mapster geçişi | (1.1 sonrası inline) |
| 1.D | `.gitattributes` line ending normalize | `265af7a` |
| 1.2 | BaseEntity + Identity + AppDbContext + InitialIdentity migration | `da75a54` |
| 1.3.A.1 | 6 domain entity + 6 translation + AddDomainEntities migration | `7dcb096` |
| 1.3.A.2 | Generic Repository + UnitOfWork + 6 özel repository + 5 birim test | `1275a37` |
| 1.3.A.3 | 24 DTO + 6 Mapster IRegister + DI wiring + 3 mapping testi | `7406cdc` |
| 1.4 | Çok dilli (i18n) altyapısı + 3 entegrasyon testi | `fac8c07` |

### İstatistikler

- **Toplam commit (Faz 1):** 7 feature/chore commit
- **Test sayısı:** 11 PASSED (5 repository + 3 mapping + 3 localization), 0 failed
- **DB tablo sayısı:** 22 (history dahil; 21 history hariç — 7 Identity + 14 domain)
- **Build durumu:** 0 error, 0 warning
- **Kod katmanları:** 6 proje (Core, DataAccess, Business, Infrastructure, Web, Tests)

### Faz 2'ye Aktarılan Notlar

- **Service katmanı henüz yazılmadı** — Faz 2'de admin paneli ile birlikte use-case driven yazılacak (örn. `IArticleService.CreateAsync`, `IPageService.UpdateAsync`)
- **Slug otomatik üretimi** service katmanında implement edilecek (entity ve repository nötr)
- **Email confirmation, RateLimit, AntiForgery konfigürasyonu** Adım 5 (sonraki fazlarda)
- **Soft delete filter** statik HasQueryFilter pattern ile her configuration'da; `AppDbContextModelSnapshot.cs` filter'ları yazmaz (EF Core 10 davranışı), runtime'da OnModelCreating uyguluyor
- **Test environment için** `Program.cs` environment-aware DbContext check zorunlu (multi-provider conflict)
- **Admin Area culture-bağımsız** tutuldu (`/admin/...`); Faz 2'de admin sayfaları yapılırken karar gözden geçirilebilir
- **Frontend page-bazlı resource files** Faz 4'te eklenecek; şu an sadece `SharedResource.tr-TR.resx` (5 örnek key) var
- **AutoMapper yerine Mapster** lisans + güvenlik açığı sebebiyle (NU1903 GHSA-rvv3-g6hj-g44x)
- **LocalDB** development ortamı için; production deployment'ta full SQL Server'a geçiş



### Tamamlanan Adımlar

- ✅ **Adım 1 (1.A):** 5 katmanlı solution iskeleti
  - 6 proje (`Core`, `DataAccess`, `Business`, `Infrastructure`, `Web`, `Tests`)
  - N-Layer referans yapısı kuruldu (Web → DataAccess referansı YOK — CLAUDE.md kuralı)
  - `Directory.Build.props` ile merkezi proje ayarı (TargetFramework `net10.0`, Nullable, ImplicitUsings)
  - NuGet paketleri sabit sürümlerle kuruldu
  - Klasör iskelet `.gitkeep` ile korundu
  - Class library template'lerinin `Class1.cs` dosyaları silindi

- ✅ **Adım 1.B:** Güvenlik açığı paketleri yamalı sürümlere yükseltildi
  - `MailKit` 4.8.0 → **4.16.0** (NU1902 GHSA-9j88-vvj5-vhgr fix)
  - `MimeKit` 4.8.0 → **4.16.0** (NU1902 GHSA-g7hc-96xr-gvvx fix)
  - `System.Security.Cryptography.Xml` (transitive 9.0.0) → **10.0.7** explicit override (NU1903 GHSA-37gx-xxp4-5rgx + GHSA-w3x6-4m5h-cxqf fix)

- ✅ **Adım 1.C:** Mapping kütüphanesi değişikliği — AutoMapper → Mapster
  - **Sebep:** AutoMapper 13/14'te NU1903 (GHSA-rvv3-g6hj-g44x) açığı, fix ancak 15+ commercial Sponsorware sürümünde
  - `AutoMapper` paketi Business projesinden kaldırıldı
  - `Mapster` 10.0.7 + `Mapster.DependencyInjection` 10.0.7 (MIT, vuln-free) eklendi
  - CLAUDE.md teknoloji stack tablosu güncellendi

- ✅ **Adım 1.D:** Line ending normalize + push
  - `.gitattributes` eklendi (`* text=auto eol=lf` + binary/script kuralları)
  - `feature/faz-1-altyapi` branch GitHub'a push edildi

- ✅ **Adım 4:** Çok dilli (i18n) altyapısı
  - **Web:**
    - `Localization/CultureRouteConstraint` — `LanguageCodes.Supported` üzerinden geçerli culture kontrolü; geçersiz culture'da route eşleşmesi başarısız olup 404 döner
    - `Resources/SharedResource.cs` — `IStringLocalizer<SharedResource>` marker class
    - `Resources/SharedResource.tr-TR.resx` — 5 örnek key (`HomeTitle`, `ContactTitle`, `ReadMore`, `BackToHome`, `WelcomeMessage`)
  - **Program.cs:**
    - `AddLocalization(ResourcesPath = "Resources")`
    - `RequestLocalizationOptions`: default `tr-TR`, supported `[tr-TR]`, provider önceliği Route → Cookie → Accept-Language (default'lar temizlenip yeniden eklendi)
    - `ConstraintMap.Add("culture", typeof(CultureRouteConstraint))`
    - `AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)` + `AddDataAnnotationsLocalization()`
    - Middleware sırası: `UseHttpsRedirection → UseStaticFiles → UseRouting → UseRequestLocalization → UseAuthentication → UseAuthorization`
    - Kök URL `/` → `/tr-TR` 302 redirect (`MapGet("/")`)
    - Default route: `{culture:culture}/{controller=Home}/{action=Index}/{id?}`
    - Admin route: `admin/{controller=Home}/{action=Index}/{id?}` (culture-bağımsız)
    - Testing env'de `AddDbContext<AppDbContext>` atlanır (entegrasyon testi kendi SQLite provider'ını ekler — multi-provider conflict önlenir)
    - `public partial class Program { }` — `WebApplicationFactory<Program>` için
  - **HomeController:**
    - `IStringLocalizer<SharedResource> _localizer` constructor injection
    - `Index`: `ViewData["Title"] = _localizer["HomeTitle"]`, `ViewData["Welcome"] = _localizer["WelcomeMessage"]`
    - `Views/Home/Index.cshtml` — `@System.Globalization.CultureInfo.CurrentCulture.Name` ile mevcut culture'ı gösteriyor
  - **Tests:**
    - `tests/KucukMericHukuk.Tests` → `KucukMericHukuk.Web.csproj` proje referansı eklendi
    - `LocalizationIntegrationTests`: `WebApplicationFactory<Program>` + `UseEnvironment("Testing")` + SQLite in-memory
    - 3 entegrasyon testi: `RootUrl_ShouldRedirectToDefaultCulture`, `DefaultCultureUrl_ShouldReturnSuccess`, `InvalidCulture_ShouldReturn404`
  - **Test sonucu:** 11/11 PASSED (5 repo + 3 mapping + 3 localization), 0 failed, 4s
  - **Build:** 0 Uyarı / 0 Hata

- ✅ **Adım 3.A.3:** DTO'lar + Mapster IRegister'lar + DI wiring + mapping testleri
  - **Core DTOs (24 dosya):**
    - Common: `TranslationDto`, `LookupDto`, `PagedResult<T>` (önceki adımda)
    - Page: `PageDetailDto`, `PageAdminDto` + `PageTranslationDto`, `PageInputDto` + `PageTranslationInputDto`
    - Service: `ServiceListDto`, `ServiceDetailDto`, `ServiceAdminDto` + `ServiceTranslationDto`, `ServiceInputDto` + `ServiceTranslationInputDto`
    - Attorney: `AttorneyListDto`, `AttorneyDetailDto`, `AttorneyAdminDto` + `AttorneyTranslationDto`, `AttorneyInputDto` + `AttorneyTranslationInputDto`
    - Category: `CategoryListDto` (recursive SubCategories), `CategoryAdminDto` + `CategoryTranslationDto`, `CategoryInputDto` + `CategoryTranslationInputDto`
    - Tag: `TagListDto`, `TagAdminDto` + `TagTranslationDto`, `TagInputDto` + `TagTranslationInputDto`
    - Article: `ArticleListDto`, `ArticleDetailDto`, `ArticleAdminDto` + `ArticleTranslationDto`, `ArticleInputDto` + `ArticleTranslationInputDto`
    - Frontend DTO'lar **flatten** (tek dilli, Translation listesi yok); Admin DTO'lar `List<TranslationDto>` içerir; Input DTO `Id` nullable (null=create, dolu=update)
  - **Business Mappings (6 IRegister):**
    - `PageMappingConfig`, `ServiceMappingConfig`, `AttorneyMappingConfig`, `CategoryMappingConfig`, `TagMappingConfig`, `ArticleMappingConfig`
    - Frontend mapping pattern: `src.Translations.Select(t => t.X).FirstOrDefault()` (repository sorgusu zaten dil filtreliyor)
    - Admin mapping: nested entity'ler (`Author`, `Category`, `Services`, `Tags`) için özel `Map(...)` ifadeleri; `LookupDto` collection'lar manuel
    - Input → Entity: `BaseEntity` audit alanları (`CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`) ve M2M nav'ları `Ignore`
    - Category recursive mapping: `PreserveReference(true)` ile sonsuz döngü engelleniyor
  - **Business DI:**
    - `Business/DependencyInjection.cs` — `AddBusiness()`: `TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly())` + `AddSingleton(config)` + `AddScoped<IMapper, ServiceMapper>()`
  - **Web:**
    - `Program.cs` — `builder.Services.AddBusiness()` AddDataAccess sonrası
  - **Tests:**
    - 3 mapping unit testi (`MappingTests.cs`): `Service_To_ServiceListDto_ShouldFlattenTranslation`, `Article_To_ArticleListDto_ShouldFlattenTranslationAndAuthor`, `Article_To_ArticleAdminDto_ShouldIncludeAllTranslations`
    - Test mapper: `new Mapper(config)` — Mapster 10.0.7 `ServiceMapper` her zaman `IServiceProvider` istiyor; `Mapper` basit alternatif (DI gerektirmez)
  - **Test sonucu:** 8/8 PASSED (5 repo + 3 mapping), 0 failed, 8s
  - **Build:** 0 Uyarı / 0 Hata

- ✅ **Adım 3.A.2:** Generic Repository + UnitOfWork + 6 özel repository + birim testler
  - **Core:**
    - `PagedResult<T>` (Items, TotalCount, PageNumber, PageSize, TotalPages, HasPrevious, HasNext)
    - `IGenericRepository<T> where T : BaseEntity` — read/write/delete (soft+hard)/restore + `Query()`/`QueryWithDeleted()` escape hatchleri
    - 6 özel repository interface (`IPageRepository`, `IServiceRepository`, `IAttorneyRepository`, `ICategoryRepository`, `ITagRepository`, `IArticleRepository`)
    - `IUnitOfWork` (6 repo property + SaveChangesAsync + transaction methodları + IDisposable + IAsyncDisposable)
  - **DataAccess:**
    - `QueryableExtensions.ToPagedListAsync<T>` (Skip/Take + total count)
    - `GenericRepository<T>` — soft delete `IsDeleted=true + DeletedAt=now`, `HardDelete` `Remove`, `Restore` `IsDeleted=false + DeletedAt=null`
    - 6 özel repository (Page, Service, Attorney, Category, Tag, Article) — translation-aware include'lar (`Include(Translations.Where(t => t.LanguageCode == languageCode))`)
    - `Article.IncrementViewCountAsync`: `ExecuteUpdateAsync` ile transactional increment (tracking yok)
    - `UnitOfWork`: lazy-init repo property'ler, `BeginTransactionAsync/CommitTransactionAsync/RollbackTransactionAsync`
    - `DependencyInjection.AddDataAccess()`: open generic + 6 özel + UoW kayıt (Scoped lifetime)
  - **Web:**
    - `Program.cs` — `builder.Services.AddDataAccess()` Identity'den sonra çağrılıyor
  - **Tests:**
    - SQLite in-memory provider tercih edildi (EF InMemory query filter desteği yetersiz)
    - `TestDbContextFactory` — `Filename=:memory:` connection, `EnsureCreated()` ile schema kurulur
    - 5 birim testi: `AddAsync`, soft delete (filter çalışıyor), hard delete, restore, pagination
  - **Test sonucu:** 5/5 PASSED, 0 failed, 6s
  - **Build:** 0 Uyarı / 0 Hata

- ✅ **Adım 3.A.1:** Domain entity'leri (Page, Service, Attorney, Article, Category, Tag) + translation tabloları + AddDomainEntities migration uygulandı
  - **Core:**
    - `ArticleStatus` enum (`Draft=0`, `Published=1`, `Archived=2`)
    - `BaseTranslation` abstract class (`Id`, `LanguageCode`, `CreatedAt`, `UpdatedAt`) — `ITranslation` implementasyonu
    - 6 domain entity: `Page`, `Service`, `Attorney`, `Category`, `Tag`, `Article` (hepsi `BaseEntity` + `ITranslatable<T>`)
    - 6 translation entity: `PageTranslation`, `ServiceTranslation`, `AttorneyTranslation`, `CategoryTranslation`, `TagTranslation`, `ArticleTranslation`
    - `Attorney.UserId → ApplicationUser`, `Article.AuthorId → ApplicationUser` (nullable, SetNull)
    - `Category.ParentCategoryId` self-FK (Restrict)
    - Many-to-many: `Service ↔ Attorney` (join: `AttorneyServices`), `Article ↔ Tag` (join: `ArticleTags`)
  - **DataAccess:**
    - 12 EntityTypeConfiguration (entity + translation eşleri)
    - `ToTable()` plural isimlendirme (CLAUDE.md kuralı): `Pages`, `Services`, `Attorneys`, `Categories`, `Tags`, `Articles` + translation eşleri
    - Translation tabloları: `(LanguageCode + Slug)` ve `(LanguageCode + ParentEntityId)` composite UNIQUE index
    - `BaseEntity` türevleri: `IsDeleted` index
    - `Articles`: `PublishedAt`, `Status`, `IsFeatured` index
    - `Pages.PageKey`: unique index
    - Translation cascade: parent silinince translation'lar silinir
    - `Category.SubCategories` ve `Articles → Category`: Restrict / SetNull (cycle önlenir)
    - `Author/User → SetNull`: kullanıcı silinince makale/avukat orphan kalır
  - **AppDbContext sadeleştirme:**
    - Adım 2'de yazılan reflection-based dynamic `Expression.Lambda` soft delete filter loop'u **kaldırıldı**
    - `OnModelCreating` artık sadece Identity tablo isimlendirmelerini ve `ApplyConfigurationsFromAssembly` çağrısını içeriyor — soft delete filter'ları her configuration'da statik tanımlı
  - **Soft delete filter — statik HasQueryFilter (snapshot serileştirme sorunu düzeltildi):**
    - Her domain entity configuration'ına `builder.HasQueryFilter(x => !x.IsDeleted)` (6 tane: Page, Service, Attorney, Category, Tag, Article)
    - Her translation configuration'ına `builder.HasQueryFilter(t => !t.Parent.IsDeleted)` (6 tane: parent soft-delete edilince translation da otomatik gizlenir — required navigation tutarlılığı)
    - Avantaj: dinamik lambda (Expression.Lambda) yerine statik typed lambda → EF Core 10 migration tool model karşılaştırmasında tutarlı, `PendingModelChangesWarning` tetiklenmiyor
    - Not: HasQueryFilter satırları hâlâ `AppDbContextModelSnapshot.cs` içinde **görünmüyor** (EF Core 10 query filter'ları snapshot'a yazmıyor — bilinen davranış); ancak statik lambda ile model karşılaştırma sorunsuz çalışıyor
    - `Program.cs` orijinal sade haline döndü — `ConfigureWarnings(...)` baskılaması **kaldırıldı**
  - **Migration:** `20260504211535_AddDomainEntities` — 14 yeni tablo (6 entity + 6 translation + 2 join), DB'de toplam 22 tablo (history dahil; 21 history hariç). Identity migration korundu, sadece domain migration yenilendi.
  - **Build:** 0 Uyarı / 0 Hata. EF migrations add/update sırasında uyarı yok.

- ✅ **Adım 2:** BaseEntity + Identity entegrasyonu + AppDbContext + InitialIdentity migration
  - **Core:**
    - `BaseEntity` (audit kolonları + soft delete: `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`)
    - `ITranslatable<T>` + `ITranslation` interface'leri (çok dilli altyapı)
    - `LanguageCodes` constants (`tr-TR`, `en-US`, `de-DE`; şu an sadece `tr-TR` Supported)
    - `ApplicationUser : IdentityUser<int>` (FullName + audit + soft delete)
    - `ApplicationRole : IdentityRole<int>` (Description + audit)
    - `Microsoft.Extensions.Identity.Stores 10.0.0` paketi (EF bağımlılığı olmadan Identity model sınıfları)
  - **DataAccess:**
    - `AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>`
    - Identity tablo isimleri sadeleştirildi (Users, Roles, UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims — `AspNet` prefix kaldırıldı)
    - Soft delete global query filter `BaseEntity` türevleri için reflection ile uygulanıyor (şu an domain entity yok, altyapı hazır)
    - `SaveChangesAsync` override: `UpdatedAt` audit alanı otomatik dolduruluyor (`BaseEntity`, `ApplicationUser`, `ApplicationRole` için)
    - `ApplicationUserConfiguration` (FullName max 150, IsDeleted index)
    - `ApplicationRoleConfiguration` (Description max 250)
    - `ApplyConfigurationsFromAssembly` ile tüm `IEntityTypeConfiguration<T>` otomatik yüklenir
  - **Web:**
    - `Program.cs`: `AddDbContext<AppDbContext>(UseSqlServer)`, `AddIdentity<...>` + `AddEntityFrameworkStores<AppDbContext>` + `AddDefaultTokenProviders`
    - Identity ayarları: password ≥8, lockout 5 başarısızlık → 15 dk, email unique, email confirmation şu an kapalı (Adım 5'te açılacak)
    - `app.UseAuthentication()` + `UseAuthorization()` pipeline'a eklendi
    - .NET 10 `MapStaticAssets` / `WithStaticAssets` korundu
  - **Connection string:**
    - `appsettings.json`'da placeholder (`""`)
    - User Secrets'ta gerçek değer: `Server=(localdb)\MSSQLLocalDB;Database=KucukMericHukukDb;Trusted_Connection=True;...`
    - User Secrets ID: `ec9bfee5-0aa3-4c77-8b06-22aa85b9fe43`
  - **Migration:** `20260504112905_InitialIdentity` — 7 Identity tablosu DB'ye uygulandı (`KucukMericHukukDb` LocalDB'de oluştu)

### Build Durumu

- `dotnet build`: **0 Uyarı / 0 Hata**
- Migration: `InitialIdentity` uygulandı, 8 tablo (`__EFMigrationsHistory` + 7 Identity) oluştu.

### Bilinen Sorunlar / Geçici Çözümler

- Yok

### Bir Sonraki Adıma Aktarılan Notlar

- Mapster için DI kaydı (`services.AddSingleton<TypeAdapterConfig>` + `services.AddScoped<IMapper, ServiceMapper>`) Business `DependencyInjection.cs` içinde yapılacak (Adım 3+).
- Domain entity'ler (Article, Attorney, vb.) eklendiğinde otomatik soft delete query filter onlara da uygulanacak (zaten `OnModelCreating` reflection ile).
- Layer-modül DI registration sınıfları (`Web/DependencyInjection.cs`, `Business/DependencyInjection.cs`, `DataAccess/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs`) Adım 3'te oluşturulacak — şu an `Program.cs` doğrudan kayıt yapıyor.
- Email confirmation, RateLimit, AntiForgery konfigürasyonu Adım 5'te.

---

## FAZ 0 — Hazırlık — 🔄 DEVAM EDİYOR

**Başlama Tarihi:** [Tarih girilecek]
**Hedef Bitiş:** [Tarih girilecek]

### Karar Verilenler

- [x] Renk paleti: Seçenek A — Koyu orman yeşili (#0F2A23) + Şampanya altın (#C9A961) + Krem (#FAF7F2)
- [x] .NET sürümü: .NET 10 (STS)
- [x] Domain uzantısı: .av.tr
- [x] Çok dilli destek: Şu an Türkçe, altyapı çok dile hazır
- [ ] Hosting tercihi: [Yurt içi / Yurt dışı + sağlayıcı]
- [ ] Randevu sistemi: [Form tabanlı / Takvim entegrasyonlu]
- [ ] CDN kullanımı: [Cloudflare / Yok]

### Müşteriden Beklenen İçerikler

Detay için `docs/Musteri-Icerik-Toplama-Formu.docx` dosyasına bakınız.

- [ ] Logo (vektör tercihi)
- [ ] Avukat bilgileri ve fotoğrafları
- [ ] Ofis fotoğrafları
- [ ] Hizmet alanı açıklamaları
- [ ] İletişim bilgileri ve harita konumu
- [ ] Sosyal medya hesapları
- [ ] (Varsa) hazır makaleler
- [ ] KVKK aydınlatma metni taslağı

### Teknik Hazırlık

- [ ] GitHub repository oluşturulacak
- [ ] Sunucu / hosting kararı verilecek
- [ ] SSL sertifikası planlanacak
- [ ] Domain transferi/kaydı (.av.tr için baro levha kaydı gerekir)

### Bir Sonraki Faza Aktarılan Notlar

- TBB reklam yasağı kuralları gözetilerek "Referanslar" sayfası içeriği hazırlanmalı.
- KVKK için iletişim formunda aydınlatma onayı zorunlu olacak; tasarımda yer ayrılmalı.

---

## ŞABLON: FAZ X — [Faz Adı] — [DURUM]

**Tamamlanma Tarihi:** GG.AA.YYYY

### Yapılanlar

- [Tamamlanan iş 1]
- [Tamamlanan iş 2]

### Bilinen Sorunlar / Geçici Çözümler

- [Sorun ve geçici çözüm]

### Bir Sonraki Faza Aktarılan Notlar

- [Dikkat edilmesi gereken nokta]
- [İyileştirme önerisi]

### Test Sonuçları

- PageSpeed (Mobil): XX / 100
- PageSpeed (Masaüstü): XX / 100
- Kritik Hatalar: Yok / [Açıklama]
- Tarayıcı uyumluluğu: Chrome ✅ / Firefox ✅ / Safari ✅ / Edge ✅
