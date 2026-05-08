using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IOptions<SeedOptions> seedOptions,
        ILogger<DbInitializer> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
        _seedOptions = seedOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();

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

    private async Task SeedDemoContentAsync(CancellationToken ct)
    {
        _logger.LogInformation("Demo content seed başlatılıyor (tablo başına idempotent)...");

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
                    ShortDescription = "Ceza davalarında uzman hukuki destek.",
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
                    ShortDescription = "Boşanma, velayet ve nafaka davalarında deneyim.",
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
                    ShortDescription = "İşçi ve işveren hakları, iş davaları.",
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

        _logger.LogInformation("Demo content seed tamamlandı.");
    }
}
