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
- `Business/DependencyInjection.cs`
- `DataAccess/DependencyInjection.cs`
- `Infrastructure/DependencyInjection.cs`

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

### Çok Dilli Altyapı

- Tüm metin içerikli entity'ler `LanguageCode` kolonu içerir (örn. `tr-TR`, `de-DE`).
- Route prefix'i: `/{culture}/...` (varsayılan `tr-TR`).
- Resource dosyaları: `Resources/SharedResource.tr.resx`, `SharedResource.de.resx`.
- Şu an yalnızca Türkçe içerik üretilecek; altyapı çok dile hazır olacak.

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
| `Article.CategoryId → Category` | `SetNull` | Kategori silinince makaleler orphan kalır (manuel taşıma) |
| `Category.ParentCategoryId → Category` | `Restrict` | Alt kategorisi olan kategori silinemez |
| Translation → Parent (Page/Service/Attorney/Category/Tag/Article) | `Cascade` | Parent silinince translation'lar SQL düzeyinde de silinir (hard delete) |
| `AttorneyServices` (M:N) | `Cascade` | Attorney veya Service silinince join kaydı silinir |
| `ArticleTags` (M:N) | `Cascade` | Article veya Tag silinince join kaydı silinir |

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

- **Unit test:** Business katmanı servisleri için xUnit + Moq.
- **Integration test:** API endpoint'leri için WebApplicationFactory.
- **Coverage hedefi:** Business katmanı en az %70.
- Her PR'da test eklenmesi beklenir.

---

## 11. Faz Durumu

**Mevcut Faz:** Faz 0 — Hazırlık

Faz tamamlama bilgileri için `PROGRESS.md` dosyasına bakınız.

---

## 12. Önemli Hatırlatmalar (Claude Code için)

1. **CLAUDE.md ve PROGRESS.md değişiklik gerektirir mi?** — Yeni mimari karar veya bağımlılık eklendiğinde güncelle.
2. **Kod yazmadan önce klasör yapısını kontrol et** — Yanlış katmana kod koyma.
3. **EF Core sorgu yazarken `.Include()` ve `.AsNoTracking()` kullanımına dikkat** — Performans için.
4. **Frontend'de inline JS minimuma** — Tüm JS `wwwroot/js/` altında modüler dosyalarda.
5. **Türkiye Barolar Birliği reklam yasağı** — "Müvekkil yorumları" sayfasında müvekkil ismi/davası ifşa edilmez. "Başarılarımız" yerine "Çalışma alanları" vurgusu yapılır.
6. **KVKK uyumu** — İletişim formu submit'inde aydınlatma onayı checkbox'ı zorunlu, onay tarih-saat ile loglanır.
7. **Çok dilli altyapı şimdi kurulacak** — Sonradan eklemek pahalı olur.
