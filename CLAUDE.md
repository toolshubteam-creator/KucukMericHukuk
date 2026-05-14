# CLAUDE.md — Küçükmeriç Hukuk Bürosu Web Projesi

> Bu dosya Claude Code tarafından her oturum başında okunur. Proje boyunca tutarlı geliştirme yapabilmek için kodlama standartları, mimari kararlar ve faz durumu burada tutulur. Karar değişikliklerinde bu dosya güncellenmelidir.

---

## 1. Proje Tanımı

**Müşteri:** Küçükmeriç Hukuk Bürosu (Serdivan / Sakarya)
**Tip:** Kurumsal web sitesi + İçerik Yönetim Sistemi (CMS)
**Hedef:** SEO-odaklı, tam yönetilebilir, modern hukuk bürosu websitesi
**Domain:** kucukmerichukuk.av.tr (Türkiye Barolar Birliği uyumu için .av.tr seçildi)

---

## 2. Teknoloji Stack

| Katman | Teknoloji | Sürüm |
| --- | --- | --- |
| Backend | ASP.NET MVC Core | .NET 10 (STS) |
| ORM | Entity Framework Core | 10 |
| Veritabanı | Microsoft SQL Server | 2022+ |
| Kimlik Doğrulama | ASP.NET Identity Core | 10 |
| Loglama | Serilog | son sürüm |
| Mapping | Mapster (+ Mapster.DependencyInjection) | 10.0.7 |
| Validasyon | FluentValidation | son sürüm |
| WYSIWYG Editör | Quill | 2.x |
| CSS Framework | Bootstrap 5 + custom | 5.3+ |
| İkonlar | Lucide Icons / Phosphor Icons | son sürüm |

**Not:** .NET 10 STS sürümüdür, Kasım 2026'ya kadar destek vardır. .NET 11 LTS yayınlanınca geçiş planlanacak.

---

## 3. Klasör Yapısı

```
KucukMericHukuk/
├── src/
│   ├── KucukMericHukuk.Web              → MVC Presentation
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── wwwroot/
│   │   └── Areas/Admin/                 → Yönetim paneli
│   ├── KucukMericHukuk.Business         → İş mantığı + servisler
│   ├── KucukMericHukuk.DataAccess       → EF Core, Repository, UoW
│   ├── KucukMericHukuk.Core             → Entity, DTO, Interface, Enum
│   └── KucukMericHukuk.Infrastructure   → E-posta, dosya, cache, sitemap
├── tests/
│   └── KucukMericHukuk.Tests
├── docs/                                → Dökümanlar
├── CLAUDE.md                            → Bu dosya
├── PROGRESS.md                          → Faz ilerleme kayıtları
└── README.md
```

---

## 4. Mimari Prensipler

### Katmanlı Mimari (N-Layer)

- **Web** sadece Controller + ViewModel + View içerir, doğrudan DbContext'e erişmez.
- **Business** servisleri arayüz üzerinden tanımlanır: `IArticleService` → `ArticleService`.
- **DataAccess** Repository pattern kullanır: `IGenericRepository<T>`, `IUnitOfWork`.
- **Core** entity ve DTO'ları tutar, başka katmana referans vermez (en alt katman).
- **Infrastructure** dış servisleri sarmalar (E-posta, Cache, Sitemap üretici).

### Bağımlılık Yönü

```
Web → Business → DataAccess → Core
                ↓
         Infrastructure → Core
```

`Core` hiçbir şeye bağımlı değildir. `Web` `DataAccess`'e doğrudan referans VERMEZ.

### Dependency Injection

Tüm servisler `Program.cs`'te kayıtlı modüller üzerinden register edilir:
- `Web/DependencyInjection.cs`
- `Business/DependencyInjection.cs` — `AddBusiness()` (Mapster scan + service'ler)
- `DataAccess/DependencyInjection.cs` — `AddDataAccess()` (repositories + UoW)
- `Infrastructure/DependencyInjection.cs`

### Repository Pattern

Generic Repository + Özel Repository hybrid yapısı:
- `IGenericRepository<T> where T : BaseEntity` (Core/Interfaces): Standart CRUD + Pagination + Soft/Hard Delete + Restore
- `I<Entity>Repository : IGenericRepository<T>` (Core/Interfaces/Repositories): Entity-specific sorgular (örn. `IArticleRepository.GetPublishedPagedAsync`)
- Implementations: `DataAccess/Repositories/`
- `IUnitOfWork` (Core/Interfaces): 6 özel repository property + `SaveChangesAsync` + transaction methodları
- DI: open generic + concrete repos + UoW, hepsi `AddScoped`

**IQueryable kuralı:** `Query()` ve `QueryWithDeleted()` metodları `IQueryable<T>` döner. Bunlar SADECE DataAccess katmanı içinde özel repository sorgularında kullanılır. Service katmanına IQueryable LEAK ETMEZ — service sadece materialized koleksiyonlar (`IReadOnlyList<T>`, `PagedResult<T>`, single entity) görür.

**Soft delete:** `Delete(entity)` = `IsDeleted=true + DeletedAt=now` (default davranış). Fiziksel silme için `HardDelete(entity)`. Geri getirmek için `Restore(entity)`.

---

## 5. Kodlama Standartları

### İsimlendirme

| Tür | Kural | Örnek |
| --- | --- | --- |
| Class | PascalCase | `ArticleService` |
| Interface | I + PascalCase | `IArticleService` |
| Method | PascalCase | `GetByIdAsync` |
| Private field | _camelCase | `_dbContext` |
| Parametre, lokal değişken | camelCase | `articleId` |
| Constant | PascalCase | `MaxPageSize` |
| Async method | Async son eki | `GetAllAsync` |
| DTO | PascalCase + Dto | `ArticleListDto` |
| ViewModel | PascalCase + ViewModel | `ArticleEditViewModel` |
| Entity | PascalCase (tekil) | `Article`, `Attorney` |
| DbSet | PascalCase (çoğul) | `Articles`, `Attorneys` |

### Async/Await Kullanımı

- Tüm DB ve I/O işlemleri **async** olmalı.
- `Task<T>` dönen metodlarda `Async` son eki kullan.
- `.Result` veya `.Wait()` **kesinlikle yok**.
- `CancellationToken` parametre olarak alınmalı.

### Null Safety

- Nullable reference types **aktif** (`<Nullable>enable</Nullable>`).
- DTO ve ViewModellerde nullable tipler açıkça belirtilir: `string?`, `int?`.

### Exception Handling

- Servis katmanında özel exception sınıfları: `NotFoundException`, `ValidationException`, `UnauthorizedException`.
- Web katmanında global exception middleware (`ExceptionHandlingMiddleware`).
- Kullanıcıya gösterilen mesaj ile log mesajı **ayrı** olmalı.

### Service Katmanı Sözleşmesi

- Tüm service metotları `Task<Result>` veya `Task<Result<T>>` döner (`Core/Common/Result.cs`).
- Service içinde input validation: FluentValidation `IValidator<TInput>` enjeksiyonu, `await validator.ValidateAsync(input)`.
- Validation hatası: `validationResult.ToFailureResult<T>()` ile `Result.Failure`'a çevrilir (`Business/Common/ValidationResultExtensions.cs`).
- İş kuralı hatası: `Result.Failure(new Error(ErrorCodes.X.Y, "Mesaj", field))`. Field opsiyonel — varsa ilgili form alanına bağlanır, yoksa validation summary'ye düşer.
- Error code'ları `Core/Common/ErrorCodes.cs` altında entity başına grup halinde tutulur. Yeni hata türü eklenirken bu sınıfa eklenir.
- Repository'den `NotFoundException` yakalanır → `Result.Failure(ErrorCodes.X.NotFound)`.
- Beklenmedik exception (DB down, network) yakalanmaz, global middleware'e (Faz 5'te) bırakılır.
- **FluentValidation auto-MVC YOK.** `AddFluentValidationAutoValidation` çağrılmaz; validation `Action` katmanında değil **service** katmanında manuel yapılır. `AddBusiness()` sadece `AddValidatorsFromAssembly` çağırır.
- Controller pattern:

  ```csharp
  var result = await _service.CreateAsync(input);
  if (result.IsFailure)
  {
      ModelState.AddErrors(result);     // Web/Extensions/ModelStateExtensions.cs
      return View(input);
  }
  TempData["Success"] = "Kayıt oluşturuldu.";
  return RedirectToAction(nameof(Index));
  ```

### Custom Exception Kullanımı

- `NotFoundException` (`Core/Exceptions/`): Repository/DB seviyesinde "olmazsa olmaz" kayıt bulunamadığında fırlatılır.
- `BusinessException` (`Core/Exceptions/`): Bir service'ten başka bir service'e iş kuralı sinyali (örn. `Article` create sırasında `CategoryService.EnsureExistsAsync` bunu fırlatır).
- **Controller bu exception'ları görmez** — service yakalar, `Result.Failure` döner.
- `Result<T>.Value`, `IsFailure` durumunda erişilirse `InvalidOperationException` fırlatır — programcı hatasıdır, mutlaka `IsSuccess`/`IsFailure` ile koru.

### Mapster Kullanımı

- Entity başına 1 IRegister sınıfı: `Business/Mappings/<Entity>MappingConfig.cs`
- Tüm yönler (Read DTO + Write DTO) tek dosyada
- Frontend DTO'lar **translation flatten**: `src.Translations.Select(t => t.X).FirstOrDefault()` — repository sorgusu zaten dil filtreliyor
- Admin DTO'lar `List<TranslationDto>` içerir — birden fazla dil aynı anda yönetilir
- Recursive entity'lerde (örn. `Category.SubCategories`): `PreserveReference(true)` zorunlu
- Input → Entity mapping: `BaseEntity` audit alanları (`CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`) ve M:N nav property'leri `Ignore`
- DI: `AddBusiness()` extension `Assembly.Scan` ile `IRegister`'ları otomatik bulur

### Çok Dilli Altyapı (i18n)

- Tüm metin içerikli entity'ler `LanguageCode` kolonu içerir (örn. `tr-TR`, `de-DE`)
- Default culture: `tr-TR`, supported: `LanguageCodes.Supported`
- URL pattern: `/{culture:culture}/{controller}/{action}` — culture **ZORUNLU** (geçersiz culture'da `CultureRouteConstraint` 404 verir)
- Admin Area culture-bağımsız: `/admin/...`
- Kök URL `/` → `/tr-TR` 302 redirect (`MapGet("/")`)
- Culture algılama önceliği: Route → Cookie → Accept-Language → Default
- Resource files: `Web/Resources/SharedResource.<culture>.resx` (örn. `SharedResource.tr-TR.resx`)
- Razor view'larda: `@inject IViewLocalizer Localizer` veya `@inject IStringLocalizer<SharedResource> Localizer`
- Middleware sırası kritik: `UseRouting → UseRequestLocalization → UseAuthentication → UseAuthorization`
- Şu an yalnızca Türkçe içerik üretilecek; altyapı çok dile hazır

---

## 6. Git İş Akışı

### Branch Stratejisi

- `main` — Yayın (production). Doğrudan commit YOK.
- `develop` — Ana geliştirme branch'i.
- `feature/faz-X-isim` — Özellik branch'leri (örn. `feature/faz-2-medya-galerisi`).
- `hotfix/aciklama` — Acil prod düzeltmeleri.

### Commit Mesaj Formatı

[Conventional Commits](https://www.conventionalcommits.org/) formatı:

```
<tip>: <kısa açıklama>

[opsiyonel detay paragrafı]

[opsiyonel footer: ilgili issue]
```

**Tipler:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `perf`

**Örnekler:**
- `feat: makale yönetimi modülü eklendi`
- `fix: 404 log middleware'inde null reference hatası düzeltildi`
- `refactor: ArticleService dependency injection sadeleştirildi`
- `docs: PROGRESS.md faz 2 güncellendi`

### Pull Request

- Her feature branch develop'a PR ile birleştirilir.
- PR açıklaması: ne yapıldı, nasıl test edildi, ekran görüntüsü (UI değişikliği varsa).
- Self-review zorunlu — merge öncesi kendi kodunu gözden geçir.

---

## 7. Veritabanı

### Bağlantı Stringi

`appsettings.json` içinde **YOK**. `appsettings.Development.json` (gitignore'da) veya environment variable:

```
ConnectionStrings__DefaultConnection
```

### Migration Komutları

```bash
# Yeni migration ekle
dotnet ef migrations add MigrationName --project src/KucukMericHukuk.DataAccess --startup-project src/KucukMericHukuk.Web

# Veritabanına uygula
dotnet ef database update --project src/KucukMericHukuk.DataAccess --startup-project src/KucukMericHukuk.Web
```

### Naming

- Tablo adları: PascalCase, çoğul (`Articles`, `Attorneys`).
- Kolon adları: PascalCase (`Title`, `CreatedAt`).
- Primary key: `Id` (int veya Guid — proje boyunca tutarlı: **int** seçildi).
- Foreign key: `<Entity>Id` (örn. `CategoryId`).
- Tarih kolonları: `CreatedAt`, `UpdatedAt`, `DeletedAt` (soft delete).

### Soft Delete Davranışı

Tüm `BaseEntity` türevleri (Page, Service, Attorney, Article, Category, Tag) `IsDeleted` (bool) ve `DeletedAt` (DateTime?) içerir. Soft delete query filter ile silinmiş kayıtlar EF sorgularında otomatik gizlenir.

- Filter her entity'nin `EntityTypeConfiguration` sınıfında **statik typed lambda** olarak tanımlıdır:
  `builder.HasQueryFilter(x => !x.IsDeleted);`
- `AppDbContext.OnModelCreating` içinde dinamik reflection ile filter eklenmez — bu pattern EF Core 10'da snapshot tutarsızlığına yol açıyor (PendingModelChangesWarning).

**Translation tablolarında cascade soft-delete:**

Translation tabloları parent entity'nin `IsDeleted` durumuna göre filtrelenir:
- `ArticleTranslation` → `!t.Article.IsDeleted`
- `PageTranslation` → `!t.Page.IsDeleted`
- (`Service`, `Attorney`, `Category`, `Tag` translation'ları aynı pattern)

Bu, soft-deleted bir parent'ın translation'larının da otomatik gizlenmesini ve required navigation'ın bozulmamasını sağlar. Restore edildiğinde translation'lar otomatik tekrar görünür.

**Filter bypass:**

Admin panelinde "silinmiş kayıtları göster" senaryolarında `query.IgnoreQueryFilters()` kullanılır. Hem ana entity hem translation sorgularında bypass etmek gerekir.

### Cascade Davranışı (FK OnDelete)

| İlişki | Davranış | Sebep |
| --- | --- | --- |
| `Attorney.UserId → ApplicationUser` | `SetNull` | Kullanıcı silinince attorney kaydı korunur |
| `Article.AuthorId → ApplicationUser` | `SetNull` | Yazar silinince makale korunur |
| `Article.EditorId → ApplicationUser` | `Restrict` | İki FK aynı tabloya SetNull olamaz (SQL Server multi-cascade); ApplicationUser soft-delete olduğu için pratikte tetiklenmez |
| `Article.CategoryId → Category` | `SetNull` | Kategori silinince makaleler orphan kalır (manuel taşıma) |
| `Category.ParentCategoryId → Category` | `Restrict` | Alt kategorisi olan kategori silinemez |
| Translation → Parent (Page/Service/Attorney/Category/Tag/Article) | `Cascade` | Parent silinince translation'lar SQL düzeyinde de silinir (hard delete) |
| `AttorneyServices` (M:N) | `Cascade` | Attorney veya Service silinince join kaydı silinir |
| `ArticleTags` (M:N) | `Cascade` | Article veya Tag silinince join kaydı silinir |

### Test Ortamı (Integration Tests)

Integration testlerde production'da kullanılan SQL Server yerine **SQLite in-memory** kullanılır (gerçek SQL davranışı simüle eder, soft delete query filter'ları doğru çalışır).

1. **Program.cs'de environment-aware DbContext registration:**
   ```csharp
   if (!builder.Environment.IsEnvironment("Testing"))
   {
       builder.Services.AddDbContext<AppDbContext>(options =>
           options.UseSqlServer(...));
   }
   ```
   Test fixture `UseEnvironment("Testing")` çağırarak production DbContext kaydını atlatır. Bu environment check'i **SİLMEYİN** — testler çoklu provider hatası verir (`Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Sqlite` çakışması).

2. **Test fixture'da SQLite registration:**
   ```csharp
   builder.UseEnvironment("Testing");
   builder.ConfigureServices(services =>
   {
       var connection = new SqliteConnection("Filename=:memory:");
       connection.Open();
       services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
       // ... EnsureCreated()
   });
   ```

3. **Unit testlerde `TestDbContextFactory`:** `tests/.../Infrastructure/TestDbContextFactory.cs` — repository testlerinde standalone SQLite context.

4. `Program.cs` sonunda `public partial class Program { }` zorunlu — `WebApplicationFactory<Program>` generic'i için.

---

## 8. Güvenlik Kuralları

- **Asla hard-coded** API key, şifre, connection string commit edilmez.
- Kullanıcı girdileri **HtmlSanitizer** ile temizlenir (Quill çıktısı dahil).
- Tüm formlarda **AntiForgery token** zorunlu.
- Admin paneli HTTPS üzerinden, Cookie Authentication kullanır.
- Şifreler **BCrypt** veya Identity'nin default hasher'ı ile saklanır.
- Login'de **brute-force koruması**: AspNetCoreRateLimit + Identity LockoutOnFailure.
- File upload'da: MIME type doğrulama + uzantı whitelist + max 10MB.

---

## 9. Tasarım Token'ları

Renk paleti — Seçenek A (Modern Prestij):

```css
:root {
  --color-primary: #0F2A23;        /* Koyu orman yeşili */
  --color-primary-dark: #081814;
  --color-primary-light: #1F4438;

  --color-accent: #C9A961;          /* Şampanya altın */
  --color-accent-dark: #A88A45;
  --color-accent-light: #D9BD7C;

  --color-bg: #FAF7F2;              /* Krem zemin */
  --color-bg-alt: #FFFFFF;
  --color-text: #2C2C2A;
  --color-text-muted: #6B6B6B;
  --color-border: #E5E0D5;
}
```

Tipografi:
- Başlıklar: **Playfair Display** (Google Fonts, serif, 400/500/700)
- Gövde: **Inter** (Google Fonts, sans-serif, 400/500/600)

Ölçü sistemi: 8px tabanlı (`8, 16, 24, 32, 48, 64`).

Border radius: `4px` (küçük), `8px` (kart), `16px` (büyük blok).

---

## 10. Test Gereksinimleri

- **Unit test:** Business katmanı servisleri için xUnit + Moq (`tests/KucukMericHukuk.Tests`).
- **Integration test:** HTTP endpoint'leri için `WebApplicationFactory<Program>` + SQLite in-memory (`tests/KucukMericHukuk.IntegrationTests`).
- **Coverage hedefi:** Business katmanı en az %70.
- Her PR'da test eklenmesi beklenir.

### Integration Test HTML Assertion'ları

Razor view'lar HTML çıktıyı encoded üretir. Türkçe karakterler HTML entity formuna çevrilir:

| Karakter | HTML entity |
| --- | --- |
| `ı` | `&#x131;` |
| `ş` | `&#x15F;` |
| `ğ` | `&#x11F;` |
| `ü` | `&#xFC;` |
| `ö` | `&#xF6;` |
| `ç` | `&#xE7;` |
| `İ` | `&#x130;` |

Bu nedenle integration test'lerde HTML body assertion'ları yazarken **mesajdan ASCII-only bir alt-string seç** ve onu kontrol et:

- ❌ `html.Should().Contain("hatalı")`        — `ı` HTML-encoded olduğu için yakalanmaz
- ✅ `html.Should().Contain("E-posta veya")`   — ASCII-only substring
- ✅ `html.Should().Contain("zorunludur")`     — `z/o/r/u/n/l/u/d/u/r` ASCII

### Geliştirme Komutu — HTTPS Profili

`dotnet run` her zaman `https` profili ile çalıştırılır:

```bash
dotnet run --project src/KucukMericHukuk.Web --launch-profile https
```

Sebep: Cookie auth `SecurePolicy = Always` (Faz 2.2 güvenlik kararı). HTTP profili (default) auth cookie'yi engeller — login redirect döngüleri ve smoke test'lerde 302/401 hatalarına yol açar.

Integration test'lerde `IntegrationTestFactory` `SecurePolicy.SameAsRequest`'e indirger — production kodu DOKUNULMAZ.

---

## 11. Faz Durumu

**Mevcut Faz:** Faz 6 — Test, Düzeltme, Yayına Hazırlık (🔄 devam ediyor). 6.1–6.17 tamamlandı, 6.18 belge senkronizasyonu.

- ✅ **Faz 1 — Proje Kurulumu & Mimari** tamamlandı (05.05.2026): 5 katmanlı solution, BaseEntity + Identity, 6 domain entity + translation, Generic Repository + UoW + 6 özel repository, 24 DTO + 6 Mapster mapping config, çok dilli altyapı, 11 test PASSED
- ✅ **Faz 2 — Yönetim Paneli (Admin CMS)** tamamlandı (06.05.2026, tag `v0.2.0`): 5 modül full CRUD (Page/Tag/Service/Category/Attorney), cookie auth + 3 rol seed, FluentValidation server+client side, SweetAlert2 + Quill + Tabler 1.4.0, integration test altyapısı, **180/180 test PASSED** (167 birim + 13 integration), 29 commit
- ✅ **Faz 3 — İçerik Yönetimi: Medya + Article** tamamlandı (07.05.2026): medya altyapısı (entity + LocalFileStorage + SkiaSharp) + admin galeri/upload/picker, Article tam CRUD + frontend, 226/226 test PASSED (203 birim + 23 integration)
- ✅ **Faz 4 — Frontend (Tasarım & Geliştirme)** tamamlandı (09.05.2026, tag `v0.4.0`): 17 public sayfa + responsive + SEO temelleri + KVKK uyumlu iletişim formu + custom 404, Faq + ContactMessage entity, 231/231 test PASSED (208 birim + 23 integration)
- ✅ **Faz 5 — SEO, Güvenlik, Admin Geri Dönüşleri** tamamlandı (11.05.2026, tag `v0.5.0`): meta tag + JSON-LD + sitemap + breadcrumb, CDN SRI + Turnstile + RateLimit + global exception middleware + security headers, Faq admin CRUD + ContactMessages liste UI, 275/275 test PASSED (246 birim + 29 integration)
- 🔄 **Faz 6 — Test, Düzeltme, Yayına Hazırlık** devam ediyor: 6.1–6.17 tamamlandı (SiteSettings, Testimonial, Galeri, kullanıcı yönetimi, Editor/Author authz, production-readiness, pre-launch audit, CDN self-host), 438/438 test PASSED; 6.18 belge senkronizasyonu

Faz tamamlama detayları için `PROGRESS.md` dosyasına bakınız.

---

## 12. Önemli Hatırlatmalar (Claude Code için)

1. **CLAUDE.md ve PROGRESS.md değişiklik gerektirir mi?** — Yeni mimari karar veya bağımlılık eklendiğinde güncelle.
2. **Kod yazmadan önce klasör yapısını kontrol et** — Yanlış katmana kod koyma.
3. **EF Core sorgu yazarken `.Include()` ve `.AsNoTracking()` kullanımına dikkat** — Performans için.
4. **Frontend'de inline JS minimuma** — Tüm JS `wwwroot/js/` altında modüler dosyalarda.
5. **Türkiye Barolar Birliği reklam yasağı** — "Müvekkil yorumları" sayfasında müvekkil ismi/davası ifşa edilmez. "Başarılarımız" yerine "Çalışma alanları" vurgusu yapılır.
6. **KVKK uyumu** — İletişim formu submit'inde aydınlatma onayı checkbox'ı zorunlu, onay tarih-saat ile loglanır.
7. **Çok dilli altyapı şimdi kurulacak** — Sonradan eklemek pahalı olur.
8. **DEFERRED.md kontrolü** — Her adım başında DEFERRED.md oku, o adımda kapatılabilecek erteleme var mı raporla. Adım sonunda DEFERRED.md güncellenir (kapatılanlar silinir, yeniler eklenir). Detay WORKING_STYLE.md madde 11'de.
