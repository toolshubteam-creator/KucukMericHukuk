using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Entities;

public class Subscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public SubscriberStatus Status { get; set; } = SubscriberStatus.Active;

    /// <summary>Auth'suz iptal linki için kayıt anında üretilir.</summary>
    public Guid UnsubscribeToken { get; set; } = Guid.NewGuid();

    public bool KvkkConsent { get; set; }
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
