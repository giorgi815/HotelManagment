
namespace HMS.Application.Models.Reservation
{
    public class ReservationForCreatingDto
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public List<int> RoomIds { get; set; }
    }
}
