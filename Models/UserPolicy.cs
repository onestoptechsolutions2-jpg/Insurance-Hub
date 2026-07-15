using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    /// <summary>
    /// A policy the user has registered as currently active with an insurer.
    /// Used for existing-plan inquiry and payment reminder scheduling.
    /// </summary>
    public class UserPolicy
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        [Required, MaxLength(200)]
        public string PolicyName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ProviderName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string InsuranceType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string PolicyNumber { get; set; } = string.Empty;

        public decimal MonthlyPremium { get; set; }

        /// <summary>Commission percentage the broker earns on this policy's monthly premium — entered manually at conversion, since rates vary by deal/provider.</summary>
        public decimal? CommissionRate { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime RenewalDate { get; set; }

        /// <summary>User opted in for renewal reminder emails.</summary>
        public bool RemindersEnabled { get; set; } = true;

        /// <summary>When the last reminder was sent (prevents duplicates).</summary>
        public DateTime? LastReminderSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Client? Client { get; set; }
    }
}
