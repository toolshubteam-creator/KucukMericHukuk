namespace KucukMericHukuk.Core.DTOs.Contact;

public class ContactMessageReplyDto
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string? SentByUserName { get; set; }
}

public class ContactMessageReplyInputDto
{
    public int ContactMessageId { get; set; }
    public string Body { get; set; } = string.Empty;
}
