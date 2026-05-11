namespace KucukMericHukuk.Core.DTOs.Contact;

public class ContactMessageListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
