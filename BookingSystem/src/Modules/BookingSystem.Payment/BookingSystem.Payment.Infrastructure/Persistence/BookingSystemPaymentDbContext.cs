using BookingSystem.EntityFrameworkCore.Reliability;
using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using PaymentEntity = BookingSystem.Payment.Domain.Models.Payment;

namespace BookingSystem.Payment.Infrastructure.Persistence;

public sealed class BookingSystemPaymentDbContext : DbContext
{
    public BookingSystemPaymentDbContext(DbContextOptions<BookingSystemPaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<PaymentProviderEvent> PaymentProviderEvents => Set<PaymentProviderEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<PaymentReconciliationCase> PaymentReconciliationCases => Set<PaymentReconciliationCase>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemPaymentDbContext).Assembly);
        modelBuilder.ApplyReliabilityConfigurations(ReliabilityDatabaseProvider.SqlServer);
        modelBuilder.ApplyApplicationConventions();
    }
}
