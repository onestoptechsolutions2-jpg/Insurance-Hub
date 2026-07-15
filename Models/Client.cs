using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    /// <summary>
    /// The real CRM anchor for a person — a prospect, lead, or client. Independent of whether
    /// they've ever created a site login: a site account (User) is optional metadata on a
    /// Client, not a requirement to be tracked. Matched/deduped by email via IClientService.
    /// </summary>
    public class Client
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>Set only if this client also has a site login.</summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<QuoteRequest> Quotes { get; set; } = new List<QuoteRequest>();
        public ICollection<UserPolicy> Policies { get; set; } = new List<UserPolicy>();
    }
}
