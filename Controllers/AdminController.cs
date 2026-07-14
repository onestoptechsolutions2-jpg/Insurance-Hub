using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebhookDispatcher _webhooks;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebhookDispatcher webhooks)
        {
            _db = db;
            _userManager = userManager;
            _webhooks = webhooks;
        }

        [HttpGet("")]
        public IActionResult Index() => RedirectToAction(nameof(Quotes));

        // ── QUOTES ────────────────────────────────────────────────────────────

        [HttpGet("quotes")]
        public async Task<IActionResult> Quotes(string? status)
        {
            var query = _db.QuoteRequests.AsQueryable();

            if (Enum.TryParse<QuoteStatus>(status, out var parsed))
                query = query.Where(q => q.Status == parsed);

            var quotes = await query.OrderByDescending(q => q.RequestedAt).ToListAsync();
            ViewBag.StatusFilter = status;
            ViewBag.Users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            return View(quotes);
        }

        [HttpPost("quotes/{id}/status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuoteStatus(int id, QuoteStatus status, string? adminNotes)
        {
            var quote = await _db.QuoteRequests.FindAsync(id);
            if (quote is null) return NotFound();

            quote.Status = status;
            if (adminNotes is not null)
                quote.AdminNotes = adminNotes.Trim();

            await _db.SaveChangesAsync();

            var webhookPayload = new
            {
                quoteId      = quote.Id,
                fullName     = quote.FullName,
                email        = quote.Email,
                planName     = quote.PlanName,
                providerName = quote.ProviderName,
                adminNotes   = quote.AdminNotes
            };

            if (status == QuoteStatus.Sold)
                await _webhooks.DispatchAsync(WebhookEventType.QuoteSold, webhookPayload);
            else if (status == QuoteStatus.Lost)
                await _webhooks.DispatchAsync(WebhookEventType.QuoteLost, webhookPayload);

            TempData["Success"] = "Quote updated.";
            return RedirectToAction(nameof(Quotes));
        }

        // POST /admin/quotes/{id}/convert — book a Sold quote as an active UserPolicy
        [HttpPost("quotes/{id}/convert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertQuoteToPolicy(
            int id, string userId, string? policyNumber,
            DateTime startDate, DateTime renewalDate, decimal monthlyPremium)
        {
            var quote = await _db.QuoteRequests.FindAsync(id);
            if (quote is null) return NotFound();

            if (quote.Status != QuoteStatus.Sold)
            {
                TempData["Error"] = "Only quotes marked as Sold can be converted to a policy.";
                return RedirectToAction(nameof(Quotes));
            }

            if (quote.ConvertedPolicyId is not null)
            {
                TempData["Error"] = "This quote has already been converted to a policy.";
                return RedirectToAction(nameof(Quotes));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Select the client to attach this policy to.";
                return RedirectToAction(nameof(Quotes));
            }

            var policy = new UserPolicy
            {
                UserId           = userId,
                PolicyName       = quote.PlanName,
                ProviderName     = quote.ProviderName,
                InsuranceType    = quote.InsuranceType,
                PolicyNumber     = policyNumber?.Trim() ?? string.Empty,
                MonthlyPremium   = monthlyPremium,
                StartDate        = startDate,
                RenewalDate      = renewalDate,
                RemindersEnabled = true
            };

            _db.UserPolicies.Add(policy);
            await _db.SaveChangesAsync();

            quote.ConvertedPolicyId = policy.Id;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{quote.PlanName}\" booked as an active policy for the client.";
            return RedirectToAction(nameof(Quotes));
        }

        // ── USERS ─────────────────────────────────────────────────────────────

        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpPost("users/{id}/toggle-admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own admin role.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");

            TempData["Success"] = "User role updated.";
            return RedirectToAction(nameof(Users));
        }

        // ── PLANS ─────────────────────────────────────────────────────────────

        [HttpGet("plans")]
        public async Task<IActionResult> Plans()
        {
            var plans = await _db.InsurancePlans
                .Include(p => p.Provider)
                .OrderBy(p => p.Type).ThenBy(p => p.PlanName)
                .ToListAsync();
            return View(plans);
        }

        [HttpGet("plans/create")]
        public async Task<IActionResult> CreatePlan()
        {
            ViewBag.Providers = await _db.Providers.OrderBy(p => p.Name).ToListAsync();
            return View("PlanForm", new InsurancePlan());
        }

        [HttpPost("plans/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(InsurancePlan model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Providers = await _db.Providers.OrderBy(p => p.Name).ToListAsync();
                return View("PlanForm", model);
            }
            _db.InsurancePlans.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan created.";
            return RedirectToAction(nameof(Plans));
        }

        [HttpGet("plans/{id}/edit")]
        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _db.InsurancePlans.FindAsync(id);
            if (plan is null) return NotFound();
            ViewBag.Providers = await _db.Providers.OrderBy(p => p.Name).ToListAsync();
            return View("PlanForm", plan);
        }

        [HttpPost("plans/{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(int id, InsurancePlan model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Providers = await _db.Providers.OrderBy(p => p.Name).ToListAsync();
                return View("PlanForm", model);
            }
            _db.InsurancePlans.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan saved.";
            return RedirectToAction(nameof(Plans));
        }

        [HttpPost("plans/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _db.InsurancePlans.FindAsync(id);
            if (plan is null) return NotFound();
            _db.InsurancePlans.Remove(plan);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan deleted.";
            return RedirectToAction(nameof(Plans));
        }

        // ── PROVIDERS ─────────────────────────────────────────────────────────

        [HttpGet("providers")]
        public async Task<IActionResult> Providers()
        {
            var providers = await _db.Providers.OrderBy(p => p.Name).ToListAsync();
            return View(providers);
        }

        [HttpGet("providers/create")]
        public IActionResult CreateProvider() => View("ProviderForm", new Provider());

        [HttpPost("providers/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProvider(Provider model)
        {
            if (!ModelState.IsValid) return View("ProviderForm", model);
            _db.Providers.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Provider created.";
            return RedirectToAction(nameof(Providers));
        }

        [HttpGet("providers/{id}/edit")]
        public async Task<IActionResult> EditProvider(int id)
        {
            var provider = await _db.Providers.FindAsync(id);
            if (provider is null) return NotFound();
            return View("ProviderForm", provider);
        }

        [HttpPost("providers/{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProvider(int id, Provider model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View("ProviderForm", model);
            _db.Providers.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Provider saved.";
            return RedirectToAction(nameof(Providers));
        }

        [HttpPost("providers/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            var provider = await _db.Providers.FindAsync(id);
            if (provider is null) return NotFound();
            _db.Providers.Remove(provider);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Provider deleted.";
            return RedirectToAction(nameof(Providers));
        }
    }
}
