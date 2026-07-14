using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/settings")]
    public class AdminSettingsController : Controller
    {
        private readonly ISettingsService _settings;
        private readonly IEmailService _email;

        public AdminSettingsController(ISettingsService settings, IEmailService email)
        {
            _settings = settings;
            _email    = email;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var settings = await _settings.GetAsync();
            return View(settings);
        }

        [HttpPost("branding")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Branding(string businessName, string? agentContactEmail, string? currencySymbol)
        {
            var settings = await _settings.GetAsync();
            settings.BusinessName      = businessName.Trim();
            settings.AgentContactEmail = agentContactEmail?.Trim() ?? string.Empty;
            settings.CurrencySymbol    = string.IsNullOrWhiteSpace(currencySymbol) ? "KES" : currencySymbol.Trim();
            await _settings.SaveAsync(settings);

            TempData["Success"] = "Branding updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("smtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Smtp(
            string? smtpHost, int smtpPort, bool smtpUseSsl,
            string? smtpSenderEmail, string? smtpSenderName, string? smtpSenderPassword,
            string? smtpAgentEmail)
        {
            var settings = await _settings.GetAsync();
            settings.SmtpHost           = smtpHost?.Trim() ?? string.Empty;
            settings.SmtpPort           = smtpPort;
            settings.SmtpUseSsl         = smtpUseSsl;
            settings.SmtpSenderEmail    = smtpSenderEmail?.Trim() ?? string.Empty;
            settings.SmtpSenderName     = smtpSenderName?.Trim() ?? string.Empty;
            settings.SmtpAgentEmail     = smtpAgentEmail?.Trim() ?? string.Empty;
            // Leave settings.SmtpSenderPassword as-is (still decrypted plaintext from GetAsync) unless
            // the admin typed a new one — SaveAsync only overwrites secrets when non-blank.
            settings.SmtpSenderPassword = smtpSenderPassword?.Trim() ?? string.Empty;
            await _settings.SaveAsync(settings);

            TempData["Success"] = "Email settings updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("sms")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sms(
            bool smsEnabled, string? africasTalkingUsername,
            string? africasTalkingApiKey, string? africasTalkingSenderId)
        {
            var settings = await _settings.GetAsync();
            settings.SmsEnabled             = smsEnabled;
            settings.AfricasTalkingUsername = africasTalkingUsername?.Trim() ?? string.Empty;
            settings.AfricasTalkingSenderId = africasTalkingSenderId?.Trim() ?? string.Empty;
            settings.AfricasTalkingApiKey   = africasTalkingApiKey?.Trim() ?? string.Empty;
            await _settings.SaveAsync(settings);

            TempData["Success"] = "SMS settings updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("regenerate-api-key")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateApiKey()
        {
            var newKey = await _settings.RegenerateLeadApiKeyAsync();
            TempData["Success"]  = "A new inbound lead API key was generated. Copy it now — it won't be shown again in full.";
            TempData["NewApiKey"] = newKey;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("test-email")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail()
        {
            var testData = new QuoteEmailData(
                ClientName:     "Test Client",
                ClientEmail:    "test@example.com",
                PlanName:       "Test Plan",
                PlanTier:       "Standard",
                ProviderName:   "Test Provider",
                InsuranceType:  "Life",
                MonthlyPremium: 0m,
                CoverageLimit:  0m,
                Features:       "This is a test email from your Insurance Hub settings panel.",
                RequestedAt:    DateTime.UtcNow
            );

            await _email.SendQuoteRequestToAgentAsync(testData);
            TempData["Success"] = "Test email sent — check the agent inbox configured above (if SMTP is configured).";
            return RedirectToAction(nameof(Index));
        }
    }
}
