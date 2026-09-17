using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class OrderRefundRequestConfiguration : IEntityTypeConfiguration<OrderRefundRequest>
{
    public void Configure(EntityTypeBuilder<OrderRefundRequest> builder)
    {
        BookingMapping.Configure(builder, "OrderRefundRequests");
        builder.ToTable("OrderRefundRequests", "ordering", table =>
        {
            table.HasCheckConstraint("CK_OrderRefundRequests_Status", "[Status] IN (0,1,2)");
            table.HasCheckConstraint("CK_OrderRefundRequests_Reason", "[Reason] IN (0,1,2)");
            table.HasCheckConstraint("CK_OrderRefundRequests_Amount", "[Amount] > 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_OrderRefundRequests_Completion",
                "([Status] = 1 AND [CompletedAt] IS NOT NULL) OR ([Status] <> 1 AND [CompletedAt] IS NULL)");
        });
        builder.HasOne(x => x.Order).WithMany(x => x.RefundRequests).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.PaymentAttemptId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.RequestedAt });
    }
}
