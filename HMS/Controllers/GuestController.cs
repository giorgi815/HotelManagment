using HMS.Application.Contracts.Services;
using HMS.Application.Models.Common;
using HMS.Application.Models.Guest;
using HMS.Application.Models.Reservation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security.Claims;

namespace HMS.Controllers
{

    [Route("api/guest")]
    [ApiController]
    public class GuestController : ControllerBase
    {
        private readonly IGuestService _guestService;
        private readonly IReservationService _reservationService;

        public GuestController(IGuestService guestService, IReservationService reservationService)
        {
            _guestService = guestService;
            _reservationService = reservationService;
        }


        [Authorize(Roles = "Guest,Manager")]
        [HttpGet("SearchReservations")]
        public async Task<IActionResult> SearchReservations([FromQuery] ReservationForSearchDto model)
        {
            var result = await _reservationService.GetAllReservationsAsync(
                model,
                User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            );
            var response = new CommonResponse
            {
                Message = "Reservations retrieved successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(response.HttpStatusCode, response);
        }


        [HttpPut("UpdateGuest")]
        public async Task<IActionResult> UpdateGuest([FromBody] GuestForUpdatingDto model)
        {
            var result = await _guestService.UpdateGuestAsync(model);
            var response = new CommonResponse
            {
                Message = "Reservation updated successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpDelete("DeleteGuest")]
        public async Task<IActionResult> DeleteGuest([FromRoute] int guestId)
        {
            var result = await _guestService.DeleteGuestAsync(guestId);
            var response = new CommonResponse
            {
                Message = "Guest deleted successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };
            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpDelete("DeleteReservation")]
        public async Task<IActionResult> DeleteReservation([FromRoute] int guestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _reservationService.DeleteReservationAsync(guestId, userId);
            var response = new CommonResponse
            {
                Message = "Guest deleted successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };
            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpPost("CreateReservation")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationForCreatingDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _reservationService.CreateReservationAsync(model, userId);

            var response = new CommonResponse
            {
                Message = "Reservation created successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };

            return StatusCode(response.HttpStatusCode, response);
        }

        [HttpPut("UpdateReservation")]
        public async Task<IActionResult> UpdateReservation([FromBody] ReservationForUpdatingDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _reservationService.UpdateReservationAsync(model, userId);
            var response = new CommonResponse
            {
                Message = "Reservation updated successfully",
                Result = result,
                IsSuccess = true,
                HttpStatusCode = Convert.ToInt32(HttpStatusCode.OK)
            };
            return StatusCode(response.HttpStatusCode, response);
        }

    }
}
