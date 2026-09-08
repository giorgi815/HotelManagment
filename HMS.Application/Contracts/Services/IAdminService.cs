using HMS.Application.Models.Admin;
using HMS.Domain.Entities;

namespace HMS.Application.Contracts.Services
{
    public interface IAdminService
    {
        Task<int> CreateAdminAsync(AdminForCreatingDto model);
    }
}
