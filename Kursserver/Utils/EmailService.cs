using Resend;


namespace Kursserver.Utils
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly IResend _resend;
        private readonly bool _isDevelopment;

        public EmailService(IConfiguration config, IResend resend, IWebHostEnvironment env)
        {
            _config = config;
            _resend = resend;
            _isDevelopment = env.IsDevelopment();
        }

        public async Task ResendEmailAsync(string toEmail, int passcode)
        {
            await _resend.EmailSendAsync(new EmailMessage()
            {
                From = "noreply@culprogrammering.net",
                To = toEmail,
                // To = "adam_folke@yahoo.se",
                Subject = "Din lösenkod",
                HtmlBody = $"<p>Din lösenkod är: <strong>{passcode}</strong></p>",
            });
        }

        public void SendEmailFireAndForget(string toEmail, string subject, string body)
        {
            if (_isDevelopment) return;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _resend.EmailSendAsync(new EmailMessage()
                    {
                        From = "noreply@culprogrammering.net",
                        To = toEmail,
                        Subject = subject,
                        HtmlBody = $"<p style=\"color:#c00;font-weight:bold;\">OBS! Detta är ett automatiskt emailutskick. Du kan inte svara på det.</p><hr/><p>{body}</p>",
                    });
                }
                catch { /* silently ignore */ }
            });
        }
    }
}
