using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class SeatReservationConfiguration : IEntityTypeConfiguration<SeatReservation>
{
    public void Configure(EntityTypeBuilder<SeatReservation> builder)
    {
        BookingMapping.Configure(builder, "SeatReservations");
        builder.HasAlternateKey(x => new { x.Id, x.ShowtimeId });
        builder.ToTable("SeatReservations", "ordering", table =>
        {
            table.HasCheckConstraint("CK_SeatReservations_Status", "[Status] IN (0,1,2,3,4)");
            table.HasCheckConstraint("CK_SeatReservations_Owner", BookingMapping.OwnerCheck);
            table.HasCheckConstraint("CK_SeatReservations_GuestHash", BookingMapping.GuestHashCheck);
            table.HasCheckConstraint("CK_SeatReservations_Total", "[TotalAmount] >= 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_SeatReservations_SeatCount", "[SeatCount] BETWEEN 1 AND 2600");
            table.HasCheckConstraint("CK_SeatReservations_State",
                "([Status] = 0 AND [ConvertedAt] IS NULL AND [ConfirmedAt] IS NULL AND [ReleasedAt] IS NULL) OR "
                + "([Status] = 1 AND [ConvertedAt] IS NOT NULL AND [ConfirmedAt] IS NULL AND [ReleasedAt] IS NULL) OR "
                + "([Status] = 2 AND [ConvertedAt] IS NOT NULL AND [ConfirmedAt] IS NOT NULL AND [ReleasedAt] IS NULL) OR "
                + "([Status] IN (3,4) AND [ConfirmedAt] IS NULL AND [ReleasedAt] IS NOT NULL)");
        });
        builder.HasOne(x => x.Showtime).WithMany().HasForeignKey(x => x.ShowtimeId).OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(x => x.Seats).HasField("_seats").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.ShowtimeId);
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        builder.HasIndex(x => new { x.GuestId, x.CreatedAt });
    }
}
