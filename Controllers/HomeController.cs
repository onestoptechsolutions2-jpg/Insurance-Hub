using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;
        private readonly IWebhookDispatcher _webhooks;
        private readonly IClientService _clients;

        public HomeController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IEmailService email,
            IWebhookDispatcher webhooks,
            IClientService clients)
        {
            _db          = db;
            _userManager = userManager;
            _email       = email;
            _webhooks    = webhooks;
            _clients     = clients;
        }

        // GET /  or  /?type=Motor&provider=Britam
        public async Task<IActionResult> Index(string? type, string? provider)
        {
            var query = _db.InsurancePlans
                .Include(p => p.Provider)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type) &&
                Enum.TryParse<InsuranceType>(type, ignoreCase: true, out var parsed))
            {
                query = query.Where(p => p.Type == parsed);
            }

            if (!string.IsNullOrWhiteSpace(provider))
            {
                query = query.Where(p => p.Provider!.Name == provider);
            }

            var plans = await query
                .OrderByDescending(p => p.IsPopular)
                .ThenBy(p => p.Type)
                .ThenBy(p => p.MonthlyPremium)
                .ToListAsync();

            ViewBag.ActiveType     = type ?? "All";
            ViewBag.ActiveProvider = provider ?? "All";
            ViewBag.Providers      = await _db.Providers
                                              .OrderBy(p => p.Name)
                                              .Select(p => p.Name)
                                              .ToListAsync();
            return View(plans);
        }

        // GET /Home/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _db.InsurancePlans
                .Include(p => p.Provider)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan is null) return NotFound();
            return View(plan);
        }

        // GET /Home/Guide — the 3-step intake wizard shell (no server data needed)
        public IActionResult Guide() => View();

        // GET /Home/Recommend?type=Motor&budget=Balanced&priority=Rating
        public async Task<IActionResult> Recommend(string type, string budget, string priority)
        {
            if (!Enum.TryParse<InsuranceType>(type, ignoreCase: true, out var insuranceType))
                return RedirectToAction(nameof(Guide));

            var typeMatches = await _db.InsurancePlans
                .Include(p => p.Provider)
                .Where(p => p.Type == insuranceType)
                .ToListAsync();

            if (!typeMatches.Any())
            {
                ViewBag.InsuranceType = insuranceType;
                return View(new List<InsurancePlan>());
            }

            // Bucket by tier per the budget answer; fall back to the full type-matched set if empty.
            var tierBucket = budget switch
            {
                "Budget"  => typeMatches.Where(p => p.Tier is PlanTier.Basic or PlanTier.Standard).ToList(),
                "Best"    => typeMatches.Where(p => p.Tier is PlanTier.Premium or PlanTier.Comprehensive).ToList(),
                _         => typeMatches.Where(p => p.Tier is PlanTier.Standard or PlanTier.Premium).ToList(), // "Balanced"
            };
            if (!tierBucket.Any()) tierBucket = typeMatches;

            IEnumerable<InsurancePlan> sorted = priority switch
            {
                "Price"   => tierBucket.OrderBy(p => p.MonthlyPremium),
                "Popular" => tierBucket.OrderByDescending(p => p.IsPopular).ThenByDescending(p => p.Rating),
                _         => tierBucket.OrderByDescending(p => p.Rating), // "Rating"
            };

            var top3 = sorted.Take(3).ToList();

            ViewBag.InsuranceType = insuranceType;
            ViewBag.Budget        = budget;
            ViewBag.Priority      = priority;
            ViewBag.Reasons       = top3.ToDictionary(p => p.Id, p => BuildReason(p, priority, budget));

            return View(top3);
        }

        private static string BuildReason(InsurancePlan plan, string priority, string budget) => priority switch
        {
            "Price"   => $"The most budget-friendly {plan.Type} option that fits what you're after.",
            "Popular" => plan.IsPopular
                            ? $"A favourite among our {plan.Type} customers."
                            : $"Highly rated by customers who chose {plan.Type} cover.",
            _         => $"Our highest-rated {plan.Type} option in the {budget.ToLower()} range.",
        };

        // POST /Home/RequestQuote  — requires login
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestQuote(
            int     planId,
            string  planName,
            string  planTier,
            string  providerName,
            decimal monthlyPremium,
            decimal coverageLimit,
            string  insuranceType,
            string  features,
            string  fullName,
            string  email)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["QuoteError"] = "Please fill in your name and email.";
                return RedirectToAction(nameof(Index));
            }

            var client = await _clients.GetOrCreateAsync(fullName.Trim(), email.Trim(), userId: _userManager.GetUserId(User));

            var quote = new QuoteRequest
            {
                FullName       = fullName.Trim(),
                Email          = email.Trim(),
                PlanName       = planName,
                ProviderName   = providerName,
                MonthlyPremium = monthlyPremium,
                InsuranceType  = insuranceType,
                RequestedAt    = DateTime.UtcNow,
                ClientId       = client.Id
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
                requestedAt    = quote.RequestedAt
            });

            // ── Fire emails (non-blocking, errors are logged not thrown) ──
            var emailData = new QuoteEmailData(
                ClientName:     quote.FullName,
                ClientEmail:    quote.Email,
                PlanName:       planName,
                PlanTier:       planTier,
                ProviderName:   providerName,
                InsuranceType:  insuranceType,
                MonthlyPremium: monthlyPremium,
                CoverageLimit:  coverageLimit,
                Features:       features,
                RequestedAt:    quote.RequestedAt
            );

            await Task.WhenAll(
                _email.SendQuoteRequestToAgentAsync(emailData),
                _email.SendQuoteConfirmationToClientAsync(emailData)
            );

            TempData["QuoteSuccess"] =
                $"Thanks {quote.FullName}! Your quote request for \"{planName}\" has been received. " +
                "We'll be in touch within 24 hours. Check your inbox for a confirmation email.";

            return RedirectToAction(nameof(Index));
        }
    }
}
