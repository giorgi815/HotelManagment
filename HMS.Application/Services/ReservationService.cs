using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Common;
using HMS.Application.Models.Reservation;
using HMS.Domain.Entities;
using HMS.Domain.Enum;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

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

        public async Task<ReservationForGettingDto> CreateReservationAsync(
    ReservationForCreatingDto model,
    string userId)
        {
            if (model is null)
                throw new BadRequestException("Model is required");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            if (model.RoomIds is null || !model.RoomIds.Any())
                throw new BadRequestException(
                    "At least one room must be selected");

            var today = DateTime.UtcNow.Date;

            if (model.CheckInDate.Date < today)
                throw new BadRequestException(
                    "Check-in date can't be in the past");

            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
                throw new BadRequestException(
                    "Check-out date must be after the check-in date");

            var requestedRoomIds = model.RoomIds
                .Distinct()
                .ToList();

            var guest = await _guestRepository.GetAsync(
                x => x.ApplicationUserId == userId);

            if (guest is null)
                throw new BadRequestException("Guest not found");

            var rooms = await _roomRepository.GetAllAsync(
                filter: room =>
                    requestedRoomIds.Contains(room.RoomId)

                    && !room.ReservationRooms.Any(rr =>
                        rr.Reservation.Status !=
                            ReservationStatusFilter.Cancelled

                        && rr.Reservation.CheckInDate <
                            model.CheckOutDate

                        && rr.Reservation.CheckOutDate >
                            model.CheckInDate),

                tracikng: true);

            var availableRooms = rooms.Items.ToList();

            if (availableRooms.Count != requestedRoomIds.Count)
            {
                throw new BadRequestException(
                    "One or more selected rooms are not available " +
                    "for the selected dates");
            }

            var reservation = new Reservation
            {
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                GuestId = guest.GuestId,

                Status = model.CheckInDate.Date <= today
                    ? ReservationStatusFilter.Active
                    : ReservationStatusFilter.Reserved,

                ReservationRooms = availableRooms
                    .Select(room => new ReservationRoom
                    {
                        RoomId = room.RoomId
                    })
                    .ToList()
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

        public async Task<PagedResponseDto<ReservationForGettingDto>>
    GetAllReservationsAsync(
        ReservationForSearchDto filter,
        string userId,
        string role)
        {
            if (filter is null)
                throw new BadRequestException("Search filter is required");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var isGuest = string.Equals(
                role,
                "Guest",
                StringComparison.OrdinalIgnoreCase);

            var isManager = string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase);

            if (!isGuest && !isManager)
            {
                throw new UnauthorizedException(
                    "Only guests and managers can search reservations");
            }

            var currentGuestId = 0;
            var managerHotelId = 0;

            if (isGuest)
            {
                var guest = await _guestRepository.GetAsync(
                    x => x.ApplicationUserId == userId);

                if (guest is null)
                    throw new BadRequestException("Guest not found");

                currentGuestId = guest.GuestId;
            }

            if (isManager)
            {
                var manager = await _managerRepository.GetAsync(
                    x => x.ApplicationUserId == userId);

                if (manager is null)
                    throw new BadRequestException("Manager not found");

                managerHotelId = manager.HotelId;
            }

            int? pageNumber =
                filter.PageNumber > 0
                    ? filter.PageNumber
                    : null;

            int? pageSize =
                filter.PageSize > 0
                    ? filter.PageSize
                    : null;

            var searchDate = filter.Date?.Date;

            var reservations =
                await _reservationRepository.GetAllAsync(
                    filter: reservation =>

                        (!isGuest ||
                            reservation.GuestId == currentGuestId)
                        &&

                        (!isManager ||
                            reservation.ReservationRooms.Any(rr =>
                                rr.Room.HotelId == managerHotelId))
                        &&

                        (!filter.GuestId.HasValue ||
                            reservation.GuestId ==
                            filter.GuestId.Value)

                        &&


                        (!searchDate.HasValue ||
                            (
                                reservation.CheckInDate <=
                                    searchDate.Value

                                && reservation.CheckOutDate >
                                    searchDate.Value
                            ))

                        &&

                        (!filter.RoomId.HasValue ||
                            reservation.ReservationRooms.Any(rr =>
                                rr.RoomId == filter.RoomId.Value))

                        &&

                        (!filter.HotelId.HasValue ||
                            reservation.ReservationRooms.Any(rr =>
                                rr.Room.HotelId ==
                                filter.HotelId.Value))

                        &&

                        (filter.Status ==
                            ReservationStatusFilter.All

                            || reservation.Status ==
                            filter.Status),

                    orderBy: x => x.CheckInDate,
                    ascending: false,
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    tracikng: false);

            return new PagedResponseDto<ReservationForGettingDto>
            {
                Items =
                    _mapper.Map<IEnumerable<ReservationForGettingDto>>(
                        reservations.Items),

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

        public async Task<ReservationForGettingDto>
    UpdateReservationAsync(
        ReservationForUpdatingDto model,
        string userId)
        {
            if (model is null)
                throw new BadRequestException(
                    "Reservation model is required");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException(
                    "User is not authenticated");

            var today = DateTime.UtcNow.Date;

            if (model.CheckInDate.Date < today)
            {
                throw new BadRequestException(
                    "Check-in date can't be in the past");
            }

            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
            {
                throw new BadRequestException(
                    "Check-out date must be after the check-in date");
            }

            var reservation =
                await _reservationRepository.GetAsync(
                    fillter: x =>
                        x.ReservationId == model.Id
                        && x.Guest.ApplicationUserId == userId,

                    tracking: true,

                    include: query =>
                        query.Include(x => x.ReservationRooms));

            if (reservation is null)
                throw new BadRequestException(
                    "Reservation not found");

            if (reservation.Status ==
                ReservationStatusFilter.Cancelled)
            {
                throw new BadRequestException(
                    "Cancelled reservations cannot be updated");
            }

            if (reservation.Status ==
                ReservationStatusFilter.Complete)
            {
                throw new BadRequestException(
                    "Completed reservations cannot be updated");
            }

            if (reservation.CheckOutDate.Date <= today)
            {
                throw new BadRequestException(
                    "Past reservations cannot be updated");
            }

            var roomIds = reservation.ReservationRooms
                .Select(rr => rr.RoomId)
                .ToList();

            if (!roomIds.Any())
            {
                throw new BadRequestException(
                    "Reservation does not contain any rooms");
            }

            var conflicts =
                await _reservationRepository.GetAllAsync(
                    filter: x =>

                        x.ReservationId != model.Id

                        && x.Status !=
                            ReservationStatusFilter.Cancelled

                        && x.ReservationRooms.Any(rr =>
                            roomIds.Contains(rr.RoomId))

                        && x.CheckInDate < model.CheckOutDate

                        && x.CheckOutDate > model.CheckInDate,

                    tracikng: false);

            if (conflicts.Items.Any())
            {
                throw new BadRequestException(
                    "One or more selected rooms are not available " +
                    "for the selected dates");
            }

            reservation.CheckInDate = model.CheckInDate;
            reservation.CheckOutDate = model.CheckOutDate;

            reservation.Status =
                model.CheckInDate.Date <= today
                    ? ReservationStatusFilter.Active
                    : ReservationStatusFilter.Reserved;

            _reservationRepository.Update(reservation);

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
