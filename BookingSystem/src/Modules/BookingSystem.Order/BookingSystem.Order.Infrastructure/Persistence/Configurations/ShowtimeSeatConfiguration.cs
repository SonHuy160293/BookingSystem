using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeSeatConfiguration : IEntityTypeConfiguration<ShowtimeSeat>
{
    public void Configure(EntityTypeBuilder<ShowtimeSeat> builder)
    {
        BookingMapping.Configure(builder, "ShowtimeSeats");
        builder.Ignore(x => x.Label);
        builder.HasAlternateKey(x => new { x.Id, x.ShowtimeId });
        builder.ToTable("ShowtimeSeats", "ordering", table =>
        {
            table.HasCheckConstraint("CK_ShowtimeSeats_Status", "[Status] IN (0,1,2,3)");
            table.HasCheckConstraint("CK_ShowtimeSeats_Position", BookingMapping.PositionCheck);
            table.HasCheckConstraint("CK_ShowtimeSeats_Category", "[Category] IN (0,1,2)");
            table.HasCheckConstraint("CK_ShowtimeSeats_Price", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_ShowtimeSeats_Allocation",
                "([Status] IN (0,3) AND [CurrentReservationId] IS NULL AND [CurrentOrderId] IS NULL AND [HoldExpiresAt] IS NULL) OR "
                + "([Status] = 1 AND [CurrentReservationId] IS NOT NULL AND [CurrentOrderId] IS NULL AND [HoldExpiresAt] IS NOT NULL) OR "
                + "([Status] = 2 AND [CurrentReservationId] IS NOT NULL AND [CurrentOrderId] IS NOT NULL AND [HoldExpiresAt] IS NULL)");
        });
        builder.HasOne(x => x.Showtime).WithMany(x => x.Seats).HasForeignKey(x => x.ShowtimeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CurrentReservation).WithMany()
            .HasForeignKey(x => new { x.CurrentReservationId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CurrentOrder).WithMany()
            .HasForeignKey(x => new { x.CurrentOrderId, x.CurrentReservationId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ReservationId, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.ShowtimeId, x.SeatLayoutSeatId }).IsUnique();
        builder.HasIndex(x => new { x.ShowtimeId, x.SeatId }).IsUnique();
        builder.HasIndex(x => new { x.ShowtimeId, x.Row, x.Column }).IsUnique();
        builder.HasIndex(x => new { x.CurrentReservationId, x.ShowtimeId });
        builder.HasIndex(x => new { x.CurrentOrderId, x.CurrentReservationId, x.ShowtimeId });
        builder.HasIndex(x => new { x.Status, x.HoldExpiresAt });
    }
}
