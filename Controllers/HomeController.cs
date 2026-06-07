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

        public HomeController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IEmailService email)
        {
            _db          = db;
            _userManager = userManager;
            _email       = email;
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

            var quote = new QuoteRequest
            {
                FullName       = fullName.Trim(),
                Email          = email.Trim(),
                PlanName       = planName,
                ProviderName   = providerName,
                MonthlyPremium = monthlyPremium,
                InsuranceType  = insuranceType,
                RequestedAt    = DateTime.UtcNow,
                UserId         = _userManager.GetUserId(User)
            };

            _db.QuoteRequests.Add(quote);
            await _db.SaveChangesAsync();

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
