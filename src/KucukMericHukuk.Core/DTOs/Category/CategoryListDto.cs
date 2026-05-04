namespace KucukMericHukuk.Core.DTOs.Category;

public class CategoryListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int? ParentCategoryId { get; set; }
    public List<CategoryListDto> SubCategories { get; set; } = new();
}
