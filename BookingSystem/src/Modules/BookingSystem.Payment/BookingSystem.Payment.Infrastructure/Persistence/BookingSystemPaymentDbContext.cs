using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Payment.Infrastructure.Persistence;

public sealed class BookingSystemPaymentDbContext : DbContext
{
    public BookingSystemPaymentDbContext(DbContextOptions<BookingSystemPaymentDbContext> options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemPaymentDbContext).Assembly);
        modelBuilder.ApplyApplicationConventions();
    }
}
