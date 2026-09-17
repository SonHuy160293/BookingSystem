using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Application.Mappings;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Commands.UpdateCinema;

public sealed class UpdateCinemaCommandHandler : ICommandHandler<UpdateCinemaCommand, CinemaDto>
{
    private readonly ICinemaRepository _repository;
    private readonly ICinemaAccessService _access;

    public UpdateCinemaCommandHandler(ICinemaRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<CinemaDto>> HandleAsync(UpdateCinemaCommand command, CancellationToken cancellationToken)
    {
        var scope = await _access.GetCinemaScopeAsync(ActionType.Update, cancellationToken);
        var request = command.Request;
        var cinema = await _repository.GetForUpdateAsync(command.Id, scope, cancellationToken);

        var region = await _repository.GetRegionAsync(request.RegionId, cancellationToken);
        string slug = request.Slug.Trim().ToLowerInvariant();
        if (await _repository.SlugExistsAsync(slug, command.Id, cancellationToken))
        {
            throw new ConflictException("Cinema.SlugExists", "This Cinema slug is already in use.");
        }

        cinema.ChangeDetails(request.RegionId, request.Name, slug, request.Address, request.Description, request.Latitude, request.Longitude);
        cinema.ChangeAvailability(request.PublicationStatus, request.OperatingStatus, request.OnlineBookingEnabled);

        return Result.Success(CinemaMapping.ToDto(cinema, region));
    }
}
