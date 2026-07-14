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
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PolicyReminderService.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken ct)
        {
            using var scope        = _scopeFactory.CreateScope();
            var db                 = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService       = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var webhooks           = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();
            var smsService         = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var today = DateTime.UtcNow.Date;

            // Days before renewal at which we send reminders
            var reminderDays = new[] { 30, 14, 7 };

            foreach (var days in reminderDays)
            {
                var targetDate = today.AddDays(days);

                var duePolicies = await db.UserPolicies
                    .Include(p => p.User)
                    .Where(p =>
                        p.RemindersEnabled &&
                        p.RenewalDate.Date == targetDate &&
                        (p.LastReminderSentAt == null ||
                         p.LastReminderSentAt.Value.Date < today))
                    .ToListAsync(ct);

                foreach (var policy in duePolicies)
                {
                    if (policy.User?.Email is null) continue;

                    await emailService.SendPolicyReminderAsync(new PolicyReminderData(
                        ClientName:    policy.User.DisplayName.Length > 0
                                           ? policy.User.DisplayName
                                           : policy.User.Email,
                        ClientEmail:   policy.User.Email,
                        PolicyName:    policy.PolicyName,
                        ProviderName:  policy.ProviderName,
                        InsuranceType: policy.InsuranceType,
                        RenewalDate:   policy.RenewalDate,
                        DaysRemaining: days,
                        MonthlyPremium: policy.MonthlyPremium
                    ));

                    if (!string.IsNullOrWhiteSpace(policy.User.PhoneNumber))
                    {
                        await smsService.SendSmsAsync(
                            policy.User.PhoneNumber,
                            $"Reminder: your {policy.PolicyName} policy with {policy.ProviderName} " +
                            $"renews in {days} days ({policy.RenewalDate:d MMM yyyy}). Reply to discuss options.");
                    }

                    await webhooks.DispatchAsync(WebhookEventType.PolicyRenewalDue, new
                    {
                        policyId       = policy.Id,
                        userId         = policy.UserId,
                        clientEmail    = policy.User.Email,
                        policyName     = policy.PolicyName,
                        providerName   = policy.ProviderName,
                        insuranceType  = policy.InsuranceType,
                        renewalDate    = policy.RenewalDate,
                        daysRemaining  = days,
                        monthlyPremium = policy.MonthlyPremium
                    });

                    policy.LastReminderSentAt = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Reminder sent: {PolicyName} for {Email} ({Days} days)",
                        policy.PolicyName, policy.User.Email, days);
                }

                if (duePolicies.Count > 0)
                    await db.SaveChangesAsync(ct);
            }
        }
    }
}
