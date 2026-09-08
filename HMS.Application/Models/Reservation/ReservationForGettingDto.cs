
namespace HMS.Application.Models.Reservation
{
    public class ReservationForGettingDto
    {
        public int Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestId { get; set; }
    }
}
