namespace KucukMericHukuk.Core.DTOs.SiteSetting;

public class SiteSettingGroupDto
{
    public string Group { get; set; } = string.Empty;
    public IReadOnlyList<SiteSettingDto> Settings { get; set; } = Array.Empty<SiteSettingDto>();
}
