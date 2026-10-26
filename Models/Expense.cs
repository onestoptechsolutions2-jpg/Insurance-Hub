using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    /// <summary>
    /// Money the brokerage spent running the business (rent, airtime, transport, licences...).
    /// Entered from the chat-style cash book. Commission and premiums stay in <see cref="Transaction"/>.
    /// </summary>
    public class Expense
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; } = "Other";

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime SpentAt { get; set; } = DateTime.UtcNow;

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
