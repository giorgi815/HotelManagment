using HMS.Application.Contracts.Presistence;
using HMS.Application.Contracts.Services;
using HMS.Application.Exceptions;
using HMS.Application.Models.Common;
using HMS.Application.Models.Room;
using HMS.Domain.Entities;
using HMS.Domain.Enum;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HMS.Application.Services
{
    public class RoomService(IRoomRepository roomRepository, IHotelRepository hotelRepository, IMapper mapper) : IRoomService
    {
        public async Task<PagedResponseDto<RoomForGettingDto>> GetAllRoomsAsync(PagedRequestDto parameters)
        {
            var rooms = await roomRepository.GetAllAsync(
                orderBy: BuildOrderBy(parameters.SortBy),
                ascending: parameters.Ascending,
                pageNumber: parameters.PageNumber,
                pageSize: parameters.PageSize
                );

            return MapToPagedResponseDto(rooms, parameters);
        }

        public async Task<IEnumerable<RoomForGettingDto>> GetRoomByHotelAsync(int hotelId)
        {
            var rooms = await roomRepository.GetAllAsync(filter: r => r.HotelId == hotelId, tracikng: false);
            var mappedRooms = mapper.Map<IEnumerable<RoomForGettingDto>>(rooms.Items);

            return mappedRooms;
        }

        public async Task<PagedResponseDto<RoomForGettingDto>>
    SearchAvailableRoomsAsync(
        RoomSearchRequestDto parameters)
        {
            if (parameters is null)
                throw new BadRequestException(
                    "Search parameters are required");

            if (parameters.CheckInDate.HasValue !=
                parameters.CheckOutDate.HasValue)
            {
                throw new BadRequestException(
                    "Both check-in and check-out dates are required");
            }

            if (parameters.MinPrice.HasValue &&
                parameters.MaxPrice.HasValue &&
                parameters.MinPrice.Value >
                parameters.MaxPrice.Value)
            {
                throw new BadRequestException(
                    "Minimum price cannot be greater than maximum price");
            }

            if (parameters.CheckInDate.HasValue &&
                parameters.CheckOutDate.HasValue)
            {
                if (parameters.CheckInDate.Value.Date <
                    DateTime.UtcNow.Date)
                {
                    throw new BadRequestException(
                        "Check-in date can't be in the past");
                }

                if (parameters.CheckOutDate.Value.Date <=
                    parameters.CheckInDate.Value.Date)
                {
                    throw new BadRequestException(
                        "Check-out date must be after check-in date");
                }
            }

            var checkIn = parameters.CheckInDate;
            var checkOut = parameters.CheckOutDate;

            Expression<Func<Room, bool>> filter = room =>

                (!parameters.MinPrice.HasValue ||
                    room.Price >= parameters.MinPrice.Value)

                &&

                (!parameters.MaxPrice.HasValue ||
                    room.Price <= parameters.MaxPrice.Value)

                &&

                (
                    !checkIn.HasValue ||
                    !checkOut.HasValue

                    ||

                    !room.ReservationRooms.Any(rr =>

                        rr.Reservation.Status !=
                            ReservationStatusFilter.Cancelled

                        && rr.Reservation.CheckInDate <
                            checkOut.Value

                        && rr.Reservation.CheckOutDate >
                            checkIn.Value
                    )
                );

            var rooms = await roomRepository.GetAllAsync(
                filter: filter,
                orderBy: BuildOrderBy(parameters.SortBy),
                ascending: parameters.Ascending,
                pageNumber: parameters.PageNumber,
                pageSize: parameters.PageSize,
                tracikng: false);

            return MapToPagedResponseDto(
                rooms,
                parameters);
        }

        public async Task<int> CreateRoomAsync(RoomForCreatingDto model)
        {
            if (model is null)
                throw new BadRequestException("Room model is required");


            var hotel = await hotelRepository.GetAsync(h => h.HotelId == model.HotelId);

            if (hotel is null)
                throw new NotFoundException($"Hotel with Id {model.HotelId} not found");

            var existingRoom = await roomRepository.GetAsync(r => r.Name == model.Name);

            if (existingRoom is not null)
                throw new BadRequestException($"Room with name {model.Name} already exists");

            var room = mapper.Map<Room>(model);
            await roomRepository.AddAsync(room);
            await roomRepository.SaveAsync();
            return room.RoomId;

        }
        public async Task<RoomForGettingDto> UpdateRoomAsync(RoomForUpdatingDto model)
        {
            if (model is null)
                throw new BadRequestException("Room model is required");

            var room = await roomRepository.GetAsync(r => r.RoomId == model.RoomId);

            if (room is null)
                throw new NotFoundException($"Room with Id {model.RoomId} not found");

            mapper.Map(model, room);
            roomRepository.Update(room);
            await roomRepository.SaveAsync();

            return mapper.Map<RoomForGettingDto>(room);

        }

        public async Task DeleteRoomAsync(int roomId)
        {
            if (roomId <= 0)
            {
                throw new BadRequestException(
                    "Room Id is required and must be greater than zero");
            }

            var room = await roomRepository.GetAsync(
                fillter: r => r.RoomId == roomId,
                tracking: true,
                include: query =>
                    query.Include(r => r.ReservationRooms));

            if (room is null)
            {
                throw new NotFoundException(
                    $"Room with Id {roomId} not found");
            }

            if (room.ReservationRooms.Any())
            {
                throw new BadRequestException(
                    $"Room with Id {roomId} cannot be deleted " +
                    "because it has reservation history.");
            }

            roomRepository.Remove(room);

            await roomRepository.SaveAsync();
        }

        #region
        private static Expression<Func<Room, object>> BuildOrderBy(string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "name" => c => c.Name,
                "price" => c => c.Price,
                _ => c => c.RoomId
            };
        }

        private PagedResponseDto<RoomForGettingDto> MapToPagedResponseDto((IEnumerable<Room> Items, int TotalCount) room, PagedRequestDto paramaters)
        {
            return new PagedResponseDto<RoomForGettingDto>
            {
                Items = room.Items.Any()
                ? mapper.Map<IEnumerable<RoomForGettingDto>>(room.Items)
                : Enumerable.Empty<RoomForGettingDto>(),
                TotalCount = room.TotalCount,
                PageNumber = paramaters.PageNumber,
                PageSize = paramaters.PageSize
            };
        #endregion


        }


    }
}
