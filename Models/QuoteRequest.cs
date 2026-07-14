using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    public enum QuoteStatus { New, Contacted, Sold, Lost }

    public class QuoteRequest
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string PlanName { get; set; } = string.Empty;

        // Denormalised for history — plan may change later
        public decimal MonthlyPremium { get; set; }
        public string InsuranceType { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>Optional traceability link to the actual plan, alongside the snapshot fields above.</summary>
        public int? PlanId { get; set; }
        public InsurancePlan? Plan { get; set; }

        /// <summary>Where this lead came from, e.g. "landing-page", "whatsapp-bot" — null for on-site quotes.</summary>
        [MaxLength(100)]
        public string? Source { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Optional: link to Identity user if logged in
        public string? UserId { get; set; }

        public QuoteStatus Status { get; set; } = QuoteStatus.New;

        [MaxLength(1000)]
        public string AdminNotes { get; set; } = string.Empty;

        /// <summary>Set once this quote has been converted into a UserPolicy (i.e. the sale was booked).</summary>
        public int? ConvertedPolicyId { get; set; }
        public UserPolicy? ConvertedPolicy { get; set; }
    }
}
