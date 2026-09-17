using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Order.Infrastructure.Persistence;

public sealed class BookingSystemOrderDbContext : DbContext
{
    public BookingSystemOrderDbContext(DbContextOptions<BookingSystemOrderDbContext> options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemOrderDbContext).Assembly);
        modelBuilder.ApplyApplicationConventions();
    }
}
