using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRoomById;

public sealed class GetScreeningRoomByIdQueryHandler : IQueryHandler<GetScreeningRoomByIdQuery, ScreeningRoomDto>
{
    private readonly IScreeningRoomRepository _repository;

    public GetScreeningRoomByIdQueryHandler(IScreeningRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ScreeningRoomDto>> HandleAsync(GetScreeningRoomByIdQuery query, CancellationToken cancellationToken)
        => Result.Success(await _repository.GetScreeningRoomAsync(query.Id, cancellationToken));
}
