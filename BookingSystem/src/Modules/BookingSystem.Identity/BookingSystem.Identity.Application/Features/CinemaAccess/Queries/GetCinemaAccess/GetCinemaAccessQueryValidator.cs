using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaAccess;

public sealed class GetCinemaAccessQueryValidator : IValidator<GetCinemaAccessQuery>
{
    private static readonly HashSet<string> AllowedPermissions = new(StringComparer.Ordinal)
    {
        $"{Functions.Cinemas}.{ActionType.View}", $"{Functions.Cinemas}.{ActionType.Create}", $"{Functions.Cinemas}.{ActionType.Update}",
        $"{Functions.Films}.{ActionType.View}", $"{Functions.Films}.{ActionType.Create}", $"{Functions.Films}.{ActionType.Update}"
    };

    public Task ValidateAsync(GetCinemaAccessQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query.UserId == Guid.Empty || !AllowedPermissions.Contains(query.Permission))
        {
            throw new CqrsValidationException(["A nonempty identifier and supported permission are required."]);
        }

        return Task.CompletedTask;
    }
}
