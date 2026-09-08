using HMS.Application.Models.Common;
using HMS.Application.Models.Reservation;

namespace HMS.Application.Contracts.Services
{
    public interface IReservationService
    {
        Task<ReservationForGettingDto> CreateReservationAsync(ReservationForCreatingDto model, string userId);
        Task<IEnumerable<ReservationForGettingDto>> GetReservationByIdAsync(ReservationForSearchDto model, string userId);
        Task<PagedResponseDto<ReservationForGettingDto>> GetAllReservationsAsync(ReservationForSearchDto filter, string userId, string role);
        Task<ReservationForGettingDto> UpdateReservationAsync(ReservationForUpdatingDto model, string userId);
        Task<int> DeleteReservationAsync(int id, string userId);
    }
}
