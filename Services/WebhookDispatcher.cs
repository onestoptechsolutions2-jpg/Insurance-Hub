using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Services
{
    public class WebhookDispatcher : IWebhookDispatcher
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;
        private readonly IDataProtector _protector;
        private readonly ILogger<WebhookDispatcher> _logger;

        public WebhookDispatcher(
            ApplicationDbContext db, HttpClient http,
            IDataProtectionProvider dataProtectionProvider, ILogger<WebhookDispatcher> logger)
        {
            _db        = db;
            _http      = http;
            _protector = dataProtectionProvider.CreateProtector(SettingsService.ProtectorPurpose);
            _logger    = logger;
        }

        public async Task DispatchAsync(WebhookEventType eventType, object payload)
        {
            var endpoints = await _db.WebhookEndpoints
                .Where(w => w.IsActive && (w.Events & eventType) == eventType)
                .ToListAsync();

            if (endpoints.Count == 0) return;

            var eventName = ToEventName(eventType);
            var envelope = new
            {
                @event    = eventName,
                timestamp = DateTime.UtcNow.ToString("o"),
                data      = payload
            };
            var json      = JsonSerializer.Serialize(envelope);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            foreach (var endpoint in endpoints)
                await SendToEndpointAsync(endpoint, eventName, json, bodyBytes);
        }

        public async Task<bool> TestAsync(int endpointId)
        {
            var endpoint = await _db.WebhookEndpoints.FindAsync(endpointId);
            if (endpoint is null) return false;

            var envelope = new
            {
                @event    = "webhook.test",
                timestamp = DateTime.UtcNow.ToString("o"),
                data      = new { message = "This is a test delivery from Insurance Hub." }
            };
            var json      = JsonSerializer.Serialize(envelope);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            await SendToEndpointAsync(endpoint, "webhook.test", json, bodyBytes);
            return true;
        }

        private async Task SendToEndpointAsync(WebhookEndpoint endpoint, string eventName, string json, byte[] bodyBytes)
        {
            var log = new WebhookDeliveryLog
            {
                WebhookEndpointId = endpoint.Id,
                EventType         = eventName,
                Payload           = json,
                SentAt            = DateTime.UtcNow
            };

            try
            {
                var secret    = TryUnprotect(endpoint.Secret);
                var signature = Convert.ToHexString(
                    new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(bodyBytes)).ToLowerInvariant();

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new ByteArrayContent(bodyBytes)
                };
                request.Content.Headers.Add("Content-Type", "application/json");
                request.Headers.Add("X-Webhook-Event", eventName);
                request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");

                var response = await _http.SendAsync(request);

                log.ResponseStatusCode = (int)response.StatusCode;
                log.Success             = response.IsSuccessStatusCode;
                endpoint.LastStatus     = log.Success ? "Success" : $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                log.Success         = false;
                log.ErrorMessage    = ex.Message;
                endpoint.LastStatus = "Error: " + ex.Message;
                _logger.LogWarning(ex, "Webhook delivery failed for endpoint {Id} ({Url})", endpoint.Id, endpoint.Url);
            }

            endpoint.LastTriggeredAt = DateTime.UtcNow;
            _db.WebhookDeliveryLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        private string TryUnprotect(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            try { return _protector.Unprotect(value); }
            catch { return string.Empty; }
        }

        private static string ToEventName(WebhookEventType eventType) => eventType switch
        {
            WebhookEventType.QuoteCreated     => "quote.created",
            WebhookEventType.QuoteSold        => "quote.sold",
            WebhookEventType.QuoteLost        => "quote.lost",
            WebhookEventType.PolicyRenewalDue => "policy.renewal_due",
            _                                 => eventType.ToString()
        };
    }
}
