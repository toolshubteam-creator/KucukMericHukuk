using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class FaqServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<FaqInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public FaqServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(FaqMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new FaqInputValidator();
        _sanitizer = new HtmlSanitizerService();
    }

    private FaqService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new FaqService(uow, _mapper, _validator, _sanitizer);
    }

    private static FaqInputDto BuildValidInput(
        string question = "Soru?",
        string answer = "Cevap.",
        bool isActive = true,
        int displayOrder = 0,
        int? id = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new FaqInputDto
        {
            Id = id,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Translations = new List<FaqTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Question = question,
                    Answer = answer
                }
            }
        };
    }

    // -------------------- CREATE --------------------

    [Fact]
    public async Task CreateAsync_NoTranslations_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations.Clear();

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(FaqInputDto.Translations));
    }

    [Fact]
    public async Task CreateAsync_ValidInput_TrimsWhitespace()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(question: "  Boşanma davası nasıl açılır?  ", answer: "  Detaylı açıklama.  ");

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tr = verify.Set<FaqTranslation>().First(t => t.FaqId == result.Value);
        tr.Question.Should().Be("Boşanma davası nasıl açılır?");
        tr.Answer.Should().Be("Detaylı açıklama.");
    }

    // -------------------- UPDATE --------------------

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
        result.FirstError!.Code.Should().Be(ErrorCodes.Faq.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTranslations()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(question: "Eski soru", answer: "Eski cevap"));
        var updateInput = BuildValidInput(question: "Yeni soru", answer: "Yeni cevap", id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translations = verify.Set<FaqTranslation>().Where(t => t.FaqId == created.Value).ToList();
        translations.Should().HaveCount(1);
        translations[0].Question.Should().Be("Yeni soru");
        translations[0].Answer.Should().Be("Yeni cevap");
    }

    // -------------------- DELETE / RESTORE / HARD DELETE --------------------

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Faq.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_IsDeletedTrue()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var faq = verify.Set<Faq>().IgnoreQueryFilters().First(f => f.Id == created.Value);
        faq.IsDeleted.Should().BeTrue();
        faq.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_AlreadyActive_IsIdempotent()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var faq = verify.Set<Faq>().First(f => f.Id == created.Value);
        faq.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_DeletedFaq_UnsetsIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var faq = verify.Set<Faq>().First(f => f.Id == created.Value);
        faq.IsDeleted.Should().BeFalse();
        faq.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesFromDb_WithTranslations()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var faqExists = verify.Set<Faq>().IgnoreQueryFilters().Any(f => f.Id == created.Value);
        faqExists.Should().BeFalse();

        var translationCount = verify.Set<FaqTranslation>().IgnoreQueryFilters()
            .Count(t => t.FaqId == created.Value);
        translationCount.Should().Be(0);
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldReturnFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Faq.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter_FiltersByQuestion()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(question: "Boşanma davası nasıl açılır?", answer: "Cevap A"));
        await sut.CreateAsync(BuildValidInput(question: "Miras paylaşımı?", answer: "Cevap B"));
        await sut.CreateAsync(BuildValidInput(question: "Vergi indirimi?", answer: "Cevap C"));

        var query = new FaqQueryDto { Keyword = "Boşanma", LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Translations.Should().HaveCount(1);
        result.Value.Items[0].Translations[0].Question.Should().Contain("Boşanma");
    }

    // ───────────── Faz 6.6a: HtmlSanitizer pipeline ─────────────

    [Fact]
    public async Task CreateAsync_AnswerWithScriptTag_StripsScript()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(answer: "<p>Guvenli cevap</p><script>alert(1)</script>");

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tr = verify.Set<FaqTranslation>().First(t => t.FaqId == result.Value);
        tr.Answer.Should().Contain("<p>Guvenli cevap</p>");
        tr.Answer.Should().NotContain("<script>");
        tr.Answer.Should().NotContain("alert");
    }

    [Fact]
    public async Task CreateAsync_AnswerWithJavascriptHref_RemovesDangerousAttribute()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(answer: "<a href=\"javascript:alert(1)\">click</a>");

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tr = verify.Set<FaqTranslation>().First(t => t.FaqId == result.Value);
        tr.Answer.Should().NotContain("javascript:");
        tr.Answer.Should().NotContain("alert");
    }

    [Fact]
    public async Task CreateAsync_AnswerWithAllowedRichTags_PreservesFormatting()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(answer: "<p><strong>Kalin</strong> ve <em>italik</em> metin, <a href=\"https://example.com\">link</a>.</p>");

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tr = verify.Set<FaqTranslation>().First(t => t.FaqId == result.Value);
        tr.Answer.Should().Contain("<strong>Kalin</strong>");
        tr.Answer.Should().Contain("<em>italik</em>");
        tr.Answer.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAnswerWithSanitized()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(answer: "Eski cevap"));
        var updateInput = BuildValidInput(
            answer: "<p>Yeni</p><img src=\"http://evil.com/x.jpg\" onerror=\"alert(1)\">",
            id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tr = verify.Set<FaqTranslation>().First(t => t.FaqId == created.Value);
        tr.Answer.Should().Contain("<p>Yeni</p>");
        tr.Answer.Should().NotContain("onerror");
        tr.Answer.Should().NotContain("alert");
        // HTTPS-only scheme; http img kaynak kaldırılır
        tr.Answer.Should().NotContain("http://evil.com");
    }

    public void Dispose() => _factory.Dispose();
}
