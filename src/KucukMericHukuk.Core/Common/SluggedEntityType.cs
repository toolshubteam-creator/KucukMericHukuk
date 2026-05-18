namespace KucukMericHukuk.Core.Common;

/// <summary>
/// Slug yöneten 6 entity. Mevcut tüketici: `ISlugService` (uniqueness).
/// Faz 7.4'te 2. tüketici eklendi: `SlugHistory` (slug değişim izi) — bu yüzden
/// enum `Core/Interfaces/Services/ISlugService.cs` içinden buraya taşındı.
/// </summary>
public enum SluggedEntityType
{
    Page,
    Service,
    Attorney,
    Category,
    Tag,
    Article
}
