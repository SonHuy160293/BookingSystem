using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Inventory.Infrastructure.Persistence;

public sealed class BookingSystemInventoryDbContext : DbContext
{
    public BookingSystemInventoryDbContext(DbContextOptions<BookingSystemInventoryDbContext> options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemInventoryDbContext).Assembly);
        modelBuilder.ApplyApplicationConventions();
    }
}
