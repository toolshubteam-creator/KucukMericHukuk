namespace KucukMericHukuk.Core.DTOs.Contact;

public enum ContactMessageStatusFilter
{
    All = 0,
    Unread = 1,
    Read = 2,
    Answered = 3
}

public class ContactMessageQueryDto
{
    public string? Keyword { get; set; }
    public ContactMessageStatusFilter Status { get; set; } = ContactMessageStatusFilter.All;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;
}
