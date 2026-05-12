namespace KucukMericHukuk.Core.DTOs.Contact;

/// <summary>
/// Admin Dashboard "Mesajlar" widget'ı için: unread sayısı + son mesajlar.
/// </summary>
public class ContactDashboardWidgetDto
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<ContactMessageListDto> RecentMessages { get; set; } = Array.Empty<ContactMessageListDto>();
}
