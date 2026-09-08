using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Admin;
using HMS.Application.Models.Auth;
using HMS.Application.Models.Guest;
using HMS.Application.Models.Manager;
using HMS.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace HMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IManagerService _managerService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IAdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly IGuestService _guestService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        private const string _adminRole = "Admin";
        private const string _managerRole = "Manager";
        private const string _guestRole = "Guest";


        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IManagerService managerService,
            IRefreshTokenRepository refreshTokenRepository,
            IEmailService emailService,
            IGuestService guestService,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper,
            IConfiguration configuration,
            IAdminService adminService
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _managerService = managerService;
            _guestService = guestService;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
            _configuration = configuration;
            _emailService = emailService;
            _adminService = adminService;
        }




        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
                throw new NotFoundException("User not found.");

            if (!user.EmailConfirmed)
                throw new BadRequestException("Email is not confirmed.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new BadRequestException("Your account is locked.");

            bool isValid = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!isValid)
            {
                await _userManager.AccessFailedAsync(user);
                throw new BadRequestException("Username or Password is Incorrect!");
            }
            else
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user);


            return await GenerateTokenPairAsync(user, roles);
            
        }

        public async Task<string> RegisterAdminAsync(AdminRegistrationRequestDto model)
        {

            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                throw new BadRequestException(result.Errors.First().Description);
            }

            try
            {
                await AddRoleAsync(user, _adminRole);

                var admin = new AdminForCreatingDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PersonalNumber = model.PersonalNumber,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    ApplicationUserId = user.Id
                };

                await _adminService.CreateAdminAsync(admin);
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            
            await SendConfirmationEmailAsync(user);

            return user.Id;


        }

        public async Task<string> RegisterGuestAsync(GuestRegistrationRequestDto model)
        {
            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                throw new BadRequestException(result.Errors.First().Description);
            }

            await AddRoleAsync(user, _guestRole);

            var guest = new GuestForCreatingDto
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PersonalNumber = model.PersonalNumber,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                ApplicationUserId = user.Id
            };

            await _guestService.CreateGuestAsync(guest);

            await SendConfirmationEmailAsync(user);

            return user.Id;

        }

        public async Task<string> RegisterManagerAsync(ManagerRegistrationRequestDto model)
        {
            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                throw new BadRequestException(result.Errors.First().Description);
            }
            try
            {
                var manager = new ManagerForCreatingDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PersonalNumber = model.PersonalNumber,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    ApplicationUserId = user.Id,
                    HotelId = model.HotelId
                };

                await _managerService.CreateManagerAsync(manager);

                await AddRoleAsync(user, _managerRole);
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            await SendConfirmationEmailAsync(user);

            return user.Id;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var existing = await _refreshTokenRepository.GetAsync(x => x.Token == refreshToken);

            if (existing == null)
                throw new BadRequestException("Invalid refresh token");

            if (!existing.IsActive)
                throw new BadRequestException("Token is already inactive");

            existing.RevokedAt = DateTimeOffset.Now;
            await _refreshTokenRepository.SaveAsync();
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // Load token with its user in one query
            var existing = await _refreshTokenRepository.GetAsync(
                x => x.Token == refreshToken,
                include: q => q.Include(x => x.User));

            if (existing == null)
                throw new BadRequestException("Invalid refresh token");

            if (!existing.IsActive)
                throw new UnauthorizedException(
                    existing.IsExpired ? "Refresh token has expired" : "Refresh token has been revoked");

            // Revoke the old token (rotate)
            existing.RevokedAt = DateTimeOffset.Now;

            var roles = await _userManager.GetRolesAsync(existing.User);
            var response = await GenerateTokenPairAsync(existing.User, roles);

            // Persist the revocation + new token atomically
            await _refreshTokenRepository.SaveAsync();

            return response;
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                throw new BadRequestException(
                    "User id and token are required parameters for email confirmation");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new BadRequestException("User not found");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.First().Description);



            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now);
            await _userManager.ResetAccessFailedCountAsync(user);
        }


        #region
        private async Task AddRoleAsync(
   ApplicationUser user,
   string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
        }

        private static string BuildAccountConfirmationUrl(string accountConfirmationUrl, ApplicationUser userToReturn, string token)
        {
            return $"{accountConfirmationUrl}" +
                   $"?userId={Uri.EscapeDataString(userToReturn.Id)}" +
                   $"&token={Uri.EscapeDataString(token)}";
        }
        private static string EmailConfirmationBody(string confirmationUrl)
        {
            return $@"
                <h2>Account Activation</h2>
                <p>Your administrator account has been created.</p>
                <p>Please click the link below to activate your account:</p>
                <p>
                    <a href=""{confirmationUrl}"">
                        Activate Account
                    </a>
                </p>";
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationUrl = BuildAccountConfirmationUrl(
                _configuration["ClientUrls:ConfirmEmail"],
                user,
                token);

            var body = EmailConfirmationBody(confirmationUrl);

            await _emailService.Send(user.Email, "Confirm your account", body);
        }

        
        private async Task<LoginResponseDto> GenerateTokenPairAsync(ApplicationUser user, IList<string> roles)
        {
            var accessToken = _jwtTokenGenerator.GenerateToken(user, roles);

            var refreshToken = new RefreshToken
            {
                Token = _jwtTokenGenerator.GenerateRefreshToken(),
                UserId = user.Id,
                CreatedAt = DateTimeOffset.Now,
                ExpiresAt = DateTimeOffset.Now.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"]))
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveAsync();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };

        }

        #endregion

    }
}
