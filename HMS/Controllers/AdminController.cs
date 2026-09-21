using HMS.Application.Contracts.Services;
using HMS.Application.Models.Auth;
using HMS.Application.Models.Common;
using HMS.Application.Models.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace HMS.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IManagerService _managerService;

        public AdminController(
            IManagerService managerService)
        {
            _managerService = managerService;
        }

        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers()
        {
            var result =
                await _managerService.GetManagerAsync();

            var response = new CommonResponse
            {
                Message = "Managers retrieved successfully",
                IsSuccess = true,
                HttpStatusCode =
                    Convert.ToInt32(HttpStatusCode.OK),
                Result = result
            };

            return StatusCode(
                response.HttpStatusCode,
                response);
        }


        [HttpPut("managers")]
        public async Task<IActionResult> UpdateManager(
            [FromBody] ManagerForUpdatingDto model)
        {
            var result =
                await _managerService.UpdateManagerAsync(model);

            var response = new CommonResponse
            {
                Message = "Manager updated successfully",
                IsSuccess = true,
                HttpStatusCode =
                    Convert.ToInt32(HttpStatusCode.OK),
                Result = result
            };

            return StatusCode(
                response.HttpStatusCode,
                response);
        }

        [HttpDelete("managers/{id:int}")]
        public async Task<IActionResult> DeleteManager(
            [FromRoute] int id)
        {
            var result =
                await _managerService.DeleteManagerAsync(id);

            var response = new CommonResponse
            {
                Message = "Manager deleted successfully",
                IsSuccess = true,
                HttpStatusCode =
                    Convert.ToInt32(HttpStatusCode.OK),
                Result = result
            };

            return StatusCode(
                response.HttpStatusCode,
                response);
        }
    }
}