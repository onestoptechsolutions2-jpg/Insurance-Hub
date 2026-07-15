using Insurance_Hub.Models;

namespace Insurance_Hub.Services
{
    /// <summary>The single reuse point for "who is this person" — resolves or creates the Client
    /// a quote/policy/reminder should attach to, regardless of whether they have a site login.</summary>
    public interface IClientService
    {
        /// <summary>
        /// Matches an existing client by email (case-insensitive); backfills phone/userId onto
        /// an existing match if newly known and not already set; creates one if no match exists.
        /// </summary>
        Task<Client> GetOrCreateAsync(string fullName, string email, string? phone = null, string? userId = null);

        Task<Client?> GetByUserIdAsync(string userId);
    }
}
