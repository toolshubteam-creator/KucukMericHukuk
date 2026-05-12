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

    /// <summary>Tarih aralığı filter — CreatedAt &gt;= StartDate (UTC). Inclusive.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Tarih aralığı filter — CreatedAt &lt; EndDate + 1 day (UTC). Repository [start, end+1d) exclusive uygular.</summary>
    public DateTime? EndDate { get; set; }
}
