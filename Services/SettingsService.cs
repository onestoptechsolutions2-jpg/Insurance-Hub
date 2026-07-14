using System.Security.Cryptography;
using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Services
{
    public class SettingsService : ISettingsService
    {
        /// <summary>Shared Data Protection purpose for all app-managed secrets (SMTP password,
        /// SMS API key, lead API key, webhook secrets) — one key ring for all of them.</summary>
        public const string ProtectorPurpose = "AppSettings.Secrets";

        private readonly ApplicationDbContext _db;
        private readonly IDataProtector _protector;

        public SettingsService(ApplicationDbContext db, IDataProtectionProvider dataProtectionProvider)
        {
            _db        = db;
            _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        }

        public async Task<AppSettings> GetAsync()
        {
            if (!await _db.AppSettings.AnyAsync())
            {
                _db.AppSettings.Add(new AppSettings { LeadApiKey = _protector.Protect(GenerateKey()) });
                await _db.SaveChangesAsync();
            }

            // AsNoTracking: this instance's secret fields get decrypted in place below — it must
            // never be a tracked entity, or a later SaveChanges elsewhere in this scope would
            // persist plaintext secrets back to the database.
            var entity = await _db.AppSettings.AsNoTracking().FirstAsync();

            entity.SmtpSenderPassword   = TryUnprotect(entity.SmtpSenderPassword);
            entity.AfricasTalkingApiKey = TryUnprotect(entity.AfricasTalkingApiKey);
            entity.LeadApiKey           = TryUnprotect(entity.LeadApiKey);
            return entity;
        }

        public async Task SaveAsync(AppSettings updated)
        {
            var entity = await _db.AppSettings.FirstOrDefaultAsync();
            if (entity is null)
            {
                entity = new AppSettings { LeadApiKey = _protector.Protect(GenerateKey()) };
                _db.AppSettings.Add(entity);
            }

            entity.BusinessName           = updated.BusinessName;
            entity.AgentContactEmail      = updated.AgentContactEmail;
            entity.CurrencySymbol         = updated.CurrencySymbol;
            entity.SmtpHost                = updated.SmtpHost;
            entity.SmtpPort                = updated.SmtpPort;
            entity.SmtpUseSsl              = updated.SmtpUseSsl;
            entity.SmtpSenderEmail         = updated.SmtpSenderEmail;
            entity.SmtpSenderName          = updated.SmtpSenderName;
            entity.SmtpAgentEmail          = updated.SmtpAgentEmail;
            entity.SmsEnabled              = updated.SmsEnabled;
            entity.AfricasTalkingUsername  = updated.AfricasTalkingUsername;
            entity.AfricasTalkingSenderId  = updated.AfricasTalkingSenderId;

            if (!string.IsNullOrWhiteSpace(updated.SmtpSenderPassword))
                entity.SmtpSenderPassword = _protector.Protect(updated.SmtpSenderPassword);

            if (!string.IsNullOrWhiteSpace(updated.AfricasTalkingApiKey))
                entity.AfricasTalkingApiKey = _protector.Protect(updated.AfricasTalkingApiKey);

            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<string> RegenerateLeadApiKeyAsync()
        {
            var entity = await _db.AppSettings.FirstOrDefaultAsync();
            if (entity is null)
            {
                entity = new AppSettings();
                _db.AppSettings.Add(entity);
            }

            var plainKey = GenerateKey();
            entity.LeadApiKey = _protector.Protect(plainKey);
            entity.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return plainKey;
        }

        private static string GenerateKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        private string TryUnprotect(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            try { return _protector.Unprotect(value); }
            catch { return string.Empty; } // corrupted/foreign ciphertext — fail closed, not throw
        }
    }
}
