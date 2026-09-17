using BookingSystem.EntityFrameworkCore.Reliability;
using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Order.Infrastructure.Persistence;

public sealed class BookingSystemOrderDbContext : DbContext
{
    public BookingSystemOrderDbContext(DbContextOptions<BookingSystemOrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShowtimeInventory> ShowtimeInventories => Set<ShowtimeInventory>();
    public DbSet<ShowtimeSeat> ShowtimeSeats => Set<ShowtimeSeat>();
    public DbSet<SeatReservation> SeatReservations => Set<SeatReservation>();
    public DbSet<ReservedSeat> ReservedSeats => Set<ReservedSeat>();
    public DbSet<BookingOrder> Orders => Set<BookingOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<OrderRefundRequest> OrderRefundRequests => Set<OrderRefundRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemOrderDbContext).Assembly);
        modelBuilder.ApplyReliabilityConfigurations(ReliabilityDatabaseProvider.SqlServer);
        modelBuilder.ApplyApplicationConventions();
    }
}
