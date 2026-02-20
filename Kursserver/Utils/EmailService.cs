using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Resend;


namespace Kursserver.Utils
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly IResend _resend;

        public EmailService(IConfiguration config, IResend resend)
        {
            _config = config;
            _resend = resend;
        }

        public async Task ResendEmailAsync(string toEmail, int passcode)
        {
            await _resend.EmailSendAsync(new EmailMessage()
            {
                From = "onboarding@resend.dev",
                //To = toEmail,
                To = "adam_folke@yahoo.se",
                Subject = "Din lösenkod",
                HtmlBody = $"<p>Din lösenkod är: <strong>{passcode}</strong></p>",
            });
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply@culprogrammering.se", _config["Smtp:Username"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
