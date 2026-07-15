using Insurance_Hub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/clients")]
    public class AdminClientsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminClientsController(ApplicationDbContext db) => _db = db;

        // GET /admin/clients?search=jane
        [HttpGet("")]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(term) || c.Email.ToLower().Contains(term));
            }

            var clients = await query.OrderBy(c => c.FullName).ToListAsync();
            ViewBag.Search = search;
            return View("~/Views/Admin/Clients.cshtml", clients);
        }

        // GET /admin/clients/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
            if (client is null) return NotFound();

            ViewBag.Quotes = await _db.QuoteRequests
                .Where(q => q.ClientId == id)
                .OrderByDescending(q => q.RequestedAt)
                .ToListAsync();

            ViewBag.Policies = await _db.UserPolicies
                .Where(p => p.ClientId == id)
                .OrderBy(p => p.RenewalDate)
                .ToListAsync();

            ViewBag.Transactions = await _db.Transactions
                .Where(t => t.ClientId == id)
                .OrderByDescending(t => t.RecordedAt)
                .ToListAsync();

            return View("~/Views/Admin/ClientDetail.cshtml", client);
        }

        // POST /admin/clients/{id}/notes
        [HttpPost("{id}/notes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotes(int id, string? notes)
        {
            var client = await _db.Clients.FindAsync(id);
            if (client is null) return NotFound();

            client.Notes = notes?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Notes saved.";
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
