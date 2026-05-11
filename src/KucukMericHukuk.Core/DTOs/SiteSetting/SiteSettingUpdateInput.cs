namespace KucukMericHukuk.Core.DTOs.SiteSetting;

public class SiteSettingUpdateInput
{
    public string Group { get; set; } = string.Empty;
    public Dictionary<string, string?> Values { get; set; } = new();
}
