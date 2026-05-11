using KucukMericHukuk.Core.DTOs.SiteSetting;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.SiteSettings;

public class SiteSettingsViewModel
{
    public IReadOnlyList<SiteSettingGroupDto> Groups { get; set; } = Array.Empty<SiteSettingGroupDto>();
    public string ActiveGroup { get; set; } = "SiteInfo";
}
