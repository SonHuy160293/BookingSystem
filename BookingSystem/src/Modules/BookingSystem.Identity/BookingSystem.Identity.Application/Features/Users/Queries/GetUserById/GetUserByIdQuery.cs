using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<AccountUserDto>;
