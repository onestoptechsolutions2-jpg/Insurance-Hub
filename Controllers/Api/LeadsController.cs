using Insurance_Hub.Data;
using Insurance_Hub.Filters;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers.Api
{
    public record CreateLeadRequest(
        string FullName,
        string Email,
        string? PhoneNumber,
        string? InsuranceType,
        string? PlanDescription,
        int? PlanId,
        string? Source
    );

    /// <summary>
    /// Public endpoint for external lead sources (a landing page, a WhatsApp bot, ad platforms)
    /// to create a quote request without going through the on-site form. Authenticated via
    /// X-Api-Key (see ApiKeyAuthAttribute), not cookie auth — the caller is never a logged-in user.
    /// </summary>
    [ApiController]
    [Route("api/leads")]
    [ApiKeyAuth]
    public class LeadsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;
        private readonly IWebhookDispatcher _webhooks;
        private readonly IClientService _clients;

        public LeadsController(ApplicationDbContext db, IEmailService email, IWebhookDispatcher webhooks, IClientService clients)
        {
            _db       = db;
            _email    = email;
            _webhooks = webhooks;
            _clients  = clients;
        }

        [HttpPost("")]
        public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { error = "fullName and email are required." });

            InsurancePlan? plan = null;
            if (request.PlanId is int planId)
                plan = await _db.InsurancePlans.Include(p => p.Provider).FirstOrDefaultAsync(p => p.Id == planId);

            var client = await _clients.GetOrCreateAsync(request.FullName.Trim(), request.Email.Trim(), request.PhoneNumber);

            var quote = new QuoteRequest
            {
                FullName       = request.FullName.Trim(),
                Email          = request.Email.Trim(),
                PlanName       = plan?.PlanName ?? request.PlanDescription?.Trim() ?? "General inquiry",
                ProviderName   = plan?.Provider?.Name ?? string.Empty,
                MonthlyPremium = plan?.MonthlyPremium ?? 0m,
                InsuranceType  = plan?.Type.ToString() ?? request.InsuranceType?.Trim() ?? string.Empty,
                PlanId         = plan?.Id,
                Source         = request.Source?.Trim(),
                RequestedAt    = DateTime.UtcNow,
                ClientId       = client.Id,
                Status         = QuoteStatus.New
            };

            _db.QuoteRequests.Add(quote);
            await _db.SaveChangesAsync();

            await _webhooks.DispatchAsync(WebhookEventType.QuoteCreated, new
            {
                quoteId        = quote.Id,
                fullName       = quote.FullName,
                email          = quote.Email,
                planName       = quote.PlanName,
                providerName   = quote.ProviderName,
                monthlyPremium = quote.MonthlyPremium,
                insuranceType  = quote.InsuranceType,
                source         = quote.Source,
                requestedAt    = quote.RequestedAt
            });

            await _email.SendQuoteRequestToAgentAsync(new QuoteEmailData(
                ClientName:     quote.FullName,
                ClientEmail:    quote.Email,
                PlanName:       quote.PlanName,
                PlanTier:       "N/A",
                ProviderName:   quote.ProviderName,
                InsuranceType:  quote.InsuranceType,
                MonthlyPremium: quote.MonthlyPremium,
                CoverageLimit:  0m,
                Features:       string.Empty,
                RequestedAt:    quote.RequestedAt
            ));

            return StatusCode(201, new { id = quote.Id, status = quote.Status.ToString() });
        }
    }
}
