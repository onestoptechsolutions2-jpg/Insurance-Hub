namespace Insurance_Hub.Models
{
    /// <summary>Audit trail of every webhook delivery attempt — used by the admin "Test" button and for debugging.</summary>
    public class WebhookDeliveryLog
    {
        public int Id { get; set; }

        public int WebhookEndpointId { get; set; }
        public WebhookEndpoint? WebhookEndpoint { get; set; }

        public string EventType { get; set; } = string.Empty;

        /// <summary>The exact JSON body sent, for debugging.</summary>
        public string Payload { get; set; } = string.Empty;

        public int? ResponseStatusCode { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
