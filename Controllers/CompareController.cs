using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CompareController(ApplicationDbContext db) => _db = db;

        // GET /compare?ids=1,2,3
        public async Task<IActionResult> Index(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction("Index", "Home");

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                            .Where(n => n > 0)
                            .Distinct()
                            .Take(3)
                            .ToList();

            var plans = await _db.InsurancePlans
                .Include(p => p.Provider)
                .Where(p => idList.Contains(p.Id))
                .ToListAsync();

            // Preserve the order the user selected
            plans = idList
                .Select(id => plans.FirstOrDefault(p => p.Id == id))
                .Where(p => p is not null)
                .ToList()!;

            return View(plans);
        }
    }
}
