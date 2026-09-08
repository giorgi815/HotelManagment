using HMS.Domain.Enum;
using HMS.Application.Models.Common;

namespace HMS.Application.Models.Reservation
{
    public class ReservationForSearchDto : PagedRequestDto
    {
        public int? GuestId { get; set; }
        public DateTime? Date { get; set; }
        public int? RoomId { get; set; }
        public int? HotelId { get; set; }
        public ReservationStatusFilter Status { get; set; } = ReservationStatusFilter.All;
    }
}
