using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
        public async Task<IActionResult> Ledger(int? clientId, string? month, int? page)
        {
            var query = _db.Transactions.Include(t => t.Client).Include(t => t.Policy).AsQueryable();

            if (clientId is int cid) query = query.Where(t => t.ClientId == cid);
            if (!string.IsNullOrWhiteSpace(month)) query = query.Where(t => t.PeriodMonth == month);

            var pageNo = Paging.Normalize(page);
            var total = await query.CountAsync();
            var transactions = await query.OrderByDescending(t => t.RecordedAt).ThenByDescending(t => t.Id)
                .Skip((pageNo - 1) * Paging.PageSize).Take(Paging.PageSize).ToListAsync();
            ViewBag.Page = pageNo;
            ViewBag.TotalPages = Paging.TotalPages(total);
            ViewBag.Total = total;
            return View("~/Views/Admin/AccountingLedger.cshtml", transactions);
        }

        // GET /admin/accounting/book?month=2026-07 — chat-style cash book: commission & premiums in, expenses out
        [HttpGet("book")]
        public async Task<IActionResult> Book(string? month)
        {
            var periodMonth = ValidMonth(month) ? month! : DateTime.UtcNow.ToString("yyyy-MM");
            var from = DateTime.SpecifyKind(DateTime.ParseExact(periodMonth + "-01", "yyyy-MM-dd", null), DateTimeKind.Utc);
            var to = from.AddMonths(1);

            var transactions = await _db.Transactions
                .Include(t => t.Client)
                .Where(t => t.PeriodMonth == periodMonth)
                .ToListAsync();
            var expenses = await _db.Expenses
                .Where(e => e.SpentAt >= from && e.SpentAt < to)
                .ToListAsync();

            var lines = transactions
                .Select(t => new BookLine(
                    t.RecordedAt, true, t.Amount,
                    t.Type == TransactionType.CommissionReceived ? "Commission" : "Premium collected",
                    $"{t.Client?.FullName} {t.Notes}".Trim(), null))
                .Concat(expenses.Select(e => new BookLine(e.SpentAt, false, e.Amount, e.Category, e.Notes, e.Id)))
                .OrderBy(l => l.Date)
                .ToList();

            ViewBag.PeriodMonth = periodMonth;
            return View("~/Views/Admin/AccountingBook.cshtml", lines);
        }

        // POST /admin/accounting/book/add — one chat-style message becomes one expense
        [HttpPost("book/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(string? text, string? month)
        {
            var parsed = ParseExpense(text);
            if (parsed is null)
            {
                TempData["Error"] = "Include an amount, e.g. \"paid 3500 office rent\".";
                return RedirectToAction(nameof(Book), new { month });
            }

            _db.Expenses.Add(new Expense { Amount = parsed.Value.Amount, Category = parsed.Value.Category, Notes = parsed.Value.Notes });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Book), new { month });
        }

        [HttpPost("book/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int id, string? month)
        {
            var expense = await _db.Expenses.FindAsync(id);
            if (expense is not null)
            {
                _db.Expenses.Remove(expense);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Book), new { month });
        }

        private static bool ValidMonth(string? m) => m is not null && Regex.IsMatch(m, @"^\d{4}-(0[1-9]|1[0-2])$");

        private static readonly (Regex Pattern, string Category)[] CategoryHints =
        {
            (new Regex(@"\b(rent|lease)\b", RegexOptions.IgnoreCase), "Office rent"),
            (new Regex(@"\b(airtime|data|internet|wifi|sms|phone)\b", RegexOptions.IgnoreCase), "Airtime & internet"),
            (new Regex(@"\b(fuel|petrol|transport|matatu|uber|fare)\b", RegexOptions.IgnoreCase), "Transport"),
            (new Regex(@"\b(ads?|advert\w*|facebook|google|marketing|flyers?)\b", RegexOptions.IgnoreCase), "Marketing"),
            (new Regex(@"\b(salary|salaries|wage|wages)\b", RegexOptions.IgnoreCase), "Salaries"),
            (new Regex(@"\b(licen[cs]e|kra|tax|permit|ira)\b", RegexOptions.IgnoreCase), "Licences & tax"),
            (new Regex(@"\b(stationery|printing|paper|office)\b", RegexOptions.IgnoreCase), "Office supplies"),
        };

        /// <summary>"paid 3,500 airtime" or "spent 2k fuel" → amount, category, note. Null when there is no amount.</summary>
        public static (decimal Amount, string Category, string Notes)? ParseExpense(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.Trim();
            var m = Regex.Match(t, @"(\d[\d,]*(?:\.\d+)?)\s*(k)?\b", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            if (!decimal.TryParse(m.Groups[1].Value.Replace(",", ""), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount)) return null;
            if (m.Groups[2].Success) amount *= 1000;
            if (amount <= 0 || amount > 99_999_999) return null;

            var category = CategoryHints.FirstOrDefault(h => h.Pattern.IsMatch(t)).Category ?? "Other";
            var note = Regex.Replace(t.Remove(m.Index, m.Length), @"\s+", " ").Trim();
            if (note.Length > 500) note = note[..500];
            return (Math.Round(amount, 2), category, note.Length == 0 ? t : note);
        }

        public record BookLine(DateTime Date, bool IsIn, decimal Amount, string Category, string Title, int? ExpenseId);
    }
}
