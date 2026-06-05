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
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var quotes = await _db.QuoteRequests
                .Where(q => q.UserId == user.Id)
                .OrderByDescending(q => q.RequestedAt)
                .ToListAsync();

            ViewBag.Quotes = quotes;
            return View(user);
        }

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
    }
}
