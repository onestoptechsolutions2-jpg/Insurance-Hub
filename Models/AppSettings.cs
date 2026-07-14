namespace Insurance_Hub.Models
{
    /// <summary>
    /// Singleton settings row for this broker's instance — branding, SMTP, SMS, and the
    /// inbound lead API key. Secret fields (SmtpSenderPassword, AfricasTalkingApiKey,
    /// LeadApiKey) are stored encrypted at rest via Data Protection; ISettingsService is the
    /// only place that decrypts them, and only in memory.
    /// </summary>
    public class AppSettings
    {
        public int Id { get; set; }

        // ── Branding ─────────────────────────────────────────────────────────
        public string BusinessName { get; set; } = "InsuranceHub";
        public string AgentContactEmail { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = "KES";

        // ── SMTP ─────────────────────────────────────────────────────────────
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseSsl { get; set; } = true;
        public string SmtpSenderEmail { get; set; } = string.Empty;
        public string SmtpSenderName { get; set; } = "InsuranceHub";
        public string SmtpSenderPassword { get; set; } = string.Empty;
        public string SmtpAgentEmail { get; set; } = string.Empty;

        // ── SMS (Africa's Talking) ──────────────────────────────────────────
        public bool SmsEnabled { get; set; }
        public string AfricasTalkingUsername { get; set; } = string.Empty;
        public string AfricasTalkingApiKey { get; set; } = string.Empty;
        public string AfricasTalkingSenderId { get; set; } = string.Empty;

        // ── Inbound lead API ────────────────────────────────────────────────
        public string LeadApiKey { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
