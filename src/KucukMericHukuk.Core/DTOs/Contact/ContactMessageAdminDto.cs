namespace KucukMericHukuk.Core.DTOs.Contact;

public class ContactMessageAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool KvkkConsent { get; set; }
    public bool IsRead { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsDeleted { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Admin'in gönderdiği yanıt geçmişi — Details sayfasında render edilir.</summary>
    public List<ContactMessageReplyDto> Replies { get; set; } = new();
}
