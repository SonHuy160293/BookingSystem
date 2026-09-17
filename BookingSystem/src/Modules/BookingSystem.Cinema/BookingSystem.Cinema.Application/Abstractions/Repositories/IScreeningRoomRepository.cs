using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Abstractions.Repositories;

public interface IScreeningRoomRepository : IRepositoryBase<ScreeningRoom, Guid>
{
    Task<ScreeningRoomDto> GetScreeningRoomAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<ScreeningRoomListItemDto>> ListAsync(GetScreeningRoomsRequest request, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(Guid cinemaId, string normalizedCode, Guid? excludingId, CancellationToken cancellationToken);
}
