using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Testimonial;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class TestimonialServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<TestimonialInputDto> _validator;

    public TestimonialServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(TestimonialMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new TestimonialInputValidator();
    }

    private TestimonialService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new TestimonialService(uow, _mapper, _validator);
    }

    private static TestimonialInputDto BuildValidInput(
        string content = "Profesyonel ve detayli bilgilendirme aldim, tesekkur ederim.",
        string? initials = "M.A.",
        int? rating = 5,
        int displayOrder = 0,
        bool isActive = true,
        bool isFeatured = false,
        int? id = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new TestimonialInputDto
        {
            Id = id,
            AuthorInitials = initials,
            AuthorRole = "Müvekkil",
            Rating = rating,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            IsFeatured = isFeatured,
            Translations = new List<TestimonialTranslationInputDto>
            {
                new() { LanguageCode = languageCode, Content = content }
            }
        };
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccess()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        await using var verify = _factory.CreateContext();
        var entity = verify.Set<Testimonial>().IgnoreQueryFilters().First(t => t.Id == result.Value);
        entity.AuthorInitials.Should().Be("M.A.");
        entity.AuthorRole.Should().Be("Müvekkil");
        entity.Rating.Should().Be(5);
    }

    [Fact]
    public async Task CreateAsync_RatingOutOfRange_ReturnsValidationFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(rating: 7);

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(TestimonialInputDto.Rating));
    }

    [Fact]
    public async Task CreateAsync_NoTranslations_ReturnsValidationFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations.Clear();

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(TestimonialInputDto.Translations));
    }

    [Fact]
    public async Task CreateAsync_InitialsTooLong_ReturnsValidationFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        // 10 char limit, 15 char input
        var input = BuildValidInput(initials: "TestUserName123");

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(TestimonialInputDto.AuthorInitials));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTranslations()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(content: "Eski yorum icerigi (uzun)."));
        var updateInput = BuildValidInput(
            content: "Yeni yorum icerigi (guncellenmis).",
            id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translations = verify.Set<TestimonialTranslation>()
            .Where(t => t.TestimonialId == created.Value).ToList();
        translations.Should().HaveCount(1);
        translations[0].Content.Should().Be("Yeni yorum icerigi (guncellenmis).");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var entity = verify.Set<Testimonial>().IgnoreQueryFilters().First(t => t.Id == created.Value);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_DeletedTestimonial_UnsetsIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var entity = verify.Set<Testimonial>().First(t => t.Id == created.Value);
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveOrderedAsync_ReturnsOnlyActive_TranslationFlattened()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(content: "Aktif yorum icerigi A.", displayOrder: 1));
        await sut.CreateAsync(BuildValidInput(content: "Aktif yorum icerigi B.", displayOrder: 2));
        await sut.CreateAsync(BuildValidInput(content: "Pasif yorum.", isActive: false));

        var result = await sut.GetActiveOrderedAsync(LanguageCodes.Turkish);

        result.Should().HaveCount(2);
        result[0].Content.Should().Contain("Aktif yorum icerigi A");
        result[1].Content.Should().Contain("Aktif yorum icerigi B");
    }

    [Fact]
    public async Task GetFeaturedOrderedAsync_LimitsToMaxCount()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(content: "Featured 1 yorum icerigi.", displayOrder: 1, isFeatured: true));
        await sut.CreateAsync(BuildValidInput(content: "Featured 2 yorum icerigi.", displayOrder: 2, isFeatured: true));
        await sut.CreateAsync(BuildValidInput(content: "Featured 3 yorum icerigi.", displayOrder: 3, isFeatured: true));
        await sut.CreateAsync(BuildValidInput(content: "Normal yorum icerigi.", isFeatured: false));

        var result = await sut.GetFeaturedOrderedAsync(LanguageCodes.Turkish, maxCount: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Testimonial.NotFound);
    }

    public void Dispose() => _factory.Dispose();
}
