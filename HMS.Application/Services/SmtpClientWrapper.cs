using HMS.Application.Contracts.Services;
using MimeKit;

namespace HMS.Application.Services
{
    public class SmtpClientWrapper : ISmtpClient
    {
        private readonly MailKit.Net.Smtp.SmtpClient _client = new();

        public async Task AuthenticateAsync(string userName, string password) => await _client.AuthenticateAsync(userName, password);
        public async Task ConnectAsync(string host, int port, bool useSsl) => await _client.ConnectAsync(host, port, useSsl);        
        public async Task DisconnectAsync(bool quit) => await _client.DisconnectAsync(quit);
        public void Dispose() => _client.Dispose();
        public async Task SendAsync(MimeMessage message) => await _client.SendAsync(message);
    }
}
