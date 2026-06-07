namespace Insurance_Hub.Services
{
    public interface IEmailService
    {
        Task SendQuoteRequestToAgentAsync(QuoteEmailData data);
        Task SendQuoteConfirmationToClientAsync(QuoteEmailData data);
        Task SendPolicyReminderAsync(PolicyReminderData data);
    }

    public record QuoteEmailData(
        string ClientName,
        string ClientEmail,
        string PlanName,
        string PlanTier,
        string ProviderName,
        string InsuranceType,
        decimal MonthlyPremium,
        decimal CoverageLimit,
        string Features,
        DateTime RequestedAt
    );

    public record PolicyReminderData(
        string ClientName,
        string ClientEmail,
        string PolicyName,
        string ProviderName,
        string InsuranceType,
        DateTime RenewalDate,
        int DaysRemaining,
        decimal MonthlyPremium
    );
}
