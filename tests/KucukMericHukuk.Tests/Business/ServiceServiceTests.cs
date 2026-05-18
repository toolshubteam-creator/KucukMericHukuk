using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class ServiceServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<ServiceInputDto> _validator;

    public ServiceServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new ServiceInputValidator();
    }

    private ServiceService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var slugHistoryService = new SlugHistoryService(uow);
        var sanitizer = new HtmlSanitizerService();
        return new ServiceService(uow, slugService, slugHistoryService, _mapper, _validator, sanitizer);
    }

    private static ServiceInputDto BuildValidInput(
        string name = "Aile Hukuku",
        string? slug = null,
        bool isActive = true,
        int displayOrder = 0,
        int? id = null,
        string fullDesc = "<p>Detaylı açıklama.</p>",
        List<int>? attorneyIds = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new ServiceInputDto
        {
            Id = id,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Translations = new List<ServiceTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Name = name,
                    Slug = slug ?? string.Empty,
                    ShortDescription = "Kısa.",
                    FullDescription = fullDesc,
                    MetaTitle = "Meta",
                    MetaDescription = "Meta açıklama"
                }
            },
            AttorneyIds = attorneyIds ?? new List<int>()
        };
    }

    private static async Task<List<int>> SeedAttorneysAsync(AppDbContext context, int count)
    {
        var ids = new List<int>();
        for (var i = 0; i < count; i++)
        {
            var attorney = new Attorney
            {
                IsActive = true,
                Translations = new List<AttorneyTranslation>
                {
                    new()
                    {
                        LanguageCode = LanguageCodes.Turkish,
                        FullName = $"Avukat {i + 1}",
                        Slug = $"avukat-{i + 1}"
                    }
                }
            };
            context.Set<Attorney>().Add(attorney);
            await context.SaveChangesAsync();
            ids.Add(attorney.Id);
        }
        return ids;
    }

    // -------------------- CREATE --------------------

    [Fact]
    public async Task CreateAsync_ValidInput_ShouldReturnNewId()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_SlugBos_OtomatikUretilir()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(name: "İcra Hukuku"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ServiceTranslation>()
            .First(t => t.ServiceId == result.Value);
        translation.Slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task CreateAsync_NoTranslations_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations.Clear();

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(ServiceInputDto.Translations));
    }

    [Fact]
    public async Task CreateAsync_FullDescriptionWithScript_ShouldSanitize()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(
            name: "XSS Test",
            fullDesc: "<p>OK</p><script>alert('xss')</script>");

        var result = await sut.CreateAsync(input);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ServiceTranslation>()
            .First(t => t.ServiceId == result.Value);
        translation.FullDescription.Should().Contain("<p>OK</p>");
        translation.FullDescription.Should().NotContain("script");
        translation.FullDescription.Should().NotContain("alert");
    }

    [Fact]
    public async Task CreateAsync_DisplayOrderNegative_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(displayOrder: -1);

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(ServiceInputDto.DisplayOrder));
    }

    [Fact]
    public async Task CreateAsync_NameTooLong_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: new string('a', 151));

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field != null && e.Field.Contains("Name"));
    }

    // -------------------- M:N ATTORNEYS --------------------

    [Fact]
    public async Task CreateAsync_WithValidAttorneyIds_ShouldLinkAttorneys()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedAttorneysAsync(context, 2);
        var sut = CreateSut(context);

        var input = BuildValidInput(attorneyIds: ids);
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var svc = verify.Set<Service>()
            .Include(s => s.Attorneys)
            .First(s => s.Id == result.Value);
        svc.Attorneys.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAttorneyId_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(attorneyIds: new List<int> { 9999 });
        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Service.AttorneyNotFound);
    }

    [Fact]
    public async Task CreateAsync_AttorneyIdsNull_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.AttorneyIds = null!;

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_AttorneyIdsEmpty_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(attorneyIds: new List<int>()));

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_ValidInput_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Eski"));
        var updateInput = BuildValidInput(name: "Yeni", id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_PreservesTranslationIdAndCreatedAt()
    {
        // Faz 7.1.2: diff-based merge mevcut translation Id ve CreatedAt'i korumalı.
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Eski Hizmet"));
        var originalId = context.Set<ServiceTranslation>().First(t => t.ServiceId == created.Value).Id;
        var originalCreatedAt = context.Set<ServiceTranslation>().First(t => t.ServiceId == created.Value).CreatedAt;

        var updateInput = BuildValidInput(name: "Yeni Hizmet", id: created.Value);
        var result = await sut.UpdateAsync(updateInput);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var t = verify.Set<ServiceTranslation>().First(x => x.ServiceId == created.Value);
        t.Id.Should().Be(originalId, "Translation Id KORUNMALI (diff-merge)");
        t.CreatedAt.Should().Be(originalCreatedAt);
        t.Name.Should().Be("Yeni Hizmet");
    }

    [Fact]
    public async Task UpdateAsync_NoId_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Id = null;

        var result = await sut.UpdateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Common.Validation);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(id: 9999);
        var result = await sut.UpdateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Service.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_AttorneysSync_ShouldFullReplace()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedAttorneysAsync(context, 3);
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(
            name: "Sync Test",
            attorneyIds: new List<int> { ids[0], ids[1] }));

        var updateInput = BuildValidInput(
            name: "Sync Test",
            id: created.Value,
            attorneyIds: new List<int> { ids[2] });

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var svc = verify.Set<Service>()
            .Include(s => s.Attorneys)
            .First(s => s.Id == created.Value);
        svc.Attorneys.Should().HaveCount(1);
        svc.Attorneys.First().Id.Should().Be(ids[2]);
    }

    // -------------------- DELETE / RESTORE / HARD DELETE --------------------

    [Fact]
    public async Task DeleteAsync_SoftDelete()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var svc = verify.Set<Service>().IgnoreQueryFilters().First(s => s.Id == created.Value);
        svc.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DeletedService_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);
        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var svc = verify.Set<Service>().First(s => s.Id == created.Value);
        svc.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesJoinTableRows()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedAttorneysAsync(context, 2);
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(attorneyIds: ids));
        await sut.DeleteAsync(created.Value);
        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Service>().IgnoreQueryFilters().Any(s => s.Id == created.Value);
        exists.Should().BeFalse();

        // Attorney'ler korundu (cascade sadece join row'larını siler)
        var attorneysExist = verify.Set<Attorney>().Count(a => ids.Contains(a.Id));
        attorneysExist.Should().Be(2);
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Service.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_InactiveService_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: "Pasif", slug: "pasif", isActive: false);
        await sut.CreateAsync(input);

        var result = await sut.GetBySlugAsync("pasif", LanguageCodes.Turkish);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Service.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(name: "Aile"));
        await sut.CreateAsync(BuildValidInput(name: "Ceza"));
        await sut.CreateAsync(BuildValidInput(name: "İcra"));

        var query = new ServiceQueryDto { Keyword = "Aile", LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Translations[0].Name.Should().Be("Aile");
    }

    public void Dispose() => _factory.Dispose();
}
