using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class CategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend List (recursive SubCategories — Mapster otomatik)
        config.NewConfig<Category, CategoryListDto>()
            .Map(dest => dest.Name, src => src.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Description, src => src.Translations.Select(t => t.Description).FirstOrDefault())
            .PreserveReference(true);

        // Read: Entity → Admin
        config.NewConfig<Category, CategoryAdminDto>()
            .Map(dest => dest.ParentCategoryName,
                 src => src.ParentCategory != null
                     ? src.ParentCategory.Translations.Select(t => t.Name).FirstOrDefault()
                     : null)
            .Map(dest => dest.ArticleCount, src => src.Articles.Count);
        config.NewConfig<CategoryTranslation, CategoryTranslationDto>();

        // Write
        config.NewConfig<CategoryInputDto, Category>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!)
            .Ignore(dest => dest.ParentCategory!)
            .Ignore(dest => dest.SubCategories)
            .Ignore(dest => dest.Articles);

        config.NewConfig<CategoryTranslationInputDto, CategoryTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
