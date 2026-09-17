using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Cinema.Infrastructure.Persistence;

public sealed class BookingSystemCinemaDbContext : DbContext
{
    public BookingSystemCinemaDbContext(DbContextOptions<BookingSystemCinemaDbContext> options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemCinemaDbContext).Assembly);
        modelBuilder.ApplyApplicationConventions();
    }
}
