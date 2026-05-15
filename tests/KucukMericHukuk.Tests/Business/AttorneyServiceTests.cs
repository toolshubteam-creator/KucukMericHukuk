using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class AttorneyServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<AttorneyInputDto> _validator;

    public AttorneyServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(AttorneyMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new AttorneyInputValidator();
    }

    private AttorneyService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var sanitizer = new HtmlSanitizerService();
        return new AttorneyService(uow, slugService, _mapper, _validator, sanitizer);
    }

    private static AttorneyInputDto BuildValidInput(
        string fullName = "Ahmet Yılmaz",
        string? slug = null,
        bool isActive = true,
        int displayOrder = 0,
        int? id = null,
        int? userId = null,
        string? fullBio = null,
        List<int>? serviceIds = null,
        string? email = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new AttorneyInputDto
        {
            Id = id,
            UserId = userId,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Email = email,
            Translations = new List<AttorneyTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    FullName = fullName,
                    Title = "Av.",
                    Slug = slug ?? string.Empty,
                    ShortBio = "Kısa bio",
                    FullBio = fullBio,
                    Education = null,
                    Publications = null
                }
            },
            ServiceIds = serviceIds ?? new List<int>()
        };
    }

    private static async Task<int> SeedUserAsync(AppDbContext context, string emailPrefix = "u")
    {
        var user = new ApplicationUser
        {
            UserName = $"{emailPrefix}@test.com",
            NormalizedUserName = $"{emailPrefix.ToUpper()}@TEST.COM",
            Email = $"{emailPrefix}@test.com",
            NormalizedEmail = $"{emailPrefix.ToUpper()}@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        context.Set<ApplicationUser>().Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<List<int>> SeedServicesAsync(AppDbContext context, int count)
    {
        var ids = new List<int>();
        for (var i = 0; i < count; i++)
        {
            var svc = new Service
            {
                IsActive = true,
                DisplayOrder = i,
                Translations = new List<ServiceTranslation>
                {
                    new()
                    {
                        LanguageCode = LanguageCodes.Turkish,
                        Name = $"Hizmet {i + 1}",
                        Slug = $"hizmet-{i + 1}",
                        FullDescription = "<p>tam</p>"
                    }
                }
            };
            context.Set<Service>().Add(svc);
            await context.SaveChangesAsync();
            ids.Add(svc.Id);
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

        var result = await sut.CreateAsync(BuildValidInput(fullName: "Ayşe Demir"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<AttorneyTranslation>()
            .First(t => t.AttorneyId == result.Value);
        translation.Slug.Should().Be("ayse-demir");
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
        result.Errors.Should().Contain(e => e.Field == nameof(AttorneyInputDto.Translations));
    }

    [Fact]
    public async Task CreateAsync_FullBioWithScript_ShouldSanitize()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(
            fullName: "XSS Test",
            fullBio: "<p>OK</p><script>alert('xss')</script>");

        var result = await sut.CreateAsync(input);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<AttorneyTranslation>()
            .First(t => t.AttorneyId == result.Value);
        translation.FullBio.Should().Contain("<p>OK</p>");
        translation.FullBio.Should().NotContain("script");
        translation.FullBio.Should().NotContain("alert");
    }

    [Fact]
    public async Task CreateAsync_FullBioNull_ShouldNotCallSanitizer()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(fullName: "Null Bio", fullBio: null);
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<AttorneyTranslation>()
            .First(t => t.AttorneyId == result.Value);
        translation.FullBio.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DisplayOrderNegative_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(displayOrder: -1));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(AttorneyInputDto.DisplayOrder));
    }

    [Fact]
    public async Task CreateAsync_FullNameTooLong_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(fullName: new string('a', 151));

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field != null && e.Field.Contains("FullName"));
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(email: "not-an-email");

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(AttorneyInputDto.Email));
    }

    // -------------------- USERID GUARD --------------------

    [Fact]
    public async Task CreateAsync_UserIdNull_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(userId: null));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithUniqueUserId_ShouldLink()
    {
        await using var context = _factory.CreateContext();
        var userId = await SeedUserAsync(context, "unique");
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(userId: userId));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var attorney = verify.Set<Attorney>().First(a => a.Id == result.Value);
        attorney.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUserId_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var userId = await SeedUserAsync(context, "dup");
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(fullName: "First", slug: "first", userId: userId));
        var second = await sut.CreateAsync(BuildValidInput(fullName: "Second", slug: "second", userId: userId));

        second.IsFailure.Should().BeTrue();
        second.FirstError!.Code.Should().Be(ErrorCodes.Attorney.UserAlreadyLinked);
    }

    // -------------------- SERVICE M:N --------------------

    [Fact]
    public async Task CreateAsync_WithValidServiceIds_ShouldLinkServices()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedServicesAsync(context, 2);
        var sut = CreateSut(context);

        var input = BuildValidInput(serviceIds: ids);
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var attorney = verify.Set<Attorney>()
            .Include(a => a.Services)
            .First(a => a.Id == result.Value);
        attorney.Services.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidServiceId_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(serviceIds: new List<int> { 9999 }));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Attorney.ServiceNotFound);
    }

    [Fact]
    public async Task CreateAsync_ServiceIdsNull_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.ServiceIds = null!;

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ServicesSync_ShouldFullReplace()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedServicesAsync(context, 3);
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(
            fullName: "Sync Test",
            serviceIds: new List<int> { ids[0], ids[1] }));

        var update = BuildValidInput(
            fullName: "Sync Test",
            id: created.Value,
            serviceIds: new List<int> { ids[2] });

        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var attorney = verify.Set<Attorney>()
            .Include(a => a.Services)
            .First(a => a.Id == created.Value);
        attorney.Services.Should().HaveCount(1);
        attorney.Services.First().Id.Should().Be(ids[2]);
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_ValidInput_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(fullName: "Eski"));
        var update = BuildValidInput(fullName: "Yeni", id: created.Value);

        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_PreservesTranslationIdAndCreatedAt()
    {
        // Faz 7.1.2: diff-based merge mevcut translation Id ve CreatedAt'i korumalı.
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(fullName: "Eski Av"));
        var originalId = context.Set<AttorneyTranslation>().First(t => t.AttorneyId == created.Value).Id;
        var originalCreatedAt = context.Set<AttorneyTranslation>().First(t => t.AttorneyId == created.Value).CreatedAt;

        var update = BuildValidInput(fullName: "Yeni Av", id: created.Value);
        var result = await sut.UpdateAsync(update);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var t = verify.Set<AttorneyTranslation>().First(x => x.AttorneyId == created.Value);
        t.Id.Should().Be(originalId, "Translation Id KORUNMALI (diff-merge)");
        t.CreatedAt.Should().Be(originalCreatedAt);
        t.FullName.Should().Be("Yeni Av");
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
        result.FirstError!.Code.Should().Be(ErrorCodes.Attorney.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_UserIdAlreadyLinkedExceptSelf_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var userId = await SeedUserAsync(context, "self");
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(userId: userId));

        // Aynı userId ile update — kendisi excludeId, başkası tarafından kullanılmıyor → success
        var update = BuildValidInput(id: created.Value, userId: userId);
        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();
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
        var attorney = verify.Set<Attorney>().IgnoreQueryFilters().First(a => a.Id == created.Value);
        attorney.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DeletedAttorney_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);
        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var attorney = verify.Set<Attorney>().First(a => a.Id == created.Value);
        attorney.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesAttorneyServicesJoin()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedServicesAsync(context, 2);
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(serviceIds: ids));
        await sut.DeleteAsync(created.Value);
        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Attorney>().IgnoreQueryFilters().Any(a => a.Id == created.Value);
        exists.Should().BeFalse();

        // Service'ler korundu (cascade sadece join row'larini siler)
        var servicesExist = verify.Set<Service>().Count(s => ids.Contains(s.Id));
        servicesExist.Should().Be(2);
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Attorney.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_InactiveAttorney_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(fullName: "Pasif", slug: "pasif", isActive: false));

        var result = await sut.GetBySlugAsync("pasif", LanguageCodes.Turkish);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Attorney.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ServiceIdFilter()
    {
        await using var context = _factory.CreateContext();
        var ids = await SeedServicesAsync(context, 2);
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(fullName: "Avukat A", slug: "a", serviceIds: new List<int> { ids[0] }));
        await sut.CreateAsync(BuildValidInput(fullName: "Avukat B", slug: "b", serviceIds: new List<int> { ids[1] }));
        await sut.CreateAsync(BuildValidInput(fullName: "Avukat C", slug: "c"));

        var query = new AttorneyQueryDto { ServiceId = ids[0], LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    public void Dispose() => _factory.Dispose();
}
