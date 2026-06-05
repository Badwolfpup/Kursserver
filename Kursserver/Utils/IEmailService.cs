namespace Kursserver.Utils
{
    /// <summary>
    /// Abstraction over email sending so endpoints/notifiers can be tested with a
    /// capturing fake instead of hitting Resend. The production implementation is
    /// <see cref="EmailService"/>.
    /// </summary>
    public interface IEmailService
    {
        Task ResendEmailAsync(string toEmail, int passcode);
        void SendEmailFireAndForget(string toEmail, string subject, string body);
    }
}
