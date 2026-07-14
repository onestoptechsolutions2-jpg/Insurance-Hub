namespace Insurance_Hub.Services
{
    public interface ISmsService
    {
        /// <summary>
        /// Sends an SMS via the configured gateway. No-ops (with a logged warning) if SMS isn't
        /// enabled or configured — same graceful-degrade convention as IEmailService.
        /// </summary>
        Task SendSmsAsync(string toPhoneNumber, string message);
    }
}
