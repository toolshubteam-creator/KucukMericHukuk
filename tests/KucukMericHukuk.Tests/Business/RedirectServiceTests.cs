using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Moq;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.4.3a — RedirectService: insert-time validasyonlar (self/duplicate/cycle)
/// + CheckCycleAsync AJAX endpoint mantığı + cache invalidator çağrımları.
/// </summary>
public class RedirectServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly Mock<IRedirectCacheInvalidator> _cacheMock;

    public RedirectServiceTests()
    {
        _factory = new TestDbContextFactory();
        var config = new TypeAdapterConfig();
        config.Scan(typeof(RedirectMappingConfig).Assembly);
        _mapper = new Mapper(config);
        _cacheMock = new Mock<IRedirectCacheInvalidator>();
    }

    private RedirectService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new RedirectService(uow, _mapper, _cacheMock.Object, context);
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsIdAndInvalidatesCache()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/eski",
            ToPath = "/tr-TR/yeni",
            StatusCode = 301,
            IsActive = true
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        _cacheMock.Verify(c => c.Invalidate("/tr-TR/eski"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SelfRedirect_ReturnsSelfRedirectError()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/x",
            ToPath = "/tr-TR/x",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.SelfRedirect);
    }

    [Fact]
    public async Task CreateAsync_DuplicateFromPath_ReturnsDuplicateError()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/tr-TR/var-olan",
                ToPath = "/tr-TR/hedef-1",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/var-olan",
            ToPath = "/tr-TR/hedef-2",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.DuplicateFromPath);
    }

    /// <summary>
    /// Zincir A→B var, şimdi B→A eklemek isteniyor. Cycle traverse:
    /// from=/B, to=/A → /A başka redirect'in FromPath'i mi? Evet, /A→/B. → /B
    /// → visited'da from olarak /B var → CYCLE.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoStepCycle_ReturnsCycleDetected()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/A",
                ToPath = "/B",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/B",
            ToPath = "/A",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.CycleDetected);
    }

    [Fact]
    public async Task CheckCycleAsync_SelfRedirect_ReturnsNotOk()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CheckCycleAsync("/X", "/X");

        result.Ok.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckCycleAsync_CleanChain_ReturnsOk()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CheckCycleAsync("/yeni", "/temiz-hedef");

        result.Ok.Should().BeTrue();
        result.Message.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCacheForFromPath()
    {
        int id;
        await using (var seed = _factory.CreateContext())
        {
            var r = new Redirect
            {
                FromPath = "/silinecek",
                ToPath = "/baska",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            id = r.Id;
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(c => c.Invalidate("/silinecek"), Times.Once);
    }

    // ============= Faz 7.4.3a-ek: Birleşik liste (Manuel + SlugHistory) =============

    [Fact]
    public async Task GetAdminPagedAsync_NoSourceFilter_ReturnsBothManualAndSlugHistory()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/manuel-eski",
                ToPath = "/manuel-yeni",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            });

            var article = new Article
            {
                Status = ArticleStatus.Published,
                CreatedAt = DateTime.UtcNow,
                Translations = new List<ArticleTranslation>
                {
                    new() { LanguageCode = "tr-TR", Title = "T", Slug = "guncel-slug", Content = "<p>x</p>", CreatedAt = DateTime.UtcNow }
                }
            };
            seed.Set<Article>().Add(article);
            await seed.SaveChangesAsync();
            articleId = article.Id;

            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article,
                EntityId = articleId,
                LanguageCode = "tr-TR",
                OldSlug = "eski-makale-slug",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetAdminPagedAsync(new RedirectQueryDto());

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().Contain(i => i.Source == RedirectSource.Manual && i.FromPath == "/manuel-eski");
        result.Value.Items.Should().Contain(i => i.Source == RedirectSource.SlugHistory && i.FromPath == "/tr-TR/Articles/eski-makale-slug");

        var slugItem = result.Value.Items.First(i => i.Source == RedirectSource.SlugHistory);
        slugItem.ToPath.Should().Be("/tr-TR/Articles/guncel-slug", "SlugHistory satiri current slug ile resolve edilmeli");
        slugItem.TargetDeleted.Should().BeFalse();
        slugItem.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminPagedAsync_SourceFilterManual_ExcludesSlugHistory()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/m", ToPath = "/n", StatusCode = 301, IsActive = true, CreatedAt = DateTime.UtcNow
            });
            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article, EntityId = 1, LanguageCode = "tr-TR",
                OldSlug = "skip-this", CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetAdminPagedAsync(new RedirectQueryDto { Source = RedirectSource.Manual });

        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().OnlyContain(i => i.Source == RedirectSource.Manual);
    }

    [Fact]
    public async Task GetAdminPagedAsync_SourceFilterSlugHistory_ExcludesManual()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/m", ToPath = "/n", StatusCode = 301, IsActive = true, CreatedAt = DateTime.UtcNow
            });
            var article = new Article
            {
                Status = ArticleStatus.Published, CreatedAt = DateTime.UtcNow,
                Translations = new List<ArticleTranslation> { new() { LanguageCode = "tr-TR", Title = "T", Slug = "yeni", Content = "<p>x</p>", CreatedAt = DateTime.UtcNow } }
            };
            seed.Set<Article>().Add(article);
            await seed.SaveChangesAsync();
            articleId = article.Id;

            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article, EntityId = articleId, LanguageCode = "tr-TR",
                OldSlug = "skip-manual", CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetAdminPagedAsync(new RedirectQueryDto { Source = RedirectSource.SlugHistory });

        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().OnlyContain(i => i.Source == RedirectSource.SlugHistory);
    }

    [Fact]
    public async Task GetAdminPagedAsync_SlugHistoryForSoftDeletedEntity_MarksTargetDeleted()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            var article = new Article
            {
                Status = ArticleStatus.Published,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Translations = new List<ArticleTranslation> { new() { LanguageCode = "tr-TR", Title = "T", Slug = "silinmis-guncel", Content = "<p>x</p>", CreatedAt = DateTime.UtcNow } }
            };
            seed.Set<Article>().Add(article);
            await seed.SaveChangesAsync();
            articleId = article.Id;

            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article, EntityId = articleId, LanguageCode = "tr-TR",
                OldSlug = "eski-silinmisin", CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetAdminPagedAsync(new RedirectQueryDto { Source = RedirectSource.SlugHistory });

        result.Value!.TotalCount.Should().Be(1);
        var slugItem = result.Value.Items.Single();
        slugItem.TargetDeleted.Should().BeTrue();
        slugItem.ToPath.Should().BeNull("soft-deleted parent icin current slug resolve null doner");
    }

    [Fact]
    public async Task ToggleActiveAsync_FlipsAndInvalidatesCache()
    {
        int id;
        await using (var seed = _factory.CreateContext())
        {
            var r = new Redirect
            {
                FromPath = "/toggle",
                ToPath = "/hedef",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            id = r.Id;
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.ToggleActiveAsync(id);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var saved = verify.Redirects.First(r => r.Id == id);
        saved.IsActive.Should().BeFalse();
        _cacheMock.Verify(c => c.Invalidate("/toggle"), Times.Once);
    }

    public void Dispose() => _factory.Dispose();
}
