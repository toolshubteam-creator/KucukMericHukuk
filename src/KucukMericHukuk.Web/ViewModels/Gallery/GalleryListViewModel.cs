using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Media;

namespace KucukMericHukuk.Web.ViewModels.Gallery;

public class GalleryListViewModel
{
    public PagedResult<MediaFilePublicDto> Items { get; init; } = new();
}
