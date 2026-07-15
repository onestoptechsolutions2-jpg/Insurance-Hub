using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    public enum TransactionType { CommissionReceived, PremiumCollected }

    /// <summary>
    /// A record of money that has already moved — commission the broker earned from a provider,
    /// or a premium collected from a client. Recorded manually by the admin when it actually
    /// happens (provider payout schedules aren't something this system can observe automatically).
    /// There's no "Pending" status: a Transaction only exists once it's real.
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }

        public int PolicyId { get; set; }
        public UserPolicy? Policy { get; set; }

        /// <summary>Denormalised for easy querying without joining through Policy.</summary>
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public TransactionType Type { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Which month this transaction covers, e.g. "2026-07".</summary>
        [Required, MaxLength(7)]
        public string PeriodMonth { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
