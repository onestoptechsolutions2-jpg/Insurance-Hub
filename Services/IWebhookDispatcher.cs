using Insurance_Hub.Models;

namespace Insurance_Hub.Services
{
    public interface IWebhookDispatcher
    {
        /// <summary>
        /// Fires <paramref name="payload"/> to every active endpoint subscribed to
        /// <paramref name="eventType"/>. Never throws — per-endpoint failures are caught, logged,
        /// and recorded in WebhookDeliveryLog so one broken endpoint never blocks the caller or
        /// other endpoints.
        /// </summary>
        Task DispatchAsync(WebhookEventType eventType, object payload);

        /// <summary>
        /// Sends a synthetic "webhook.test" payload to one specific endpoint (bypassing its
        /// active/event-subscription filters) so an admin can verify delivery from the UI.
        /// Returns false if the endpoint doesn't exist.
        /// </summary>
        Task<bool> TestAsync(int endpointId);
    }
}
