using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        BookingMapping.Configure(builder, "OrderItems");
        builder.Ignore(x => x.Label);
        builder.HasAlternateKey(x => new { x.Id, x.OrderId, x.ShowtimeId, x.ShowtimeSeatId });
        builder.ToTable("OrderItems", "ordering", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Price", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_OrderItems_Position", BookingMapping.PositionCheck);
            table.HasCheckConstraint("CK_OrderItems_Category", "[Category] IN (0,1,2)");
        });
        builder.HasOne(x => x.Order).WithMany(x => x.Items)
            .HasForeignKey(x => new { x.OrderId, x.ReservationId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ReservationId, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ReservedSeat).WithMany()
            .HasForeignKey(x => new { x.ReservedSeatId, x.ReservationId, x.ShowtimeId, x.ShowtimeSeatId })
            .HasPrincipalKey(x => new { x.Id, x.ReservationId, x.ShowtimeId, x.ShowtimeSeatId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ShowtimeSeat).WithMany()
            .HasForeignKey(x => new { x.ShowtimeSeatId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.ReservedSeatId).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.ShowtimeSeatId }).IsUnique();
        builder.HasIndex(x => new { x.ShowtimeSeatId, x.ShowtimeId });
    }
}
