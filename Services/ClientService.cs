using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _db;

        public ClientService(ApplicationDbContext db) => _db = db;

        public async Task<Client> GetOrCreateAsync(string fullName, string email, string? phone = null, string? userId = null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var client = await _db.Clients
                .FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail);

            if (client is null)
            {
                client = new Client
                {
                    FullName = fullName.Trim(),
                    Email    = email.Trim(),
                    Phone    = phone?.Trim() ?? string.Empty,
                    UserId   = userId
                };
                _db.Clients.Add(client);
                await _db.SaveChangesAsync();
                return client;
            }

            var changed = false;
            if (string.IsNullOrWhiteSpace(client.Phone) && !string.IsNullOrWhiteSpace(phone))
            {
                client.Phone = phone.Trim();
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(client.UserId) && !string.IsNullOrWhiteSpace(userId))
            {
                client.UserId = userId;
                changed = true;
            }
            if (changed)
                await _db.SaveChangesAsync();

            return client;
        }

        public async Task<Client?> GetByUserIdAsync(string userId) =>
            await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
    }
}
