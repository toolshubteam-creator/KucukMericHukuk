using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Subscriber;

public class SubscriberListDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public SubscriberStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime SubscribedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
