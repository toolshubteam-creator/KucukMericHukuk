# PROGRESS.md — Faz İlerleme Kayıtları

> Her faz tamamlandığında bu dosya güncellenir. Bir sonraki faza aktarılan notlar, bilinen sorunlar ve test sonuçları burada tutulur.

---

## Genel Plan

| Faz | İçerik | Süre | Durum |
| --- | --- | --- | --- |
| 0 | Hazırlık (içerik, marka, kararlar) | 3-5 gün | 🔄 Devam ediyor |
| 1 | Proje kurulumu & mimari | 1 hafta | 🔄 Devam ediyor |
| 2 | Yönetim paneli iskelet & temel modüller | 2 hafta | ⏳ Beklemede |
| 3 | İçerik yönetimi modülleri | 2 hafta | ⏳ Beklemede |
| 4 | Frontend tasarım & geliştirme | 3 hafta | ⏳ Beklemede |
| 5 | SEO, entegrasyon, güvenlik | 1 hafta | ⏳ Beklemede |
| 6 | Test, düzeltme, yayına alma | 1 hafta | ⏳ Beklemede |

**Toplam:** 10 hafta (+2 hafta tampon önerisi)

---

## FAZ 1 — Proje Kurulumu & Mimari — 🔄 DEVAM EDİYOR

**Başlama Tarihi:** 04.05.2026
**Hedef Bitiş:** [Tarih girilecek]

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
