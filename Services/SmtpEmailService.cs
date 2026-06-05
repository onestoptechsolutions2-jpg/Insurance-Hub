using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Insurance_Hub.Services
{
    public class EmailSettings
    {
        public string SmtpHost      { get; set; } = string.Empty;
        public int    SmtpPort      { get; set; } = 587;
        public bool   UseSsl        { get; set; } = true;
        public string SenderEmail   { get; set; } = string.Empty;
        public string SenderName    { get; set; } = "InsuranceHub";
        public string SenderPassword{ get; set; } = string.Empty;
        /// <summary>Agent / admin inbox that receives all quote requests.</summary>
        public string AgentEmail    { get; set; } = string.Empty;
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> opts, ILogger<SmtpEmailService> logger)
        {
            _settings = opts.Value;
            _logger   = logger;
        }

        public async Task SendQuoteRequestToAgentAsync(QuoteEmailData data)
        {
            var subject = $"[InsuranceHub] New Quote Request — {data.PlanName} ({data.InsuranceType})";
            var body    = BuildAgentEmailBody(data);
            await SendAsync(_settings.AgentEmail, subject, body);
        }

        public async Task SendQuoteConfirmationToClientAsync(QuoteEmailData data)
        {
            var subject = $"Your quote request for {data.PlanName} has been received";
            var body    = BuildClientEmailBody(data);
            await SendAsync(data.ClientEmail, subject, body);
        }

        // ── Private helpers ───────────────────────────────────────────────

        private async Task SendAsync(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                _logger.LogWarning("Email not configured — skipping send to {To}: {Subject}", to, subject);
                return;
            }

            try
            {
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl   = _settings.UseSsl,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword)
                };

                var message = new MailMessage
                {
                    From       = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject    = subject,
                    Body       = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
            }
        }

        private static string BuildAgentEmailBody(QuoteEmailData d)
        {
            var features = d.Features
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => $"<li>{f}</li>");

            var coverageText = d.CoverageLimit > 0
                ? $"KES {d.CoverageLimit:N0}"
                : "Unlimited / Contact Provider";

            return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8" /></head>
            <body style="font-family:Inter,Arial,sans-serif;background:#f6f9fc;padding:32px;">
              <div style="max-width:560px;margin:auto;background:#fff;border-radius:16px;border:1px solid #e6ebf1;padding:36px;">

                <div style="font-size:22px;font-weight:700;color:#0a2540;margin-bottom:4px;">InsuranceHub</div>
                <div style="font-size:13px;color:#425466;margin-bottom:28px;">New Quote Request</div>

                <div style="background:#f0f4ff;border-radius:10px;padding:18px 20px;margin-bottom:24px;">
                  <div style="font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:.06em;color:#635bff;margin-bottom:6px;">{d.InsuranceType} — {d.PlanTier}</div>
                  <div style="font-size:20px;font-weight:700;color:#0a2540;">{d.PlanName}</div>
                  <div style="font-size:14px;color:#425466;margin-top:2px;">{d.ProviderName}</div>
                  <div style="font-size:26px;font-weight:700;color:#0a2540;margin-top:12px;">KES {d.MonthlyPremium:N2}<span style="font-size:14px;font-weight:400;">/mo</span></div>
                  <div style="font-size:13px;color:#425466;margin-top:4px;">Coverage up to: {coverageText}</div>
                </div>

                <table style="width:100%;border-collapse:collapse;margin-bottom:24px;">
                  <tr>
                    <td style="padding:10px 0;border-bottom:1px solid #e6ebf1;font-size:13px;color:#425466;width:130px;">Client Name</td>
                    <td style="padding:10px 0;border-bottom:1px solid #e6ebf1;font-size:14px;font-weight:600;color:#0a2540;">{d.ClientName}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px 0;border-bottom:1px solid #e6ebf1;font-size:13px;color:#425466;">Client Email</td>
                    <td style="padding:10px 0;border-bottom:1px solid #e6ebf1;font-size:14px;font-weight:600;color:#0a2540;"><a href="mailto:{d.ClientEmail}" style="color:#635bff;">{d.ClientEmail}</a></td>
                  </tr>
                  <tr>
                    <td style="padding:10px 0;font-size:13px;color:#425466;">Requested At</td>
                    <td style="padding:10px 0;font-size:14px;color:#0a2540;">{d.RequestedAt:dd MMM yyyy, HH:mm} UTC</td>
                  </tr>
                </table>

                <div style="font-size:13px;font-weight:600;color:#0a2540;margin-bottom:10px;">Plan Features</div>
                <ul style="margin:0 0 24px;padding-left:20px;font-size:13px;color:#425466;line-height:1.8;">
                  {string.Join("", features)}
                </ul>

                <a href="mailto:{d.ClientEmail}" style="display:inline-block;background:#635bff;color:#fff;font-size:14px;font-weight:600;padding:12px 22px;border-radius:10px;text-decoration:none;">Reply to Client</a>

                <div style="margin-top:28px;padding-top:20px;border-top:1px solid #e6ebf1;font-size:12px;color:#a0aab4;">
                  This request was submitted via InsuranceHub. Please action within 24 hours.
                </div>
              </div>
            </body>
            </html>
            """;
        }

        private static string BuildClientEmailBody(QuoteEmailData d)
        {
            return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8" /></head>
            <body style="font-family:Inter,Arial,sans-serif;background:#f6f9fc;padding:32px;">
              <div style="max-width:560px;margin:auto;background:#fff;border-radius:16px;border:1px solid #e6ebf1;padding:36px;">

                <div style="font-size:22px;font-weight:700;color:#0a2540;margin-bottom:4px;">InsuranceHub</div>
                <div style="font-size:13px;color:#425466;margin-bottom:28px;">Quote Request Confirmation</div>

                <div style="font-size:20px;font-weight:700;color:#0a2540;margin-bottom:8px;">Hi {d.ClientName},</div>
                <p style="font-size:15px;color:#425466;line-height:1.7;margin:0 0 24px;">
                  We've received your quote request for <strong>{d.PlanName}</strong> by <strong>{d.ProviderName}</strong>.
                  One of our advisors will review your request and be in touch within <strong>24 hours</strong>.
                </p>

                <div style="background:#f0f4ff;border-radius:10px;padding:18px 20px;margin-bottom:28px;">
                  <div style="font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:.06em;color:#635bff;margin-bottom:6px;">{d.InsuranceType} — {d.PlanTier}</div>
                  <div style="font-size:18px;font-weight:700;color:#0a2540;">{d.PlanName}</div>
                  <div style="font-size:13px;color:#425466;margin-top:2px;">{d.ProviderName}</div>
                  <div style="font-size:24px;font-weight:700;color:#0a2540;margin-top:12px;">KES {d.MonthlyPremium:N2}<span style="font-size:13px;font-weight:400;">/mo</span></div>
                </div>

                <p style="font-size:13px;color:#425466;line-height:1.7;margin:0 0 28px;">
                  While you wait, feel free to explore more plans on InsuranceHub to find the best coverage for your needs.
                </p>

                <a href="https://insurancehub.com" style="display:inline-block;background:#635bff;color:#fff;font-size:14px;font-weight:600;padding:12px 22px;border-radius:10px;text-decoration:none;">Browse More Plans</a>

                <div style="margin-top:28px;padding-top:20px;border-top:1px solid #e6ebf1;font-size:12px;color:#a0aab4;">
                  &copy; {DateTime.UtcNow.Year} InsuranceHub. If you did not request this quote, please ignore this email.
                </div>
              </div>
            </body>
            </html>
            """;
        }
    }
}
