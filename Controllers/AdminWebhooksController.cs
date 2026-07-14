using System.Security.Cryptography;
using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/webhooks")]
    public class AdminWebhooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebhookDispatcher _dispatcher;
        private readonly IDataProtector _protector;

        public AdminWebhooksController(
            ApplicationDbContext db, IWebhookDispatcher dispatcher, IDataProtectionProvider dataProtectionProvider)
        {
            _db         = db;
            _dispatcher = dispatcher;
            _protector  = dataProtectionProvider.CreateProtector(SettingsService.ProtectorPurpose);
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var endpoints = await _db.WebhookEndpoints.OrderByDescending(w => w.CreatedAt).ToListAsync();
            return View(endpoints);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string url, WebhookEventType[] events)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                TempData["Error"] = "Enter a valid webhook URL.";
                return RedirectToAction(nameof(Index));
            }

            var combinedEvents = events.Aggregate(default(WebhookEventType), (acc, e) => acc | e);
            var plainSecret    = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

            _db.WebhookEndpoints.Add(new WebhookEndpoint
            {
                Url    = url.Trim(),
                Secret = _protector.Protect(plainSecret),
                Events = combinedEvents
            });
            await _db.SaveChangesAsync();

            TempData["Success"]         = "Webhook endpoint added. Copy the signing secret now — it won't be shown again.";
            TempData["NewWebhookSecret"] = plainSecret;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/toggle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var endpoint = await _db.WebhookEndpoints.FindAsync(id);
            if (endpoint is null) return NotFound();

            endpoint.IsActive = !endpoint.IsActive;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var endpoint = await _db.WebhookEndpoints.FindAsync(id);
            if (endpoint is null) return NotFound();

            _db.WebhookEndpoints.Remove(endpoint);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Webhook endpoint removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/test")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Test(int id)
        {
            var ok = await _dispatcher.TestAsync(id);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Test delivery sent — check the delivery log below for the result."
                : "Webhook endpoint not found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
