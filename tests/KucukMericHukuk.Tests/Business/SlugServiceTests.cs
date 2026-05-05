using FluentAssertions;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;

namespace KucukMericHukuk.Tests.Business;

public class SlugServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public SlugServiceTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task GenerateUniqueAsync_NoCollision_ShouldReturnBaseSlug()
    {
        await using var context = _factory.CreateContext();
        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.GenerateUniqueAsync(
            "İcra Hukuku", LanguageCodes.Turkish, SluggedEntityType.Page);

        slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task GenerateUniqueAsync_OneCollision_ShouldAppendDash2()
    {
        await using var context = _factory.CreateContext();
        context.Set<Page>().Add(new Page
        {
            PageKey = "icra-1",
            Translations = new List<PageTranslation>
            {
                new() { LanguageCode = LanguageCodes.Turkish, Title = "İcra Hukuku", Slug = "icra-hukuku" }
            }
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.GenerateUniqueAsync(
            "İcra Hukuku", LanguageCodes.Turkish, SluggedEntityType.Page);

        slug.Should().Be("icra-hukuku-2");
    }

    [Fact]
    public async Task GenerateUniqueAsync_DifferentLanguage_ShouldNotCollide()
    {
        await using var context = _factory.CreateContext();
        context.Set<Page>().Add(new Page
        {
            PageKey = "tr-only",
            Translations = new List<PageTranslation>
            {
                new() { LanguageCode = LanguageCodes.Turkish, Title = "İcra Hukuku", Slug = "icra-hukuku" }
            }
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.GenerateUniqueAsync(
            "İcra Hukuku", "en-US", SluggedEntityType.Page);

        slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task GenerateUniqueAsync_ExcludeId_ShouldIgnoreOwnSlug()
    {
        await using var context = _factory.CreateContext();
        var page = new Page
        {
            PageKey = "edit-self",
            Translations = new List<PageTranslation>
            {
                new() { LanguageCode = LanguageCodes.Turkish, Title = "İcra Hukuku", Slug = "icra-hukuku" }
            }
        };
        context.Set<Page>().Add(page);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.GenerateUniqueAsync(
            "İcra Hukuku", LanguageCodes.Turkish, SluggedEntityType.Page, excludeId: page.Id);

        slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task GenerateUniqueAsync_EmptyTitle_ShouldThrow()
    {
        await using var context = _factory.CreateContext();
        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var act = () => sut.GenerateUniqueAsync(
            "!!!@@@###", LanguageCodes.Turkish, SluggedEntityType.Page);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EnsureUniqueAsync_DesiredCollides_ShouldAppendDash2()
    {
        await using var context = _factory.CreateContext();
        context.Set<Page>().Add(new Page
        {
            PageKey = "manuel-1",
            Translations = new List<PageTranslation>
            {
                new() { LanguageCode = LanguageCodes.Turkish, Title = "Manuel", Slug = "manuel-slug" }
            }
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.EnsureUniqueAsync(
            "manuel-slug", LanguageCodes.Turkish, SluggedEntityType.Page);

        slug.Should().Be("manuel-slug-2");
    }

    [Fact]
    public async Task EnsureUniqueAsync_NormalizesInput()
    {
        await using var context = _factory.CreateContext();
        var uow = new UnitOfWork(context);
        var sut = new SlugService(uow);

        var slug = await sut.EnsureUniqueAsync(
            "Manuel Slug ÖZEL", LanguageCodes.Turkish, SluggedEntityType.Page);

        slug.Should().Be("manuel-slug-ozel");
    }

    public void Dispose() => _factory.Dispose();
}
