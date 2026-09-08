using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Admin;
using HMS.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;

namespace HMS.Application.Services
{
    public class AdminService : IAdminService
    {

        private readonly IAdminRepository _adminRepository;
        private readonly IMapper _mppper;

        public AdminService(IAdminRepository adminRepository, IMapper mapper)
        {
            _adminRepository = adminRepository;
            _mppper = mapper;
        }

        public async Task<int> CreateAdminAsync(AdminForCreatingDto model)
        {
            if (model is null)
                throw new BadRequestException("Model is required");

            if (string.IsNullOrEmpty(model.FirstName))
                throw new BadRequestException("First name is required");

            if (model.FirstName.Length < 2 || model.FirstName.Length > 100)
                throw new BadRequestException("First name must be between 2 and 100 characters");

            if (string.IsNullOrEmpty(model.LastName))
                throw new BadRequestException("Last name is required");

            if (model.LastName.Length < 2 || model.LastName.Length > 100)
                throw new BadRequestException("Last name must be between 2 and 100 characters");

            if (string.IsNullOrEmpty(model.PersonalNumber))
                throw new BadRequestException("Personal number is required");

            if (!PersonalNumberRegex.IsMatch(model.PersonalNumber))
                throw new BadRequestException("Personal number must contain exactly 11 digits");

            if (string.IsNullOrEmpty(model.Email))
                throw new BadRequestException("Email is required");

            if (!EmailRegex.IsMatch(model.Email))
                throw new BadRequestException("Invalid email format");

            if (string.IsNullOrEmpty(model.PhoneNumber))
                throw new BadRequestException("Phone number is required");

            if (!PhoneRegex.IsMatch(model.PhoneNumber))
                throw new BadRequestException("Phone number must contain exactly 9 digits");

            var personalNumberTaken = await _adminRepository.ExistsAsync(a => a.PersonalNumber == model.PersonalNumber);
            if (personalNumberTaken)
                throw new BadRequestException($"Personal number {model.PersonalNumber} is already in use");

            var emailTaken = await _adminRepository.ExistsAsync(a => a.Email == model.Email);
            if (emailTaken)
                throw new BadRequestException($"Email {model.Email} is already in use");

            var phoneNumberTaken = await _adminRepository.ExistsAsync(a => a.PhoneNumber == model.PhoneNumber);
            if (phoneNumberTaken)
                throw new BadRequestException($"Phone number {model.PhoneNumber} is already in use");

            var admin = _mppper.Map<Admin>(model);

            await _adminRepository.AddAsync(admin);
            await _adminRepository.SaveAsync();

            return admin.AdminId;
        }


        #region
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        private static readonly Regex PhoneRegex = new(
            @"^\d{9}$",
            RegexOptions.Compiled);

        private static readonly Regex PersonalNumberRegex = new(
            @"^\d{11}$",
            RegexOptions.Compiled);
        #endregion
    }
}
