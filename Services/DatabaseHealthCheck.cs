using Insurance_Hub.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Insurance_Hub.Services
{
    /// <summary>/health reports unhealthy when the database can't be reached, not just when the process is up.</summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _db;

        public DatabaseHealthCheck(ApplicationDbContext db) => _db = db;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Database unreachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed", ex);
            }
        }
    }
}
