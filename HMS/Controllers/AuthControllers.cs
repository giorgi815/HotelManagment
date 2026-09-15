using HMS.Application.Contracts.Services;
using HMS.Application.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace HMS.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthControllers(IAuthService authService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegistrationRequestDto model)
        {

            var result = await authService.RegisterAdminAsync(model);


            var response = new CommonResponse()
            {
                Message = "Admin has successfully registered",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.Created),
                Result = result
            };

            return StatusCode(response.HttpStatusCode, response);

        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register-manager")]
        public async Task<IActionResult> RegisterManager([FromBody] ManagerRegistrationRequestDto model)
        {
            var result = await authService.RegisterManagerAsync(model);

            var response = new CommonResponse()
            {
                Message = "Mannager has successfully registered",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.Created),
                Result = result
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpPost("register-guest")]
        public async Task<IActionResult> RegisterGuest([FromBody] GuestRegistrationRequestDto model)
        {
            var result = await authService.RegisterGuestAsync(model);

            var response = new CommonResponse()
            {
                Message = "Guest has successfully registered",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.Created),
                Result = result
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            await authService.ConfirmEmailAsync(userId, token);

            var response = new CommonResponse()
            {
                Message = "Email confirmed successfully",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto model)
        {
            var result = await authService.LoginAsync(model);

            var response = new CommonResponse()
            {
                Message = "Logged in successfully",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK),
                Result = result
            };

            return StatusCode(response.HttpStatusCode, response);

        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            var result = await authService.RefreshTokenAsync(model.RefreshToken);

            var response = new CommonResponse()
            {
                Message = "Token refreshed successfully",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK),
                Result = result
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto model)
        {
            await authService.RevokeRefreshTokenAsync(model.RefreshToken);

            var response = new CommonResponse()
            {
                Message = "Logged out successfully",
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(response.HttpStatusCode, response);
        }
    }
}
