using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Common;
using HMS.Application.Models.Reservation;
using HMS.Domain.Entities;
using HMS.Domain.Enum;
using MapsterMapper;

namespace HMS.Application.Services
{
    public class ReservationService : IReservationService
    {

        private readonly IReservationRepository _reservationRepository;
        private readonly IGuestRepository _guestRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IManagerRepository _managerRepository;
        private readonly IMapper _mapper;

        public ReservationService(
            IReservationRepository reservationRepository,
            IGuestRepository guestRepository,
            IRoomRepository roomRepository,
            IMapper mapper,
            IManagerRepository managerRepository)
        {
            _reservationRepository = reservationRepository;
            _guestRepository = guestRepository;
            _roomRepository = roomRepository;
            _mapper = mapper;
            _managerRepository = managerRepository;
        }

        public async Task<ReservationForGettingDto> CreateReservationAsync(ReservationForCreatingDto model, string userId)
        {
            if (model is null)
                throw new BadRequestException("Model is required");

            if (model.RoomIds is null || !model.RoomIds.Any())
                throw new BadRequestException("At least one room must be selected");

            var today = DateTime.UtcNow.Date;

            if (model.CheckInDate.Date < today)
                throw new BadRequestException("Check-in date can't be in the past");

            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
                throw new BadRequestException("Check-out date must be after the check-in date");

            var guest = await _guestRepository.GetAsync(x => x.ApplicationUserId == userId);

            if(guest is null)
                throw new BadRequestException("Guest not found");

            var rooms = await _roomRepository.GetAllAsync(
                filter: x => model.RoomIds.Contains(x.RoomId) &&
                !x.ReservationRooms.Any(
                    rr => rr.Reservation.CheckInDate < model.CheckOutDate
                    && rr.Reservation.CheckOutDate > model.CheckInDate),
                tracikng: true);

            var avialibleRooms = rooms.Items.ToList();

            if(avialibleRooms.Count != model.RoomIds.Count)
                throw new BadRequestException("One or more selected rooms are not available for the selected dates");

            var reservation = new Reservation
            {
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                GuestId = guest.GuestId,
                Status = ReservationStatusFilter.Reserved,
                ReservationRooms = avialibleRooms.Select(r => new ReservationRoom
                {
                    RoomId = r.RoomId
                }).ToList()
            };

            await _reservationRepository.AddAsync(reservation);
            await _reservationRepository.SaveAsync();

            return new ReservationForGettingDto
            {
                Id = reservation.ReservationId,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                GuestId = reservation.GuestId
            };
        }

        public async Task<int> DeleteReservationAsync(int id, string userId)
        {
            var guest = await _guestRepository.GetAsync(x => x.ApplicationUserId == userId);

            if(guest == null)
                throw new BadRequestException("Guest not found");

            var reservation = await _reservationRepository.GetAsync(x => x.ReservationId == id && x.GuestId == guest.GuestId);

            if(reservation == null)
                throw new BadRequestException("Reservation not found");

            reservation.Status = ReservationStatusFilter.Cancelled;
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveAsync();

            return id;
        }

        public async Task<PagedResponseDto<ReservationForGettingDto>> GetAllReservationsAsync(ReservationForSearchDto filter, string userId, string role)
        {
            var guest = await _guestRepository.GetAsync(x => x.ApplicationUserId == userId);

            if(guest == null)
                throw new BadRequestException("Guest not found");

            int? pageNumber = (filter.PageNumber > 0) ? filter.PageNumber : null;
            int? pageSize = (filter.PageSize > 0) ? filter.PageSize : null;

            var isGuest = string.Equals(role, "Guest", StringComparison.OrdinalIgnoreCase);
            var isManager = string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);

            int? managerHotelId = null;

            var reservations = await _reservationRepository.GetAllAsync(
                filter: x =>
                    (isGuest ? x.GuestId == guest.GuestId : true) &&
                    (filter.GuestId.HasValue ? x.GuestId == filter.GuestId.Value : true) &&
                    (filter.Date.HasValue ? x.CheckInDate <= filter.Date.Value && x.CheckOutDate >= filter.Date.Value : true) &&
                    (filter.RoomId.HasValue ? x.ReservationRooms.Any(rr => rr.RoomId == filter.RoomId.Value) : true) &&
                    (filter.HotelId.HasValue ? x.ReservationRooms.Any(rr => rr.Room.HotelId == filter.HotelId.Value) : (isManager && managerHotelId.HasValue ? x.ReservationRooms.Any(rr => rr.Room.HotelId == managerHotelId.Value) : true)) &&
                    (filter.Status != ReservationStatusFilter.All ? x.Status == filter.Status : true),
                orderBy: x => x.CheckInDate,
                ascending: false,
                pageNumber: pageNumber,
                pageSize: pageSize,
                tracikng: false);

            if (isManager)
            {
                var manager = await _managerRepository.GetAsync(m => m.ApplicationUserId == userId);
                if (manager != null)
                    managerHotelId = manager.HotelId;
            }


            return new PagedResponseDto<ReservationForGettingDto>
            {
                Items = _mapper.Map<IEnumerable<ReservationForGettingDto>>(reservations.Items),
                TotalCount = reservations.TotalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

        }

        public async Task<IEnumerable<ReservationForGettingDto>> GetReservationByIdAsync(ReservationForSearchDto model, string userId)
        {
            var guest = await _guestRepository.GetAsync(x => x.ApplicationUserId == userId);

            if(guest == null)
                throw new BadRequestException("Guest not found");
          
            var reservation = await _reservationRepository.GetAllAsync(
                filter: x => x.ReservationId == model.RoomId && x.GuestId == guest.GuestId,
                tracikng: false);

            return _mapper.Map<IEnumerable<ReservationForGettingDto>>(reservation.Items);
        }

        public async Task<ReservationForGettingDto> UpdateReservationAsync(ReservationForUpdatingDto model, string userId)
        {
            if(model.CheckInDate.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Check-in date can't be in the past");

            if(model.CheckOutDate.Date <= model.CheckInDate.Date)
                throw new BadRequestException("Check-out date must be after the check-in date");

            var reservation = _reservationRepository.GetAsync(x => x.ReservationId == model.Id && x.Guest.ApplicationUserId == userId).Result;

            if(reservation == null)
                throw new BadRequestException("Reservation not found");
                

            var hasConflict = _reservationRepository.GetAllAsync(
                filter: x => x.ReservationId != model.Id &&
                x.ReservationRooms.Any(rr => reservation.ReservationRooms.Select(r => r.RoomId).Contains(rr.RoomId)) &&
                x.CheckInDate < model.CheckOutDate && x.CheckOutDate > model.CheckInDate,
                tracikng: false).Result.Items.Any();

            if(hasConflict)
                throw new BadRequestException("One or more selected rooms are not available for the selected dates");

            reservation.CheckInDate = model.CheckInDate;
            reservation.CheckOutDate = model.CheckOutDate;

            await _reservationRepository.SaveAsync();

            return new ReservationForGettingDto
            {
                Id = reservation.ReservationId,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                GuestId = reservation.GuestId
            };
        }
    }
}
