using HMS.Application.Models.Notification;

namespace HMS.Application.Contracts.Services
{
    public interface IEmailService
    {
        Task<SendEmailResponse> Send(string to, string subject, string body);
    }
}
