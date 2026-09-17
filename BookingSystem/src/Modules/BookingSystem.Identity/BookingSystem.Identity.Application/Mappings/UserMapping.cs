using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Mappings;

public static class UserMapping
{
    public static AccountUserDto ToDto(User user)
        => new(
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.JobTitle,
            user.IsEnabled,
            user.CreatedAt);
}
