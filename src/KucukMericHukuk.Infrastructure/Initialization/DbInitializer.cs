using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Initialization;

public class DbInitializer : IDbInitializer
{
    private static readonly string[] DefaultRoles = { "Admin", "Editor", "Author" };

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly SeedOptions _seedOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IOptions<SeedOptions> seedOptions,
        IConfiguration configuration,
        ILogger<DbInitializer> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
        _seedOptions = seedOptions.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedSiteSettingsAsync(cancellationToken);

        if (_seedOptions.SeedDemoContent)
        {
            await SeedDemoContentAsync(cancellationToken);
        }
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in DefaultRoles)
        {
            if (await _roleManager.RoleExistsAsync(roleName)) continue;

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = roleName switch
                {
                    "Admin" => "Tam yetkili yönetici",
                    "Editor" => "İçerik düzenleyici",
                    "Author" => "Makale yazarı (yalnızca kendi makaleleri)",
                    _ => roleName
                }
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Rol oluşturuldu: {RoleName}", roleName);
            }
            else
            {
                _logger.LogError("Rol oluşturulamadı: {RoleName} — {Errors}",
                    roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_seedOptions.AdminEmail) ||
            string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
        {
            _logger.LogWarning(
                "Seed:AdminEmail veya Seed:AdminPassword tanımlı değil. İlk admin kullanıcı oluşturulmadı. " +
                "User Secrets veya environment variable ile tanımlayın: Seed__AdminEmail, Seed__AdminPassword.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(_seedOptions.AdminEmail);
        if (existing is not null)
        {
            _logger.LogInformation("Admin kullanıcısı zaten mevcut: {Email}", _seedOptions.AdminEmail);
            if (!await _userManager.IsInRoleAsync(existing, "Admin"))
            {
                await _userManager.AddToRoleAsync(existing, "Admin");
                _logger.LogInformation("Mevcut kullanıcıya Admin rolü eklendi: {Email}", _seedOptions.AdminEmail);
            }
            return;
        }

        var user = new ApplicationUser
        {
            Email = _seedOptions.AdminEmail,
            UserName = _seedOptions.AdminEmail,
            FullName = _seedOptions.AdminFullName ?? "Sistem Yöneticisi",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, _seedOptions.AdminPassword);
        if (!createResult.Succeeded)
        {
            _logger.LogError(
                "İlk admin kullanıcı oluşturulamadı: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
        if (!roleResult.Succeeded)
        {
            _logger.LogError(
                "Admin rolü atanamadı: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation(
            "İlk admin kullanıcı oluşturuldu: {Email} (FullName: {FullName})",
            user.Email, user.FullName);
    }

    private async Task SeedSiteSettingsAsync(CancellationToken ct)
    {
        // SiteInfo section appsettings'ten okunur; admin sonradan DB'den günceller.
        // Idempotent: Key bazlı kontrol, mevcutsa atla (kullanıcının değişikliklerini ezme).
        var siteInfoSection = _configuration.GetSection(SiteInfoOptions.SectionName);

        // (Key, Group, DataType, Description, DisplayOrder)
        var seedDefinitions = new (string Key, string Group, string DataType, string? Description, int DisplayOrder)[]
        {
            // SiteInfo group
            (SiteSettingKeys.Name, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Site/firma adı (header logo yanı, browser tab).", 1),
            (SiteSettingKeys.Tagline, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Kısa slogan (Ana sayfa title suffix).", 2),
            (SiteSettingKeys.Description, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Genel meta description fallback (her sayfada özel yoksa).", 3),
            (SiteSettingKeys.BaseUrl, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.Url, "Kanonik domain (örn. https://kucukmerichukuk.av.tr).", 4),
            (SiteSettingKeys.Locale, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "OpenGraph locale (örn. tr_TR).", 5),
            (SiteSettingKeys.TwitterHandle, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "@kucukmerichukuk gibi Twitter/X kullanıcı adı (varsa).", 6),
            (SiteSettingKeys.Telephone, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "İletişim telefonu (LegalService schema + footer).", 7),
            (SiteSettingKeys.Email, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.Email, "Genel iletişim e-postası.", 8),
            (SiteSettingKeys.StreetAddress, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Ofis cadde/sokak adresi.", 9),
            (SiteSettingKeys.AddressLocality, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "İlçe (örn. Serdivan).", 10),
            (SiteSettingKeys.AddressRegion, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "İl (örn. Sakarya).", 11),
            (SiteSettingKeys.PostalCode, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Posta kodu.", 12),
            (SiteSettingKeys.AddressCountry, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "ISO 3166 ülke kodu (örn. TR).", 13),
            (SiteSettingKeys.Latitude, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.Decimal, "GeoCoordinates enlem (örn. 40.7889).", 14),
            (SiteSettingKeys.Longitude, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.Decimal, "GeoCoordinates boylam (örn. 30.4036).", 15),
            (SiteSettingKeys.AreaServed, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "LegalService.areaServed alanı (örn. Sakarya, Türkiye).", 16),
            (SiteSettingKeys.OpeningHoursDescription, SiteSettingKeys.Groups.SiteInfo, SiteSettingKeys.DataTypes.String, "Çalışma saatleri açıklama metni.", 17),

            // Seo group
            (SiteSettingKeys.DefaultOgImage, SiteSettingKeys.Groups.Seo, SiteSettingKeys.DataTypes.String, "Sayfa özel görsel yoksa kullanılacak OG image yolu.", 1),
            (SiteSettingKeys.GoogleSearchConsoleVerification, SiteSettingKeys.Groups.Seo, SiteSettingKeys.DataTypes.String, "Google Search Console meta verification token.", 2),
            (SiteSettingKeys.RobotsTxt, SiteSettingKeys.Groups.Seo, SiteSettingKeys.DataTypes.Text, "robots.txt içeriği — boş bırakılırsa varsayılan kullanılır (admin/Identity dizinleri disallow + sitemap referansı).", 3),

            // Integration group
            (SiteSettingKeys.GoogleAnalyticsId, SiteSettingKeys.Groups.Integration, SiteSettingKeys.DataTypes.String, "GA4 measurement ID (G-XXXXXXXXXX).", 1),
            (SiteSettingKeys.GoogleTagManagerId, SiteSettingKeys.Groups.Integration, SiteSettingKeys.DataTypes.String, "GTM container ID (GTM-XXXXXXX).", 2),
            (SiteSettingKeys.MicrosoftClarityId, SiteSettingKeys.Groups.Integration, SiteSettingKeys.DataTypes.String, "Microsoft Clarity project ID.", 3),
            (SiteSettingKeys.FacebookPixelId, SiteSettingKeys.Groups.Integration, SiteSettingKeys.DataTypes.String, "Facebook Pixel ID.", 4),

            // GoogleIntegration group
            (SiteSettingKeys.GoogleAnalyticsPropertyId, SiteSettingKeys.Groups.GoogleIntegration, SiteSettingKeys.DataTypes.String, "GA4 Data API property ID (sadece sayısal ID; G- measurement ID değil).", 1),
            (SiteSettingKeys.GoogleSearchConsoleSiteUrl, SiteSettingKeys.Groups.GoogleIntegration, SiteSettingKeys.DataTypes.String, "Search Console site URL (örn. https://kucukmerichukuk.av.tr/ veya sc-domain:kucukmerichukuk.av.tr).", 2),
            (SiteSettingKeys.GoogleServiceAccountJson, SiteSettingKeys.Groups.GoogleIntegration, SiteSettingKeys.DataTypes.Text, "Service account JSON. Boş bırakılırsa GoogleIntegration__ServiceAccountJson/Base64/FilePath env-var kaynakları denenir.", 3),
        };

        var addedCount = 0;
        foreach (var def in seedDefinitions)
        {
            var exists = await _db.Set<SiteSetting>().AnyAsync(s => s.Key == def.Key, ct);
            if (exists) continue;

            // SiteInfo group için appsettings'ten initial value oku; diğer gruplar boş başlar
            string? initialValue = def.Group == SiteSettingKeys.Groups.SiteInfo
                ? siteInfoSection[def.Key]
                : null;

            _db.Set<SiteSetting>().Add(new SiteSetting
            {
                Key = def.Key,
                Value = initialValue,
                Group = def.Group,
                DataType = def.DataType,
                Description = def.Description,
                DisplayOrder = def.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("SiteSettings seed: {Count} yeni ayar eklendi (appsettings SiteInfo bölümünden hydrate).", addedCount);
        }
        else
        {
            _logger.LogInformation("SiteSettings seed: tüm key'ler zaten mevcut, ekleme yapılmadı.");
        }
    }

    private async Task SeedDemoContentAsync(CancellationToken ct)
    {
        _logger.LogInformation("Demo content seed başlatılıyor (tablo başına idempotent)...");

        // Faz 4.6 DEFERRED kapanışı: 'iletisim' → 'contact' kanonikleştirme (PageKey English standardı).
        // Idempotent: zaten 'contact' ise no-op.
        var iletisim = await _db.Set<Page>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PageKey == "iletisim", ct);
        if (iletisim != null)
        {
            iletisim.PageKey = "contact";
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: PageKey 'iletisim' → 'contact' güncellendi.");
        }

        if (!await _db.Set<Category>().AnyAsync(ct))
        {
            var cezaCat = new Category
            {
                DisplayOrder = 1,
                IsActive = true,
                Translations = new List<CategoryTranslation>
                {
                    new() { LanguageCode = "tr-TR", Name = "Ceza Hukuku", Slug = "ceza-hukuku" }
                }
            };
            var aileCat = new Category
            {
                DisplayOrder = 2,
                IsActive = true,
                Translations = new List<CategoryTranslation>
                {
                    new() { LanguageCode = "tr-TR", Name = "Aile Hukuku", Slug = "aile-hukuku" }
                }
            };
            _db.Set<Category>().AddRange(cezaCat, aileCat);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 2 kategori eklendi.");
        }

        if (!await _db.Set<Service>().AnyAsync(ct))
        {
            var s1 = new Service
        {
            DisplayOrder = 1,
            IsActive = true,
            Icon = "scale",
            Translations = new List<ServiceTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    Name = "Ceza Hukuku",
                    Slug = "ceza-hukuku",
                    // Faz 6.14: meta description uzunluğu (Lighthouse SEO)
                    ShortDescription = "Soruşturma, kovuşturma ve infaz aşamalarında deneyimli temsil. Müvekkillerimize geniş kapsamlı ceza hukuku danışmanlığı ve dava takibi hizmetleri.",
                    FullDescription = "<p>Ceza hukuku alanında müvekkillerimize geniş kapsamlı danışmanlık ve dava takibi hizmeti sağlıyoruz. Soruşturma, kovuşturma ve infaz aşamalarında deneyimli temsil sunuyoruz.</p>"
                }
            }
        };
        var s2 = new Service
        {
            DisplayOrder = 2,
            IsActive = true,
            Icon = "users",
            Translations = new List<ServiceTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    Name = "Aile Hukuku",
                    Slug = "aile-hukuku",
                    ShortDescription = "Anlaşmalı ve çekişmeli boşanma, velayet, nafaka ve mal paylaşımı davalarında hassas yaklaşım. Sürecin her aşamasında müvekkilimizin yanındayız.",
                    FullDescription = "<p>Aile hukuku konularında hassas yaklaşımla, sürecin her aşamasında yanınızdayız. Anlaşmalı ve çekişmeli boşanma, velayet, nafaka ve mal paylaşımı davaları.</p>"
                }
            }
        };
        var s3 = new Service
        {
            DisplayOrder = 3,
            IsActive = true,
            Icon = "briefcase",
            Translations = new List<ServiceTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    Name = "İş Hukuku",
                    Slug = "is-hukuku",
                    ShortDescription = "Kıdem tazminatı, ihbar tazminatı, işe iade ve iş kazası davalarında işçi ve işveren tarafında deneyimli temsil ve danışmanlık hizmetleri.",
                    FullDescription = "<p>İş hukuku alanında işçi ve işveren tarafında deneyimli temsil. Kıdem tazminatı, ihbar tazminatı, işe iade ve iş kazası davaları.</p>"
                }
            }
        };
            _db.Set<Service>().AddRange(s1, s2, s3);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 3 servis eklendi.");
        }

        if (!await _db.Set<Attorney>().AnyAsync(ct))
        {
            var attorney = new Attorney
        {
            DisplayOrder = 1,
            IsActive = true,
            Email = "info@kucukmerichukuk.av.tr",
            BarName = "Sakarya Barosu",
            Translations = new List<AttorneyTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    FullName = "Av. Demo Kullanıcı",
                    Title = "Kurucu Avukat",
                    Slug = "av-demo-kullanici",
                    ShortBio = "Hukuk alanında deneyimli avukat. Detaylı içerik müşteri tarafından girilecek.",
                    FullBio = "<p>Detaylı biyografi içeriği müşteri tarafından girildiğinde bu alana gelecek.</p>"
                }
            }
        };
            _db.Set<Attorney>().Add(attorney);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 1 avukat eklendi.");
        }

        if (!await _db.Set<Article>().AnyAsync(ct))
        {
            var cezaCat = await _db.Set<Category>()
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Translations.Any(t => t.Slug == "ceza-hukuku"), ct);
            var aileCat = await _db.Set<Category>()
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Translations.Any(t => t.Slug == "aile-hukuku"), ct);

            var a1 = new Article
            {
                CategoryId = cezaCat?.Id,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                IsFeatured = true,
                Translations = new List<ArticleTranslation>
                {
                    new()
                    {
                        LanguageCode = "tr-TR",
                        Title = "Hukuki Süreçlerde Bilinmesi Gerekenler",
                        Slug = "hukuki-sureclerde-bilinmesi-gerekenler",
                        Excerpt = "Bir hukuki süreç başlatmadan önce dikkat edilmesi gereken temel noktalar.",
                        Content = "<p>Hukuki süreçler sabır ve uzman desteği gerektirir. Bu yazıda temel adımları ele alıyoruz.</p><p>Demo içerik — gerçek makale müşteri tarafından girilecek.</p>",
                        ReadingTimeMinutes = 3
                    }
                }
            };
            var a2 = new Article
            {
                CategoryId = aileCat?.Id,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-14),
                Translations = new List<ArticleTranslation>
                {
                    new()
                    {
                        LanguageCode = "tr-TR",
                        Title = "Boşanma Davalarında Çocuk Velayeti",
                        Slug = "bosanma-davalarinda-cocuk-velayeti",
                        Excerpt = "Velayet kararlarında mahkemenin dikkate aldığı kriterler.",
                        Content = "<p>Velayet konusunda mahkeme çocuğun üstün yararını gözetir. Bu yazıda temel kriterleri ele alıyoruz.</p>",
                        ReadingTimeMinutes = 4
                    }
                }
            };
            var a3 = new Article
            {
                CategoryId = aileCat?.Id,
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-21),
                Translations = new List<ArticleTranslation>
                {
                    new()
                    {
                        LanguageCode = "tr-TR",
                        Title = "Anlaşmalı Boşanmada Mal Paylaşımı",
                        Slug = "anlasmali-bosanmada-mal-paylasimi",
                        Excerpt = "Edinilmiş mallara katılma rejiminde temel ilkeler.",
                        Content = "<p>Anlaşmalı boşanmada mal paylaşımı için tarafların üzerinde anlaştığı protokol mahkemece onaylanır. Bu yazıda dikkat edilecek noktaları ele alıyoruz.</p>",
                        ReadingTimeMinutes = 5
                    }
                }
            };
            _db.Set<Article>().AddRange(a1, a2, a3);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 3 makale eklendi (kategorili: 1 Ceza + 2 Aile).");
        }

        var pageSeeds = new[]
        {
            new
            {
                Key = "about", DisplayOrder = 1,
                Title = "Hakkımızda", Slug = "hakkimizda",
                Content = "<h2>Küçükmeriç Hukuk Bürosu</h2><p>Sakarya Serdivan'da hizmet veren büromuz, müvekkillerimize geniş hukuki alanlarda uzmanlık ve titiz takip sağlamaktadır.</p><p>Misyonumuz, etik ve şeffaf çalışma ilkeleri çerçevesinde adil hukuki çözümler üretmektir.</p>",
                MetaTitle = "Hakkımızda — Küçükmeriç Hukuk Bürosu",
                // Faz 6.14: 72 → 152 char (Google 155-160 hedef)
                MetaDescription = "Sakarya Serdivan'da hizmet veren Küçükmeriç Hukuk Bürosu — kurucularımız, çalışma alanlarımız ve müvekkillerimize sunduğumuz hukuki hizmet anlayışı."
            },
            new
            {
                Key = "privacy", DisplayOrder = 2,
                Title = "Gizlilik Politikası (KVKK)", Slug = "gizlilik",
                Content = "<h2>Kişisel Verilerin Korunması</h2><p>6698 sayılı Kişisel Verilerin Korunması Kanunu (KVKK) kapsamında, kişisel verilerinizin işlenmesi ve korunması ile ilgili politikalarımız bu sayfada açıklanmaktadır.</p><p><em>Bu içerik müşteri tarafından detaylandırılacaktır.</em></p>",
                MetaTitle = "Gizlilik Politikası — KVKK",
                // Faz 6.16: 67 → 137 char
                MetaDescription = "Küçükmeriç Hukuk Bürosu kişisel verilerin korunması, KVKK uyumluluğu, ziyaretçi gizlilik hakları ve veri işleme politikalarına ilişkin metin."
            },
            new
            {
                Key = "cookie", DisplayOrder = 3,
                Title = "Çerez Politikası", Slug = "cerez-politikasi",
                Content = "<h2>Çerez Kullanımı</h2><p>Web sitemiz kullanıcı deneyimini iyileştirmek için çerezler kullanmaktadır. Bu çerezlerin türleri ve kullanım amaçları hakkında detaylı bilgi bu sayfada yer alır.</p><p><em>Bu içerik müşteri tarafından detaylandırılacaktır.</em></p>",
                MetaTitle = "Çerez Politikası",
                // Faz 6.16: 47 → 132 char
                MetaDescription = "Küçükmeriç Hukuk Bürosu web sitesinde çerez kullanımı, türleri, amaçları ve ziyaretçi tercihleri hakkında detaylı bilgi."
            },
            new
            {
                Key = "terms", DisplayOrder = 4,
                Title = "Kullanım Koşulları", Slug = "kullanim-kosullari",
                Content = "<h2>Site Kullanım Koşulları</h2><p>Bu web sitesini ziyaret eden ve kullanan ziyaretçilerimizin uyması beklenen koşullar bu sayfada açıklanmaktadır.</p><p><em>Bu içerik müşteri tarafından detaylandırılacaktır.</em></p>",
                MetaTitle = "Kullanım Koşulları",
                // Faz 6.16: 24 → 144 char
                MetaDescription = "Küçükmeriç Hukuk Bürosu web sitesi kullanım koşulları, hizmet sınırları, ziyaretçi sorumlulukları ve fikri mülkiyet haklarına ilişkin yasal metin."
            },
            new
            {
                Key = "disclosure", DisplayOrder = 5,
                Title = "Aydınlatma Metni", Slug = "aydinlatma",
                Content = "<h2>KVKK Aydınlatma Metni</h2><p>6698 sayılı Kişisel Verilerin Korunması Kanunu (KVKK) kapsamında, iletişim formu üzerinden tarafımıza ilettiğiniz kişisel verileriniz (ad-soyad, e-posta, telefon, mesaj içeriği) yalnızca tarafınıza geri dönüş sağlamak ve hukuki danışmanlık talebinizi değerlendirmek amacıyla işlenmektedir.</p><p>Verileriniz üçüncü kişilerle paylaşılmamaktadır. KVKK m.11 uyarınca verilerinize ilişkin haklarınızı kullanmak için bizimle iletişime geçebilirsiniz.</p><p><em>Bu metin müşteri tarafından detaylandırılacaktır.</em></p>",
                MetaTitle = "Aydınlatma Metni — KVKK",
                // Faz 6.16: 63 → 139 char
                MetaDescription = "Küçükmeriç Hukuk Bürosu KVKK kapsamında kişisel verilerin işlenmesi, saklanması ve aktarılmasına ilişkin aydınlatma metni."
            }
        };

        var addedPageCount = 0;
        foreach (var seed in pageSeeds)
        {
            // Idempotent: PageKey'e göre kontrol (dil-bağımsız teknik ID).
            var exists = await _db.Set<Page>().AnyAsync(p => p.PageKey == seed.Key, ct);
            if (exists) continue;

            var page = new Page
            {
                PageKey = seed.Key,
                IsActive = true,
                DisplayOrder = seed.DisplayOrder,
                Translations = new List<PageTranslation>
                {
                    new()
                    {
                        LanguageCode = "tr-TR",
                        Title = seed.Title,
                        Slug = seed.Slug,
                        Content = seed.Content,
                        MetaTitle = seed.MetaTitle,
                        MetaDescription = seed.MetaDescription
                    }
                }
            };
            _db.Set<Page>().Add(page);
            addedPageCount++;
        }
        if (addedPageCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: {Count} sayfa eklendi (eksik PageKey'ler).", addedPageCount);
        }
        else
        {
            _logger.LogInformation("Demo seed: tüm PageKey'ler zaten mevcut, sayfa eklenmedi.");
        }

        if (!await _db.Set<Faq>().AnyAsync(ct))
        {
            var faqs = new[]
            {
                new Faq
                {
                    DisplayOrder = 1, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Ücretsiz danışmanlık veriyor musunuz?",
                            Answer = "İlk görüşmemizi kısa bir tanışma seansı olarak ücretsiz yürütüyoruz. Davanın detaylarına girildiğinde, hizmet kapsamına göre ücretlendirme yapılır." }
                    }
                },
                new Faq
                {
                    DisplayOrder = 2, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Hangi hukuk alanlarında çalışıyorsunuz?",
                            Answer = "Ceza, aile, iş, gayrimenkul ve ticaret hukuku başta olmak üzere geniş bir alanda hizmet veriyoruz. Detaylı bilgi için Hizmet Alanları sayfamızı inceleyebilirsiniz." }
                    }
                },
                new Faq
                {
                    DisplayOrder = 3, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Vekalet sürecinizi nasıl ilerletiyorsunuz?",
                            Answer = "İlk görüşmenin ardından dava değerlendirmesi yapılır, vekaletname düzenlenir ve süreç müvekkille birlikte planlanır. Her aşamada bilgilendirme yapılır." }
                    }
                },
                new Faq
                {
                    DisplayOrder = 4, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Online görüşme imkânınız var mı?",
                            Answer = "Evet, talebiniz halinde Zoom veya Google Meet üzerinden online görüşme planlayabiliyoruz. Şehir dışı müvekkillerimiz için bu seçenek aktif olarak kullanılmaktadır." }
                    }
                },
                new Faq
                {
                    DisplayOrder = 5, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Davam ne kadar sürer?",
                            Answer = "Dava süresi konusu, mahkemenin yoğunluğu, delil durumu ve karşı tarafın tutumu gibi faktörlere bağlıdır. İlk görüşmede genel bir takvim öngörüsü sunulabilir." }
                    }
                },
                new Faq
                {
                    DisplayOrder = 6, IsActive = true,
                    Translations = new List<FaqTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Question = "Belgelerimi sizinle paylaşmam güvenli mi?",
                            Answer = "Tüm müvekkil bilgileri Avukatlık Kanunu çerçevesinde sır saklama yükümlülüğü kapsamındadır. Dijital belgeleriniz şifreli ortamlarda saklanır." }
                    }
                }
            };
            _db.Set<Faq>().AddRange(faqs);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 6 SSS eklendi.");
        }

        // Testimonial seed (TBB-safe: inisyal + nötr rol, dava/firma detayı yok)
        if (!await _db.Set<Testimonial>().AnyAsync(ct))
        {
            var testimonials = new[]
            {
                new Testimonial
                {
                    AuthorInitials = "M.A.", AuthorRole = "Müvekkil", Rating = 5,
                    DisplayOrder = 1, IsActive = true, IsFeatured = true,
                    Translations = new List<TestimonialTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Content = "Profesyonel yaklaşım ve detaylı bilgilendirmeyi takdir ediyorum. Sürecin her aşamasında açık iletişim kuruldu." }
                    }
                },
                new Testimonial
                {
                    AuthorInitials = "E.Y.", AuthorRole = "Müvekkil", Rating = 5,
                    DisplayOrder = 2, IsActive = true, IsFeatured = true,
                    Translations = new List<TestimonialTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Content = "Hukuki sürecimde gösterilen özen ve hızlı dönüşler için teşekkür ederim." }
                    }
                },
                new Testimonial
                {
                    AuthorInitials = "K.D.", AuthorRole = "Müvekkil", Rating = 4,
                    DisplayOrder = 3, IsActive = true, IsFeatured = false,
                    Translations = new List<TestimonialTranslation>
                    {
                        new() { LanguageCode = "tr-TR",
                            Content = "Uzman ve güvenilir bir hukuki danışmanlık aldığımı belirtmek isterim." }
                    }
                }
            };
            _db.Set<Testimonial>().AddRange(testimonials);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: 3 müvekkil yorumu eklendi (TBB-safe).");
        }

        var attorneys = await _db.Set<Attorney>().Include(a => a.Services).ToListAsync(ct);
        var allServices = await _db.Set<Service>().ToListAsync(ct);
        var anyChange = false;
        foreach (var att in attorneys)
        {
            foreach (var svc in allServices)
            {
                if (att.Services.All(s => s.Id != svc.Id))
                {
                    att.Services.Add(svc);
                    anyChange = true;
                }
            }
        }
        if (anyChange)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo seed: Attorney-Service M:N bağları eklendi.");
        }

        _logger.LogInformation("Demo content seed tamamlandı.");
    }
}
