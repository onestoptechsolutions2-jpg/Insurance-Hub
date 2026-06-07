using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize]
    [Route("profile")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db          = db;
            _userManager = userManager;
        }

        // GET /profile
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            ViewBag.Quotes = await _db.QuoteRequests
                .Where(q => q.UserId == user.Id)
                .OrderByDescending(q => q.RequestedAt)
                .ToListAsync();

            ViewBag.Policies = await _db.UserPolicies
                .Where(p => p.UserId == user.Id)
                .OrderBy(p => p.RenewalDate)
                .ToListAsync();

            return View(user);
        }

        // POST /profile — update display name & phone
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string? displayName, string? phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            user.DisplayName = displayName?.Trim() ?? string.Empty;
            user.PhoneNumber = phoneNumber?.Trim();
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST /profile/policies/add — register an existing policy
        [HttpPost("policies/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPolicy(
            string policyName, string providerName, string insuranceType,
            string policyNumber, decimal monthlyPremium,
            DateTime startDate, DateTime renewalDate, bool remindersEnabled)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(providerName))
            {
                TempData["PolicyError"] = "Policy name and provider are required.";
                return RedirectToAction(nameof(Index));
            }

            _db.UserPolicies.Add(new UserPolicy
            {
                UserId           = user.Id,
                PolicyName       = policyName.Trim(),
                ProviderName     = providerName.Trim(),
                InsuranceType    = insuranceType,
                PolicyNumber     = policyNumber?.Trim() ?? string.Empty,
                MonthlyPremium   = monthlyPremium,
                StartDate        = startDate,
                RenewalDate      = renewalDate,
                RemindersEnabled = remindersEnabled
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"\"{policyName}\" added to your policies.";
            return RedirectToAction(nameof(Index));
        }

        // POST /profile/policies/delete/{id}
        [HttpPost("policies/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePolicy(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var policy = await _db.UserPolicies
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (policy is not null)
            {
                _db.UserPolicies.Remove(policy);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Policy removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST /profile/policies/toggle-reminders/{id}
        [HttpPost("policies/toggle-reminders/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReminders(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var policy = await _db.UserPolicies
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (policy is not null)
            {
                policy.RemindersEnabled = !policy.RemindersEnabled;
                await _db.SaveChangesAsync();
                TempData["Success"] = policy.RemindersEnabled
                    ? "Reminders enabled."
                    : "Reminders disabled.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
