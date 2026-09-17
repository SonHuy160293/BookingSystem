using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Core;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly BookingSystemCinemaDbContext _dbContext;

    public EfUnitOfWork(BookingSystemCinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var response = await action(cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_Cinemas" or "UX_Cinemas_Slug" })
            {
                throw new ConflictException("Cinema.Conflict", "The Branch already has a Cinema profile or the slug is already in use.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_Cinemas_Regions_RegionId" or "FK_FilmGenres_Genres_GenreId" })
            {
                throw new BadRequestException("The selected Region or Genre no longer exists.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_ScreeningRooms_CinemaId_Code" })
            {
                throw new ConflictException("ScreeningRoom.CodeExists", "This screening room code is already in use within the Cinema.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_ScreeningRooms_Cinemas_CinemaId" })
            {
                throw new BadRequestException("The selected Cinema no longer exists.");
            }

            await transaction.CommitAsync(cancellationToken);

            return response;
        });
    }
}
