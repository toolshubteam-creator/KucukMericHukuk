using FluentAssertions;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;

namespace KucukMericHukuk.Tests.DataAccess;

public class TestimonialRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public TestimonialRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    private static Testimonial Build(
        string initials,
        int displayOrder,
        bool isActive = true,
        bool isFeatured = false,
        int? rating = 5,
        string content = "Profesyonel ve guvenilir yaklasim.")
    {
        return new Testimonial
        {
            AuthorInitials = initials,
            AuthorRole = "Müvekkil",
            Rating = rating,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            IsFeatured = isFeatured,
            Translations = new List<TestimonialTranslation>
            {
                new() { LanguageCode = LanguageCodes.Turkish, Content = content }
            }
        };
    }

    [Fact]
    public async Task GetActiveOrderedAsync_OnlyActive_OrderedByDisplayOrder()
    {
        await using var context = _factory.CreateContext();
        var repo = new TestimonialRepository(context);

        await repo.AddAsync(Build("A.B.", displayOrder: 2));
        await repo.AddAsync(Build("C.D.", displayOrder: 1));
        await repo.AddAsync(Build("E.F.", displayOrder: 3, isActive: false));
        await context.SaveChangesAsync();

        var result = await repo.GetActiveOrderedAsync(LanguageCodes.Turkish);

        result.Should().HaveCount(2);
        result[0].AuthorInitials.Should().Be("C.D.");
        result[1].AuthorInitials.Should().Be("A.B.");
    }

    [Fact]
    public async Task GetFeaturedOrderedAsync_OnlyFeatured_RespectsMaxCount()
    {
        await using var context = _factory.CreateContext();
        var repo = new TestimonialRepository(context);

        await repo.AddAsync(Build("A.B.", displayOrder: 1, isFeatured: true));
        await repo.AddAsync(Build("C.D.", displayOrder: 2, isFeatured: true));
        await repo.AddAsync(Build("E.F.", displayOrder: 3, isFeatured: true));
        await repo.AddAsync(Build("G.H.", displayOrder: 4, isFeatured: false));
        await context.SaveChangesAsync();

        var result = await repo.GetFeaturedOrderedAsync(LanguageCodes.Turkish, maxCount: 2);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsFeatured);
        result[0].AuthorInitials.Should().Be("A.B.");
    }

    [Fact]
    public async Task GetByIdWithTranslationsAsync_IncludesTranslations()
    {
        int id;
        await using (var ctx = _factory.CreateContext())
        {
            var repo = new TestimonialRepository(ctx);
            var entity = Build("M.A.", displayOrder: 1, content: "Cok memnun kaldim.");
            await repo.AddAsync(entity);
            await ctx.SaveChangesAsync();
            id = entity.Id;
        }

        await using var verify = _factory.CreateContext();
        var verifyRepo = new TestimonialRepository(verify);
        var fetched = await verifyRepo.GetByIdWithTranslationsAsync(id);

        fetched.Should().NotBeNull();
        fetched!.Translations.Should().HaveCount(1);
        fetched.Translations.First().Content.Should().Be("Cok memnun kaldim.");
    }

    [Fact]
    public async Task GetAdminPagedAsync_SearchByContent_FiltersMatchingRecords()
    {
        await using var context = _factory.CreateContext();
        var repo = new TestimonialRepository(context);

        await repo.AddAsync(Build("A.B.", displayOrder: 1, content: "Cok profesyonel yaklasim."));
        await repo.AddAsync(Build("C.D.", displayOrder: 2, content: "Hizli ve guvenilir hizmet."));
        await repo.AddAsync(Build("E.F.", displayOrder: 3, content: "Detayli bilgilendirme."));
        await context.SaveChangesAsync();

        var paged = await repo.GetAdminPagedAsync(
            keyword: "profesyonel",
            languageCode: LanguageCodes.Turkish,
            page: 1,
            pageSize: 20,
            includeDeleted: false);

        paged.TotalCount.Should().Be(1);
        paged.Items.Should().HaveCount(1);
        paged.Items[0].AuthorInitials.Should().Be("A.B.");
    }

    public void Dispose() => _factory.Dispose();
}
