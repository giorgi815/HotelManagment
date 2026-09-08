using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System.Net.Mail;

namespace HMS.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ISmtpClient _smtpClient;
        ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ISmtpClient smtpClient, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _smtpClient = smtpClient;
            _logger = logger;
        }

        public async Task<SendEmailResponse> Send(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Starting to send emau=il to {Recipient}", to);

                ValidateAddressWhereEmailSent(to);
                _logger.LogInformation("Validated recipient email: {Recipient}", to);

                var normalizeSubject = NormalizeSubject(subject);
                _logger.LogInformation("Normalized subject: {Subject}", normalizeSubject);

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:Sender"]));
                email.To.Add(MailboxAddress.Parse(to.Trim()));
                email.Subject = normalizeSubject;
                email.Body = new TextPart(TextFormat.Html) { Text = body };

                _logger.LogInformation("Connecting to SMTP server: {Server}:{Port}", _configuration["EmailSettings:SmtpServer"], _configuration["EmmailSettings:Port"]);

                await _smtpClient.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    int.Parse(_configuration["EmailSettings:Port"]),
                    bool.Parse(_configuration["EmailSettings:UseSsl"])
                    );


                _logger.LogInformation("Authentication with SMTP server...");


                await _smtpClient.AuthenticateAsync(
                    _configuration["EmailSettings:UserName"],
                    _configuration["EmailSettings:Password"]
                    );

                _logger.LogInformation("Sending email to {Recipient}", to);
                await _smtpClient.SendAsync(email);

                _logger.LogInformation("Disconnect from SMTP server...");
                await _smtpClient.DisconnectAsync(true);

                return new SendEmailResponse(true, $"Message sent successfully to: {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}: {Message}", to, ex.Message);
                return new SendEmailResponse(success: false, message: ex.Message, error: ex);
            }
        }

        private string NormalizeSubject(string subject)
        {
            return string.IsNullOrWhiteSpace(subject) ? string.Empty : subject.Trim();
        }

        private void ValidateAddressWhereEmailSent(string to)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new BadRequestException("Email address cannot be empty.");

            try
            {
                var mailAddress = new MailAddress(to);
                if (!mailAddress.Address.Contains("@") || !mailAddress.Address.Contains("."))
                    throw new BadRequestException($"Invalid email address format {to}.");
            }
            catch
            {
                throw new BadRequestException($"Sending email {to} must be a valid email address.");
            }
        }
    }
}
