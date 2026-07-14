namespace Insurance_Hub.Services
{
    /// <summary>
    /// SMS via Africa's Talking (https://africastalking.com) — the standard SMS gateway for
    /// Kenya/East Africa. WhatsApp is deliberately out of scope here: it needs a separately
    /// approved WhatsApp Business sender + message templates, an external process outside this
    /// codebase. This interface is the seam to add it later without touching call sites.
    /// </summary>
    public class AfricasTalkingSmsService : ISmsService
    {
        private const string ApiUrl = "https://api.africastalking.com/version1/messaging";

        private readonly HttpClient _http;
        private readonly ISettingsService _settings;
        private readonly ILogger<AfricasTalkingSmsService> _logger;

        public AfricasTalkingSmsService(HttpClient http, ISettingsService settings, ILogger<AfricasTalkingSmsService> logger)
        {
            _http     = http;
            _settings = settings;
            _logger   = logger;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            var settings = await _settings.GetAsync();

            if (!settings.SmsEnabled || string.IsNullOrWhiteSpace(settings.AfricasTalkingApiKey))
            {
                _logger.LogWarning("SMS not configured or disabled — skipping send to {To}", toPhoneNumber);
                return;
            }

            try
            {
                var form = new Dictionary<string, string>
                {
                    ["username"] = settings.AfricasTalkingUsername,
                    ["to"]       = toPhoneNumber,
                    ["message"]  = message
                };
                if (!string.IsNullOrWhiteSpace(settings.AfricasTalkingSenderId))
                    form["from"] = settings.AfricasTalkingSenderId;

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                request.Headers.Add("apiKey", settings.AfricasTalkingApiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("SMS sent to {To}", toPhoneNumber);
                else
                    _logger.LogWarning("SMS send to {To} failed with HTTP {Status}", toPhoneNumber, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {To}", toPhoneNumber);
            }
        }
    }
}
