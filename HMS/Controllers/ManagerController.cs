using HMS.Application.Contracts.Services;
using HMS.Application.Models.Common;
using HMS.Application.Models.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace HMS.Controllers
{
    [Route("api/manager")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class ManagerController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ManagerController(
            IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet("reservations")]
        public async Task<IActionResult> SearchReservations(
            [FromQuery] ReservationForSearchDto model)
        {
            var userId =
                User.FindFirstValue(
                    JwtRegisteredClaimNames.Sub)
                ??
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result =
                await _reservationService
                    .GetAllReservationsAsync(
                        model,
                        userId,
                        "Manager");

            var response = new CommonResponse
            {
                Message =
                    "Hotel reservations retrieved successfully",

                Result = result,
                IsSuccess = true,

                HttpStatusCode =
                    Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(
                response.HttpStatusCode,
                response);
        }
    }
}