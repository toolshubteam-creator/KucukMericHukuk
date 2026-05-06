using KucukMericHukuk.Core.DTOs.Media;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class MediaFileMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Url ve ThumbnailUrl service katmanında IFileStorageService.GetPublicUrl
        // ile set edilir; mapping'de RelativePath olduğu gibi taşınır.
        config.NewConfig<MediaFile, MediaFileDto>()
            .Map(d => d.Url, s => s.RelativePath)
            .Map(d => d.ThumbnailUrl, s => s.ThumbnailRelativePath);

        config.NewConfig<MediaFile, MediaFileListDto>()
            .Map(d => d.ThumbnailUrl, s => s.ThumbnailRelativePath);
    }
}
