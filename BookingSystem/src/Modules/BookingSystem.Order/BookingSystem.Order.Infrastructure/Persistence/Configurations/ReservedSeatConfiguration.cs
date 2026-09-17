using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class ReservedSeatConfiguration : IEntityTypeConfiguration<ReservedSeat>
{
    public void Configure(EntityTypeBuilder<ReservedSeat> builder)
    {
        BookingMapping.Configure(builder, "ReservedSeats");
        builder.HasAlternateKey(x => new { x.Id, x.ReservationId, x.ShowtimeId, x.ShowtimeSeatId });
        builder.ToTable("ReservedSeats", "ordering", table => table.HasCheckConstraint("CK_ReservedSeats_Price", "[UnitPrice] >= 0"));
        builder.HasOne(x => x.Reservation).WithMany(x => x.Seats)
            .HasForeignKey(x => new { x.ReservationId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ShowtimeSeat).WithMany()
            .HasForeignKey(x => new { x.ShowtimeSeatId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.ReservationId, x.ShowtimeSeatId }).IsUnique();
        builder.HasIndex(x => new { x.ShowtimeSeatId, x.ShowtimeId });
    }
}
