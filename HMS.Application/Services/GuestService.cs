using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Guest;
using HMS.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HMS.Application.Services
{
    public class GuestService : IGuestService
    {

        private readonly IGuestRepository _guestRepository;
        private readonly IReservationRepository _reservationRepositoy;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public GuestService(
            IGuestRepository guestRepository,
            IReservationRepository reservationRepository,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _guestRepository = guestRepository;
            _reservationRepositoy = reservationRepository;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<GuestForGettingDto> CreateGuestAsync(GuestForCreatingDto model)
        {
            if (model is null)
                throw new BadRequestException("Model is requred");

            if (string.IsNullOrWhiteSpace(model.Email) || !EmailRegex.IsMatch(model.Email))
                throw new BadRequestException("Invalid Email format");

            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || !PhoneRegex.IsMatch(model.PhoneNumber))
                throw new BadRequestException("Invalid phone number format. It must contain exactly 9 digits");

            if (string.IsNullOrWhiteSpace(model.PersonalNumber) || !PersonalNumberRegex.IsMatch(model.PersonalNumber))
                throw new BadRequestException("Invalid personal number format. It must contain exactly 11 digits");

            var personalNumberTaken = await _guestRepository.ExistsAsync(
            g => g.PersonalNumber == model.PersonalNumber);

            if (personalNumberTaken)
                throw new BadRequestException($"Personal number {model.PersonalNumber} is already in use");

            var phoneNumberTaken = await _guestRepository.ExistsAsync(
            g => g.PhoneNumber == model.PhoneNumber);

            if (phoneNumberTaken)
                throw new BadRequestException($"Phone number {model.PhoneNumber} is already in use");

            var emailTaken = await _guestRepository.ExistsAsync(
                g => g.Email == model.Email);

            if (emailTaken)
                throw new BadRequestException($"Email {model.Email} is already in use");

            var guest = _mapper.Map<Guest>(model);


            await _guestRepository.AddAsync(guest);
            await _guestRepository.SaveAsync();

            return _mapper.Map<GuestForGettingDto>(guest);
        }

        public async Task<int> DeleteGuestAsync(int id)
        {
            var guest = await _guestRepository.GetAsync(g => g.GuestId == id, include: query => query.Include(x => x.ApplicationUserId));

            if (guest == null)
                throw new BadRequestException("Id not found");

            var hasActiveReservatios = await _reservationRepositoy.ExistsAsync(g => g.GuestId == id && g.CheckOutDate > DateTime.UtcNow);

            if (hasActiveReservatios)
                throw new BadRequestException("Guest can't be deleted while it has active or future reservation");

            var applicationUser = guest.ApplicationUser;

            _guestRepository.Remove(guest);

            if (applicationUser != null)
            {
                var result = await _userManager.DeleteAsync(applicationUser);

                if (!result.Succeeded)
                {
                    throw new BadRequestException(
                        result.Errors.First().Description);
                }
            }

            await _guestRepository.SaveAsync();

            return id;
        }

        public async Task<GuestForGettingDto> UpdateGuestAsync(GuestForUpdatingDto model)
        {
            if (model == null)
                throw new BadRequestException("Model is empty");

            var guest = await _guestRepository.GetAsync(g => g.GuestId == model.GuestId);

            if (guest == null)
                throw new BadRequestException("Guest can't be found");

            if (string.IsNullOrWhiteSpace(model.Email) || !EmailRegex.IsMatch(model.Email))
                throw new BadRequestException("Invalid Email format");

            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || !PhoneRegex.IsMatch(model.PhoneNumber))
                throw new BadRequestException("Invalid phone number format. It must contain exactly 9 digits");

            if (string.IsNullOrWhiteSpace(model.PersonalNumber) || !PersonalNumberRegex.IsMatch(model.PersonalNumber))
                throw new BadRequestException("Invalid personal number format. It must contain exactly 11 digits");

            var emailTaken = await _guestRepository.ExistsAsync(
                g => g.Email == model.Email && g.GuestId != model.GuestId);

            if (emailTaken)
                throw new BadRequestException($"Email {model.Email} is already in use");

            var personalNumberTaken = await _guestRepository.ExistsAsync(
                g => g.PersonalNumber == model.PersonalNumber && g.GuestId != model.GuestId);

            if (personalNumberTaken)
                throw new BadRequestException($"Personal number {model.PersonalNumber} is already in use");

            var phoneNumberTaken = await _guestRepository.ExistsAsync(
                g => g.PhoneNumber == model.PhoneNumber && g.GuestId != model.GuestId);

            if (phoneNumberTaken)
                throw new BadRequestException($"Phone number {model.PhoneNumber} is already in use");

            _mapper.Map(model, guest);
            _guestRepository.Update(guest);
            await _guestRepository.SaveAsync();

            return _mapper.Map<GuestForGettingDto>(guest);
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
