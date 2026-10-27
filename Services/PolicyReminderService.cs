using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Services
{
    /// <summary>
    /// Background service that runs daily and sends renewal reminder emails
    /// 30, 14, and 7 days before a policy's renewal date.
    /// </summary>
    public class PolicyReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PolicyReminderService> _logger;

        // Run once every 24 hours
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        // If any reminder failed, try again soon instead of waiting a full day.
        // Safe to repeat: policies already reminded today are skipped.
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(15);

        // Postgres advisory lock so only one app instance sends reminders at a time.
        private const long LockKey = 727402;

        public PolicyReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<PolicyReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PolicyReminderService started.");

            // Stagger first run by 1 minute so startup isn't overwhelmed
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var failed = false;
                try
                {
                    failed = await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed = true;
                    _logger.LogError(ex, "Error in PolicyReminderService.");
                }

                await Task.Delay(failed ? RetryInterval : Interval, stoppingToken);
            }
        }

        /// <summary>Returns true if anything failed and the run should be retried soon.</summary>
        private async Task<bool> SendDueRemindersAsync(CancellationToken ct)
        {
            using var scope        = _scopeFactory.CreateScope();
            var db                 = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService       = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var webhooks           = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();
            var smsService         = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var isPostgres = db.Database.ProviderName?.Contains("Npgsql") == true;
            if (isPostgres)
            {
                // The lock belongs to this connection, so keep it open until we unlock.
                await db.Database.OpenConnectionAsync(ct);
                var gotLock = (await db.Database
                    .SqlQuery<bool>($"SELECT pg_try_advisory_lock({LockKey}) AS \"Value\"")
                    .ToListAsync(ct)).FirstOrDefault();
                if (!gotLock)
                {
                    _logger.LogInformation("Another instance is sending reminders; skipping this run.");
                    await db.Database.CloseConnectionAsync();
                    return false;
                }
            }

            try
            {
                return await SendRemindersAsync(db, emailService, webhooks, smsService, ct);
            }
            finally
            {
                if (isPostgres)
                {
                    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(" + LockKey + ")");
                    await db.Database.CloseConnectionAsync();
                }
            }
        }

        private async Task<bool> SendRemindersAsync(
            ApplicationDbContext db, IEmailService emailService, IWebhookDispatcher webhooks,
            ISmsService smsService, CancellationToken ct)
        {
            var anyFailed = false;
            var today = DateTime.UtcNow.Date;

            // Days before renewal at which we send reminders
            var reminderDays = new[] { 30, 14, 7 };

            foreach (var days in reminderDays)
            {
                var targetDate = today.AddDays(days);

                var duePolicies = await db.UserPolicies
                    .Include(p => p.Client)
                    .Where(p =>
                        p.RemindersEnabled &&
                        p.RenewalDate.Date == targetDate &&
                        (p.LastReminderSentAt == null ||
                         p.LastReminderSentAt.Value.Date < today))
                    .ToListAsync(ct);

                foreach (var policy in duePolicies)
                {
                    if (policy.Client?.Email is null) continue;

                    // One policy failing must not stop the others, and each success is
                    // saved straight away so a later crash can't cause a duplicate reminder.
                    try
                    {
                        await emailService.SendPolicyReminderAsync(new PolicyReminderData(
                            ClientName:    policy.Client.FullName.Length > 0
                                               ? policy.Client.FullName
                                               : policy.Client.Email,
                            ClientEmail:   policy.Client.Email,
                            PolicyName:    policy.PolicyName,
                            ProviderName:  policy.ProviderName,
                            InsuranceType: policy.InsuranceType,
                            RenewalDate:   policy.RenewalDate,
                            DaysRemaining: days,
                            MonthlyPremium: policy.MonthlyPremium
                        ));

                        // The email went out: record it now, before the optional extras.
                        policy.LastReminderSentAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);

                        if (!string.IsNullOrWhiteSpace(policy.Client.Phone))
                        {
                            await smsService.SendSmsAsync(
                                policy.Client.Phone,
                                $"Reminder: your {policy.PolicyName} policy with {policy.ProviderName} " +
                                $"renews in {days} days ({policy.RenewalDate:d MMM yyyy}). Reply to discuss options.");
                        }

                        await webhooks.DispatchAsync(WebhookEventType.PolicyRenewalDue, new
                        {
                            policyId       = policy.Id,
                            clientId       = policy.ClientId,
                            clientEmail    = policy.Client.Email,
                            policyName     = policy.PolicyName,
                            providerName   = policy.ProviderName,
                            insuranceType  = policy.InsuranceType,
                            renewalDate    = policy.RenewalDate,
                            daysRemaining  = days,
                            monthlyPremium = policy.MonthlyPremium
                        });

                        _logger.LogInformation(
                            "Reminder sent: {PolicyName} for {Email} ({Days} days)",
                            policy.PolicyName, policy.Client.Email, days);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        anyFailed = true;
                        _logger.LogError(ex, "Reminder failed for policy {PolicyId}; will retry.", policy.Id);
                    }
                }
            }

            return anyFailed;
        }
    }
}
