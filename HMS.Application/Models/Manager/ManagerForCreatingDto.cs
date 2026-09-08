
namespace HMS.Application.Models.Manager
{
    public class ManagerForCreatingDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PersonalNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public string ApplicationUserId { get; set; }
        public int HotelId { get; set; }
    }
}
