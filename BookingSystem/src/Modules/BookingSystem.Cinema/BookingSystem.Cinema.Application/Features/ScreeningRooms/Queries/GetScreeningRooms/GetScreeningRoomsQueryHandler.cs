using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRooms;

public sealed class GetScreeningRoomsQueryHandler : IQueryHandler<GetScreeningRoomsQuery, PagedResult<ScreeningRoomListItemDto>>
{
    private readonly IScreeningRoomRepository _repository;

    public GetScreeningRoomsQueryHandler(IScreeningRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ScreeningRoomListItemDto>>> HandleAsync(GetScreeningRoomsQuery query, CancellationToken cancellationToken)
        => Result.Success(await _repository.ListAsync(query.Request, cancellationToken));
}
