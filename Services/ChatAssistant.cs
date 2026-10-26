using System.Globalization;
using System.Text.RegularExpressions;
using Insurance_Hub.Controllers;
using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Services
{
    /// <summary>A tap-to-confirm choice. Type is "COMMISSION", "PREMIUM", "EXPENSE" or "CLIENT"; re-validated when tapped.</summary>
    public class ChatChoice
    {
        public string Type { get; set; } = "";
        public string Label { get; set; } = "";
        public int PolicyId { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";
        public string Note { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class ChatLink
    {
        public string Label { get; set; } = "";
        public string Href { get; set; } = "";
    }

    public class ChatPayload
    {
        public List<ChatChoice> Choices { get; set; } = new();
        public List<ChatLink> Links { get; set; } = new();
    }

    public class ChatReply
    {
        public string Text { get; set; } = "";
        public ChatPayload? Payload { get; set; }
    }

    /// <summary>
    /// Turns a typed sentence ("Jubilee commission 12000", "new client Jane jane@x.com 0712345678",
    /// "paid 3500 airtime") into a reply with tap-to-confirm choices, and carries out the choice that is tapped.
    /// Keyword based — no external service.
    /// </summary>
    public class ChatAssistant
    {
        private readonly ApplicationDbContext _db;
        private readonly IClientService _clients;

        public ChatAssistant(ApplicationDbContext db, IClientService clients)
        {
            _db = db;
            _clients = clients;
        }

        public const string Help =
            "Just tell me what happened — I'll set it up and you tap Confirm.\n\n" +
            "• \"New client Jane Wanjiru jane@mail.com 0712345678\"\n" +
            "• \"Jubilee commission 12000\"\n" +
            "• \"Jane paid premium 4500\"\n" +
            "• \"Paid 3500 airtime\"  (an expense)\n" +
            "• \"Renewals coming up\"\n" +
            "• \"How are we doing this month?\"";

        private static string Kes(decimal n) => "KES " + n.ToString("N0", CultureInfo.InvariantCulture);
        private static string Norm(string s) => Regex.Replace(Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9 ]", " "), @"\s+", " ").Trim();

        private static bool Mentions(string text, string name)
        {
            var t = " " + Norm(text) + " ";
            var full = Norm(name);
            if (full.Length > 0 && t.Contains(" " + full + " ")) return true;
            return full.Split(' ').Any(w => w.Length >= 3 && t.Contains(" " + w + " "));
        }

        public async Task<ChatReply> InterpretAsync(string text)
        {
            text = text.Trim();
            var lower = text.ToLowerInvariant();

            if (Regex.IsMatch(lower, @"^(help|hi|hello|hey|menu|\?)$")) return new ChatReply { Text = Help };

            // New client: needs an email (clients are matched by email).
            if (Regex.IsMatch(lower, @"^\s*(add|new|create|register)\s+(a\s+)?(new\s+)?client\b"))
            {
                var email = Regex.Match(text, @"[^\s@]+@[^\s@]+\.[^\s@]+").Value;
                var phone = Regex.Match(text, @"(?:\+?254|0)\d{9}\b").Value;
                var name = Regex.Replace(text, @"^\s*(add|new|create|register)\s+(a\s+)?(new\s+)?client\b", "", RegexOptions.IgnoreCase);
                if (email.Length > 0) name = name.Replace(email, "");
                if (phone.Length > 0) name = name.Replace(phone, "");
                name = Regex.Replace(name, @"\s+", " ").Trim();
                if (name.Length < 2) return new ChatReply { Text = "What is the client's name? e.g. \"New client Jane Wanjiru jane@mail.com 0712345678\"" };
                if (email.Length == 0) return new ChatReply { Text = $"What is {name}'s email? Clients are matched by email, so I need it." };
                return Offer($"Add {name} ({email}) as a client?", new ChatChoice { Type = "CLIENT", Label = $"Add {name}", Name = name, Email = email, Phone = phone });
            }

            if (Regex.IsMatch(lower, @"renewal|renew|expir|due soon"))
            {
                var today = DateTime.UtcNow.Date;
                var due = await _db.UserPolicies.Include(p => p.Client)
                    .Where(p => p.RenewalDate >= today && p.RenewalDate <= today.AddDays(30))
                    .OrderBy(p => p.RenewalDate).Take(15).ToListAsync();
                if (due.Count == 0) return new ChatReply { Text = "No renewals in the next 30 days." };
                var links = due
                    .Where(p => !string.IsNullOrWhiteSpace(p.Client?.Phone))
                    .Select(p => new ChatLink
                    {
                        Label = $"WhatsApp {p.Client!.FullName}",
                        Href = "https://wa.me/" + ToWaNumber(p.Client.Phone) + "?text=" + Uri.EscapeDataString(
                            $"Hello {p.Client.FullName.Split(' ')[0]}, your {p.PolicyName} policy renews on {p.RenewalDate:dd MMM yyyy}. Shall we renew it for you?")
                    }).ToList();
                return new ChatReply
                {
                    Text = $"{due.Count} renewing in the next 30 days:\n" +
                           string.Join("\n", due.Select(p => $"• {p.Client?.FullName} · {p.PolicyName} · {p.RenewalDate:dd MMM}")),
                    Payload = links.Count > 0 ? new ChatPayload { Links = links } : null
                };
            }

            var parsed = AdminAccountingController.ParseExpense(text);

            if (Regex.IsMatch(lower, @"summary|how (are|am) (we|i)|this month|profit|balance|totals?") && parsed is null)
            {
                var month = DateTime.UtcNow.ToString("yyyy-MM");
                var from = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var tx = await _db.Transactions.Where(t => t.PeriodMonth == month).ToListAsync();
                var commission = tx.Where(t => t.Type == TransactionType.CommissionReceived).Sum(t => t.Amount);
                var premiums = tx.Where(t => t.Type == TransactionType.PremiumCollected).Sum(t => t.Amount);
                var spent = await _db.Expenses.Where(e => e.SpentAt >= from && e.SpentAt < from.AddMonths(1)).SumAsync(e => e.Amount);
                var profit = commission - spent;
                return new ChatReply
                {
                    Text = $"This month so far:\n• Commission received {Kes(commission)}\n• Premiums collected {Kes(premiums)}\n• Expenses {Kes(spent)}\n• {(profit >= 0 ? "Profit" : "Loss")} {Kes(Math.Abs(profit))} (commission minus expenses)",
                    Payload = new ChatPayload { Links = { new ChatLink { Label = "Open cash book", Href = "/admin/accounting/book" } } }
                };
            }

            if (parsed is null) return new ChatReply { Text = "I didn't catch that.\n\n" + Help };
            var (amount, category, note) = parsed.Value;

            var policies = await _db.UserPolicies.Include(p => p.Client).ToListAsync();

            // "Jubilee commission 12000" / "commission from Jane 800"
            if (lower.Contains("commission"))
            {
                var hits = policies.Where(p => Mentions(text, p.ProviderName) || (p.Client is not null && Mentions(text, p.Client.FullName))).Take(4).ToList();
                if (hits.Count == 0) return new ChatReply { Text = "Which provider or client was the commission for? Include a name, e.g. \"Jubilee commission 12000\"." };
                return Offer(hits.Count == 1 ? $"Record {Kes(amount)} commission received?" : "Which policy?",
                    hits.Select(p => new ChatChoice { Type = "COMMISSION", PolicyId = p.Id, Amount = amount, Label = $"{p.Client?.FullName} · {p.ProviderName} {p.PolicyName} · {Kes(amount)}" }).ToArray());
            }

            // "Jane paid premium 4500"
            var byClient = policies.Where(p => p.Client is not null && Mentions(text, p.Client.FullName)).Take(4).ToList();
            if (byClient.Count > 0 && Regex.IsMatch(lower, @"premium|paid|pays|payment"))
            {
                return Offer(byClient.Count == 1 ? $"Record {Kes(amount)} premium collected?" : "Which policy?",
                    byClient.Select(p => new ChatChoice { Type = "PREMIUM", PolicyId = p.Id, Amount = amount, Label = $"{p.Client?.FullName} · {p.ProviderName} {p.PolicyName} · {Kes(amount)}" }).ToArray());
            }

            return Offer($"Expense: {Kes(amount)} ({category}). Add to your cash book?",
                new ChatChoice { Type = "EXPENSE", Label = $"Add expense · {category}", Amount = amount, Category = category, Note = note });
        }

        private static ChatReply Offer(string text, params ChatChoice[] choices) =>
            new() { Text = text, Payload = new ChatPayload { Choices = choices.ToList() } };

        private static string ToWaNumber(string raw)
        {
            var d = new string(raw.Where(char.IsDigit).ToArray());
            if (d.StartsWith("0")) d = "254" + d[1..];
            return d;
        }

        /// <summary>Carry out a tapped choice. Re-checks everything against the database.</summary>
        public async Task<string> ExecuteAsync(ChatChoice c)
        {
            switch (c.Type)
            {
                case "CLIENT":
                    if (string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Email)) throw new InvalidOperationException("name or email missing");
                    var client = await _clients.GetOrCreateAsync(c.Name, c.Email, string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone);
                    return $"✓ {client.FullName} is in your clients. Open /admin/clients/{client.Id} to add a policy.";

                case "COMMISSION":
                case "PREMIUM":
                    if (c.Amount <= 0) throw new InvalidOperationException("invalid amount");
                    var policy = await _db.UserPolicies.FindAsync(c.PolicyId) ?? throw new InvalidOperationException("policy not found");
                    _db.Transactions.Add(new Transaction
                    {
                        PolicyId = policy.Id,
                        ClientId = policy.ClientId,
                        Type = c.Type == "COMMISSION" ? TransactionType.CommissionReceived : TransactionType.PremiumCollected,
                        Amount = c.Amount,
                        PeriodMonth = DateTime.UtcNow.ToString("yyyy-MM"),
                        Notes = "Recorded from chat"
                    });
                    await _db.SaveChangesAsync();
                    return $"✓ Recorded {Kes(c.Amount)} {(c.Type == "COMMISSION" ? "commission received" : "premium collected")}.";

                case "EXPENSE":
                    if (c.Amount <= 0) throw new InvalidOperationException("invalid amount");
                    _db.Expenses.Add(new Expense { Amount = c.Amount, Category = string.IsNullOrWhiteSpace(c.Category) ? "Other" : c.Category, Notes = c.Note ?? "" });
                    await _db.SaveChangesAsync();
                    return $"✓ Added expense {Kes(c.Amount)} ({c.Category}).";

                default:
                    throw new InvalidOperationException("unknown action");
            }
        }
    }
}
