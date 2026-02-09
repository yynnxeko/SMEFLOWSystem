using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Config;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _settings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            if (string.IsNullOrWhiteSpace(_settings.FromName))
                throw new InvalidOperationException("Missing config: EmailSettings:FromName");
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException("Missing config: EmailSettings:FromEmail");
            if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
                throw new InvalidOperationException("Missing config: EmailSettings:SmtpServer");
            if (_settings.SmtpPort <= 0)
                throw new InvalidOperationException("Invalid config: EmailSettings:SmtpPort");
            if (string.IsNullOrWhiteSpace(_settings.Username))
                throw new InvalidOperationException("Missing config: EmailSettings:Username");
            if (string.IsNullOrWhiteSpace(_settings.Password))
                throw new InvalidOperationException("Missing config: EmailSettings:Password");

            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body; // Cho phép gửi HTML
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();  // Sửa 'email' thành 'message' để nhất quán
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "SMEFLOW System - Mã OTP của bạn";  // Sửa tiêu đề để phù hợp với hệ thống (thay ShopDemo)

            var body = new TextPart("plain")
            {
                Text = $"Mã OTP của bạn là: {otp}\n" +
                       "Mã này có hiệu lực trong 5 phút.\n" +
                       "Nếu bạn không yêu cầu, vui lòng bỏ qua email này."
            };
            message.Body = body;

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}