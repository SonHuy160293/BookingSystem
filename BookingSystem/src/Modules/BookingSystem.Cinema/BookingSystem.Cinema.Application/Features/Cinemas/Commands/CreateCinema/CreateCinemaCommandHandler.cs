using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Application.Mappings;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Commands.CreateCinema;

public sealed class CreateCinemaCommandHandler : ICommandHandler<CreateCinemaCommand, CinemaDto>
{
    private readonly ICinemaRepository _repository;
    private readonly ICinemaAccessService _access;

    public CreateCinemaCommandHandler(ICinemaRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<CinemaDto>> HandleAsync(CreateCinemaCommand command, CancellationToken cancellationToken)
    {
        var scope = await _access.GetCinemaScopeAsync(ActionType.Create, cancellationToken);
        var request = command.Request;
        var branch = await _access.RequireBranchAsync(request.BranchId, scope, cancellationToken);
        if (await _repository.ExistsAsync(branch.Id, cancellationToken))
        {
            throw new ConflictException("Cinema.AlreadyExists", "A Cinema profile already exists for this Branch.");
        }

        var region = await _repository.GetRegionAsync(request.RegionId, cancellationToken);
        string slug = request.Slug.Trim().ToLowerInvariant();
        if (await _repository.SlugExistsAsync(slug, null, cancellationToken))
        {
            throw new ConflictException("Cinema.SlugExists", "This Cinema slug is already in use.");
        }

        var cinema = CinemaEntity.Create(
            branch.TenantId,
            branch.Id,
            request.RegionId,
            request.Name,
            slug,
            request.Address,
            request.PublicationStatus,
            request.OperatingStatus,
            request.OnlineBookingEnabled,
            request.Description,
            request.Latitude,
            request.Longitude);
        await _repository.AddAsync(cinema, cancellationToken);

        return Result.Success(CinemaMapping.ToDto(cinema, region));
    }
}
