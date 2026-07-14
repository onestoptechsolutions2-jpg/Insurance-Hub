namespace Insurance_Hub.Models
{
    [Flags]
    public enum WebhookEventType
    {
        QuoteCreated     = 1,
        QuoteSold        = 2,
        QuoteLost        = 4,
        PolicyRenewalDue = 8
    }

    /// <summary>
    /// A broker-configured URL that receives a signed POST when subscribed events fire.
    /// Secret is stored encrypted at rest (same Data Protection purpose as AppSettings secrets)
    /// and only ever decrypted in memory, by WebhookDispatcher, to compute the HMAC signature.
    /// </summary>
    public class WebhookEndpoint
    {
        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public WebhookEventType Events { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastTriggeredAt { get; set; }

        public string? LastStatus { get; set; }

        public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
    }
}
