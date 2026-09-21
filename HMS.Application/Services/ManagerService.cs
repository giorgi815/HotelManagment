using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Manager;
using HMS.Domain.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HMS.Application.Services
{
    public class ManagerService : IManagerService
    {

        private readonly IManagerRepository _managerRepoitory;
        private readonly IHotelService _hotelService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public ManagerService(
            IManagerRepository managerRepository,
            IHotelService hotelService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _managerRepoitory = managerRepository;
            _hotelService = hotelService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<int> CreateManagerAsync(ManagerForCreatingDto model)
        {
            if (model == null)
                throw new BadRequestException("Model is required");

            var hotel = await _hotelService.GetHotelByIdAsync(model.HotelId);

            if (hotel is null)
                throw new BadRequestException("Hotel wasn't found");

            if (model.HotelId <= 0)
                throw new BadRequestException("Hotel id is required");

            if (string.IsNullOrWhiteSpace(model.Email) || !EmailRegex.IsMatch(model.Email))
                throw new BadRequestException("Invalid email format");

            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || !PhoneRegex.IsMatch(model.PhoneNumber))
                throw new BadRequestException("Invalid phone number format. It must contain exactly 9 digits");

            if (string.IsNullOrWhiteSpace(model.PersonalNumber) || !PersonalNumberRegex.IsMatch(model.PersonalNumber))
                throw new BadRequestException("Invalid personal number format. It must contain exactly 11 digits");

            var personalNumberTaken = await _managerRepoitory.ExistsAsync(m => m.PersonalNumber == model.PersonalNumber);
            if (personalNumberTaken)
                throw new BadRequestException($"Personal number {model.PersonalNumber} is already in use");

            var emailTaken = await _managerRepoitory.ExistsAsync(m => m.Email == model.Email);
            if (emailTaken)
                throw new BadRequestException($"Email {model.Email} is already in use");

            var phoneNumberTaken = await _managerRepoitory.ExistsAsync(m => m.PhoneNumber == model.PhoneNumber);
            if (phoneNumberTaken)
                throw new BadRequestException($"Phone number {model.PhoneNumber} is already in use");

            var manager = _mapper.Map<Manager>(model);

            await _managerRepoitory.AddAsync(manager);
            await _managerRepoitory.SaveAsync();

            return manager.HotelId;
        }

        public async Task<int> DeleteManagerAsync(int id)
        {
            if (id <= 0)
            {
                throw new BadRequestException(
                    "Manager id must be greater than zero");
            }

            var manager =
                await _managerRepoitory.GetAsync(
                    fillter: m => m.ManagerId == id,
                    tracking: true,
                    include: query =>
                        query.Include(
                            m => m.ApplicationUser));

            if (manager is null)
            {
                throw new BadRequestException(
                    "Manager not found");
            }

            var hasAnotherManager =
                await _managerRepoitory.ExistsAsync(
                    m =>
                        m.HotelId == manager.HotelId
                        &&
                        m.ManagerId != manager.ManagerId);

            if (!hasAnotherManager)
            {
                throw new BadRequestException(
                    "Manager cannot be deleted because " +
                    "the hotel must have at least one manager");
            }

            var applicationUser =
                manager.ApplicationUser;

            if (applicationUser is null)
            {
                throw new BadRequestException(
                    "Manager application user was not found");
            }

            var result =
                await _userManager.DeleteAsync(
                    applicationUser);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    result.Errors.FirstOrDefault()?.Description
                    ?? "Failed to delete manager");
            }

            return id;
        }

        public async Task<IEnumerable<ManagerForGettingDto>> GetManagerAsync()
        {
            var manager = await _managerRepoitory.GetAllAsync();

            return _mapper.Map<IEnumerable<ManagerForGettingDto>>(manager.Items);

        }

        public async Task<int> UpdateManagerAsync(ManagerForUpdatingDto model)
        {
            if (model is null)
                throw new BadRequestException("Manager id not found");

            var manager = await _managerRepoitory.GetAsync(m => m.ManagerId == model.Id);

            if (manager is null)
                throw new BadRequestException("Manager not found");

            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || !PhoneRegex.IsMatch(model.PhoneNumber))
                throw new BadRequestException("Invalid phone number format. It must contain exactly 9 digits");

            if (string.IsNullOrWhiteSpace(model.PersonalNumber) || !PersonalNumberRegex.IsMatch(model.PersonalNumber))
                throw new BadRequestException("Invalid personal number format. It must contain exactly 11 digits");

            var personalNumberTaken = await _managerRepoitory.ExistsAsync(
                m => m.PersonalNumber == model.PersonalNumber && m.ManagerId != model.Id);
            if (personalNumberTaken)
                throw new BadRequestException($"Personal number {model.PersonalNumber} is already in use");

            var phoneNumberTaken = await _managerRepoitory.ExistsAsync(
                m => m.PhoneNumber == model.PhoneNumber && m.ManagerId != model.Id);
            if (phoneNumberTaken)
                throw new BadRequestException($"Phone number {model.PhoneNumber} is already in use");


            _mapper.Map(model, manager);
            _managerRepoitory.Update(manager);
            await _managerRepoitory.SaveAsync();
            return manager.HotelId;
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
