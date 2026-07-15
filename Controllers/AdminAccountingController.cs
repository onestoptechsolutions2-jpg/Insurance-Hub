using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/accounting")]
    public class AdminAccountingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminAccountingController(ApplicationDbContext db) => _db = db;

        // GET /admin/accounting?month=2026-07
        [HttpGet("")]
        public async Task<IActionResult> Index(string? month)
        {
            var periodMonth = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;

            var policies = await _db.UserPolicies
                .Include(p => p.Client)
                .OrderBy(p => p.Client!.FullName)
                .ToListAsync();

            var transactionsThisMonth = await _db.Transactions
                .Where(t => t.PeriodMonth == periodMonth)
                .ToListAsync();

            ViewBag.PeriodMonth = periodMonth;
            ViewBag.TransactionsThisMonth = transactionsThisMonth;
            ViewBag.ExpectedCommissionTotal = policies.Sum(p => p.MonthlyPremium * (p.CommissionRate ?? 0) / 100);
            ViewBag.ExpectedPremiumTotal = policies.Sum(p => p.MonthlyPremium);
            ViewBag.ReceivedCommissionTotal = transactionsThisMonth
                .Where(t => t.Type == TransactionType.CommissionReceived).Sum(t => t.Amount);
            ViewBag.CollectedPremiumTotal = transactionsThisMonth
                .Where(t => t.Type == TransactionType.PremiumCollected).Sum(t => t.Amount);

            return View("~/Views/Admin/Accounting.cshtml", policies);
        }

        [HttpPost("record")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordTransaction(
            int policyId, TransactionType type, decimal amount, string periodMonth, string? notes)
        {
            var policy = await _db.UserPolicies.FindAsync(policyId);
            if (policy is null) return NotFound();

            _db.Transactions.Add(new Transaction
            {
                PolicyId    = policy.Id,
                ClientId    = policy.ClientId,
                Type        = type,
                Amount      = amount,
                PeriodMonth = periodMonth,
                Notes       = notes?.Trim() ?? string.Empty
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Transaction recorded.";
            return RedirectToAction(nameof(Index), new { month = periodMonth });
        }

        // GET /admin/accounting/ledger — full history, optionally filtered
        [HttpGet("ledger")]
        public async Task<IActionResult> Ledger(int? clientId, string? month)
        {
            var query = _db.Transactions.Include(t => t.Client).Include(t => t.Policy).AsQueryable();

            if (clientId is int cid) query = query.Where(t => t.ClientId == cid);
            if (!string.IsNullOrWhiteSpace(month)) query = query.Where(t => t.PeriodMonth == month);

            var transactions = await query.OrderByDescending(t => t.RecordedAt).ToListAsync();
            return View("~/Views/Admin/AccountingLedger.cshtml", transactions);
        }
    }
}
