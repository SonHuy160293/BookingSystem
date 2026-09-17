using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class BookingOrderConfiguration : IEntityTypeConfiguration<BookingOrder>
{
    public void Configure(EntityTypeBuilder<BookingOrder> builder)
    {
        BookingMapping.Configure(builder, "Orders");
        builder.HasAlternateKey(x => new { x.Id, x.ReservationId, x.ShowtimeId });
        builder.ToTable("Orders", "ordering", table =>
        {
            table.HasCheckConstraint("CK_Orders_Status", "[Status] IN (0,1,2,3)");
            table.HasCheckConstraint("CK_Orders_Owner", BookingMapping.OwnerCheck);
            table.HasCheckConstraint("CK_Orders_GuestHash", BookingMapping.GuestHashCheck);
            table.HasCheckConstraint("CK_Orders_Total", "[TotalAmount] > 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_Orders_ItemCount", "[ItemCount] BETWEEN 1 AND 2600");
            table.HasCheckConstraint("CK_Orders_Cancellation",
                "([Status] = 3 AND [CancelledAt] IS NOT NULL AND [CancellationReasonCode] IS NOT NULL) OR ([Status] <> 3 AND [CancelledAt] IS NULL AND [CancellationReasonCode] IS NULL)");
            table.HasCheckConstraint("CK_Orders_Confirmation",
                "(([ConfirmedAt] IS NULL AND [AcceptedPaymentAttemptId] IS NULL) OR "
                + "([ConfirmedAt] IS NOT NULL AND [AcceptedPaymentAttemptId] IS NOT NULL AND [PaymentId] IS NOT NULL)) "
                + "AND ([Status] <> 1 OR [ConfirmedAt] IS NOT NULL) AND ([Status] NOT IN (0,2) OR [ConfirmedAt] IS NULL)");
        });
        builder.HasOne(x => x.Reservation).WithMany()
            .HasForeignKey(x => new { x.ReservationId, x.ShowtimeId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Showtime).WithMany()
            .HasForeignKey(x => new { x.ShowtimeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Tickets).HasField("_tickets").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.RefundRequests).HasField("_refundRequests").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.ReservationId).IsUnique();
        builder.HasIndex(x => x.PaymentId).IsUnique().HasFilter("[PaymentId] IS NOT NULL");
        builder.HasIndex(x => x.AcceptedPaymentAttemptId).IsUnique().HasFilter("[AcceptedPaymentAttemptId] IS NOT NULL");
        builder.HasIndex(x => new { x.ShowtimeId, x.TenantId });
        builder.HasIndex(x => new { x.ShowtimeId, x.Status });
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        builder.HasIndex(x => new { x.GuestId, x.CreatedAt });
    }
}
