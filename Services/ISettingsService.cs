using Insurance_Hub.Models;

namespace Insurance_Hub.Services
{
    /// <summary>
    /// Reads/writes the single AppSettings row for this broker's instance. Secret fields are
    /// transparently decrypted on read and re-encrypted on write — callers always see plaintext.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>Gets the settings row, creating a blank one on first call if it doesn't exist yet.</summary>
        Task<AppSettings> GetAsync();

        /// <summary>
        /// Saves branding/SMTP/SMS fields. Secret fields (SmtpSenderPassword, AfricasTalkingApiKey)
        /// are only overwritten when non-blank, so a masked form field left empty means "keep existing."
        /// </summary>
        Task SaveAsync(AppSettings updated);

        /// <summary>Generates and persists a new inbound-lead API key, returning it in plaintext once.</summary>
        Task<string> RegenerateLeadApiKeyAsync();
    }
}
