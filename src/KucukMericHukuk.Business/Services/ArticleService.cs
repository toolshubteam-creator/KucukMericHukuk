using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class ArticleService : IArticleService
{
    private const int WordsPerMinute = 200;
    private const int ExcerptMaxLength = 160;

    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly IMapper _mapper;
    private readonly IValidator<ArticleInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public ArticleService(
        IUnitOfWork uow,
        ISlugService slugService,
        IMapper mapper,
        IValidator<ArticleInputDto> validator,
        IHtmlSanitizerService sanitizer)
    {
        _uow = uow;
        _slugService = slugService;
        _mapper = mapper;
        _validator = validator;
        _sanitizer = sanitizer;
    }

    public async Task<IReadOnlyList<ArticleListDto>> GetFeaturedOrRecentAsync(
        string languageCode, int count, CancellationToken ct = default)
    {
        var featured = await _uow.Articles.GetFeaturedAsync(languageCode, count, ct);
        if (featured.Count >= count)
        {
            return _mapper.Map<List<ArticleListDto>>(featured.Take(count).ToList());
        }

        var remaining = count - featured.Count;
        var recent = await _uow.Articles.GetRecentAsync(languageCode, count + featured.Count, ct);
        var featuredIds = featured.Select(a => a.Id).ToHashSet();
        var fillIn = recent.Where(a => !featuredIds.Contains(a.Id)).Take(remaining);

        var combined = featured.Concat(fillIn).ToList();
        return _mapper.Map<List<ArticleListDto>>(combined);
    }

    public async Task<Result<ArticleAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var article = await _uow.Articles.GetByIdForAdminAsync(id, ct);
        if (article is null)
        {
            return Result.Failure<ArticleAdminDto>(
                new Error(ErrorCodes.Article.NotFound, "Makale bulunamadı."));
        }

        return Result.Success(_mapper.Map<ArticleAdminDto>(article));
    }

    public async Task<Result<PagedResult<ArticleAdminDto>>> GetPagedAsync(
        ArticleQueryDto query,
        CancellationToken ct = default)
    {
        var paged = await _uow.Articles.GetAdminPagedAsync(
            keyword: query.Keyword,
            languageCode: query.LanguageCode,
            status: query.Status,
            categoryId: query.CategoryId,
            page: query.Page,
            pageSize: query.PageSize,
            includeDeleted: query.IncludeDeleted,
            ct: ct);

        var mapped = paged.Items.Select(a => _mapper.Map<ArticleAdminDto>(a)).ToList();
        var result = new PagedResult<ArticleAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(ArticleInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        if (input.CategoryId.HasValue)
        {
            var cat = await _uow.Categories.GetByIdAsync(input.CategoryId.Value, ct);
            if (cat is null)
            {
                return Result.Failure<int>(new Error(
                    ErrorCodes.Article.CategoryNotFound,
                    "Seçilen kategori bulunamadı.",
                    field: nameof(input.CategoryId)));
            }
        }

        var translationsResult = await BuildTranslationsAsync(input.Translations, articleId: null, excludeId: null, ct);
        if (translationsResult.IsFailure)
            return Result.Failure<int>(translationsResult.Errors);

        var tags = input.TagIds.Count > 0
            ? await _uow.Tags.GetByIdsAsync(input.TagIds, ct)
            : new List<Tag>();

        var publishedAt = input.PublishedAt;
        if (input.Status == ArticleStatus.Published && publishedAt is null)
            publishedAt = DateTime.UtcNow;

        var article = new Article
        {
            AuthorId = input.AuthorId,
            CategoryId = input.CategoryId,
            FeaturedImageUrl = input.FeaturedImageUrl,
            Status = input.Status,
            PublishedAt = publishedAt,
            IsFeatured = input.IsFeatured,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in translationsResult.Value)
            article.Translations.Add(t);

        foreach (var tag in tags)
            article.Tags.Add(tag);

        await _uow.Articles.AddAsync(article, ct);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success(article.Id);
    }

    public async Task<Result> UpdateAsync(ArticleInputDto input, CancellationToken ct = default)
    {
        if (!input.Id.HasValue || input.Id.Value <= 0)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation,
                "Güncelleme için ID gerekli.",
                field: nameof(input.Id)));
        }

        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        var article = await _uow.Articles.GetByIdForAdminAsync(input.Id.Value, ct);
        if (article is null)
        {
            return Result.Failure(new Error(ErrorCodes.Article.NotFound, "Makale bulunamadı."));
        }

        if (input.CategoryId.HasValue)
        {
            var cat = await _uow.Categories.GetByIdAsync(input.CategoryId.Value, ct);
            if (cat is null)
            {
                return Result.Failure(new Error(
                    ErrorCodes.Article.CategoryNotFound,
                    "Seçilen kategori bulunamadı.",
                    field: nameof(input.CategoryId)));
            }
        }

        var translationsResult = await BuildTranslationsAsync(input.Translations, articleId: article.Id, excludeId: input.Id, ct);
        if (translationsResult.IsFailure)
            return Result.Failure(translationsResult.Errors);

        article.AuthorId = input.AuthorId;
        article.CategoryId = input.CategoryId;
        article.FeaturedImageUrl = input.FeaturedImageUrl;
        article.IsFeatured = input.IsFeatured;
        article.UpdatedAt = DateTime.UtcNow;

        // Status flow: Published'a transition'da PublishedAt ata; geri çekilmede dokunma.
        if (input.Status == ArticleStatus.Published)
        {
            article.PublishedAt = input.PublishedAt ?? article.PublishedAt ?? DateTime.UtcNow;
        }
        else if (input.PublishedAt.HasValue)
        {
            article.PublishedAt = input.PublishedAt;
        }
        article.Status = input.Status;

        article.Translations.Clear();
        foreach (var t in translationsResult.Value)
            article.Translations.Add(t);

        article.Tags.Clear();
        if (input.TagIds.Count > 0)
        {
            var tags = await _uow.Tags.GetByIdsAsync(input.TagIds, ct);
            foreach (var tag in tags)
                article.Tags.Add(tag);
        }

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var article = await _uow.Articles.GetByIdAsync(id, ct);
        if (article is null)
            return Result.Failure(new Error(ErrorCodes.Article.NotFound, "Makale bulunamadı."));

        _uow.Articles.Delete(article);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var article = await _uow.Articles.GetByIdIncludingDeletedAsync(id, ct);
        if (article is null)
            return Result.Failure(new Error(ErrorCodes.Article.NotFound, "Makale bulunamadı."));

        if (!article.IsDeleted)
            return Result.Success();

        _uow.Articles.Restore(article);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var article = await _uow.Articles.GetByIdIncludingDeletedAsync(id, ct);
        if (article is null)
            return Result.Failure(new Error(ErrorCodes.Article.NotFound, "Makale bulunamadı."));

        _uow.Articles.HardDelete(article);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<List<ArticleTranslation>>> BuildTranslationsAsync(
        List<ArticleTranslationInputDto> translations,
        int? articleId,
        int? excludeId,
        CancellationToken ct)
    {
        var errors = new List<Error>();
        var output = new List<ArticleTranslation>();

        for (var i = 0; i < translations.Count; i++)
        {
            var t = translations[i];
            if (string.IsNullOrWhiteSpace(t.Title))
                continue;

            var sanitized = _sanitizer.Sanitize(t.Content ?? string.Empty);

            string slug;
            try
            {
                slug = string.IsNullOrWhiteSpace(t.Slug)
                    ? await _slugService.GenerateUniqueAsync(t.Title, t.LanguageCode, SluggedEntityType.Article, excludeId, ct)
                    : await _slugService.EnsureUniqueAsync(t.Slug, t.LanguageCode, SluggedEntityType.Article, excludeId, ct);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Article.SlugExists,
                    ex.Message,
                    field: $"Translations[{i}].Slug"));
                continue;
            }

            var excerpt = !string.IsNullOrWhiteSpace(t.Excerpt) ? t.Excerpt : ExtractExcerpt(sanitized);

            output.Add(new ArticleTranslation
            {
                ArticleId = articleId ?? 0,
                LanguageCode = t.LanguageCode,
                Title = t.Title.Trim(),
                Slug = slug,
                Excerpt = excerpt,
                Content = sanitized,
                ReadingTimeMinutes = CalculateReadingTime(sanitized),
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        return errors.Count > 0
            ? Result.Failure<List<ArticleTranslation>>(errors)
            : Result.Success(output);
    }

    private static int CalculateReadingTime(string sanitizedHtml)
    {
        if (string.IsNullOrWhiteSpace(sanitizedHtml)) return 1;
        var text = Regex.Replace(sanitizedHtml, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length == 0) return 1;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    private static string? ExtractExcerpt(string sanitizedHtml)
    {
        if (string.IsNullOrWhiteSpace(sanitizedHtml)) return null;
        var text = Regex.Replace(sanitizedHtml, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length == 0) return null;
        return text.Length <= ExcerptMaxLength
            ? text
            : text.Substring(0, ExcerptMaxLength).TrimEnd() + "…";
    }
}
