using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeInventoryConfiguration : IEntityTypeConfiguration<ShowtimeInventory>
{
    public void Configure(EntityTypeBuilder<ShowtimeInventory> builder)
    {
        BookingMapping.Configure(builder, "ShowtimeInventories");
        builder.HasAlternateKey(x => new { x.Id, x.TenantId });
        builder.ToTable("ShowtimeInventories", "ordering", table =>
        {
            table.HasCheckConstraint("CK_ShowtimeInventories_Status", "[Status] IN (0,1,2,3)");
            table.HasCheckConstraint("CK_ShowtimeInventories_Revisions", "[SourceRevision] > 0 AND [AvailabilityRevision] >= 0");
            table.HasCheckConstraint("CK_ShowtimeInventories_TimeRange", "[EndsAt] > [StartsAt]");
            table.HasCheckConstraint("CK_ShowtimeInventories_SeatCount", "[ExpectedSeatCount] BETWEEN 0 AND 2600");
            table.HasCheckConstraint("CK_ShowtimeInventories_Currency", "[CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_ShowtimeInventories_Cancellation",
                "([Status] = 3 AND [CancelledAt] IS NOT NULL AND [CancellationReasonCode] IS NOT NULL) OR ([Status] <> 3 AND [CancelledAt] IS NULL AND [CancellationReasonCode] IS NULL)");
        });
        builder.Navigation(x => x.Seats).HasField("_seats").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.TenantId, x.StartsAt });
        builder.HasIndex(x => new { x.Status, x.StartsAt });
    }
}
