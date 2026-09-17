using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        PaymentMapping.Configure(builder, "Refunds");
        builder.ToTable("Refunds", "payment", table =>
        {
            table.HasCheckConstraint("CK_Refunds_Status", "[Status] IN (0,1,2,3,4)");
            table.HasCheckConstraint("CK_Refunds_Amount", "[Amount] > 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_Refunds_Attempts", "[AttemptCount] >= 0");
            table.HasCheckConstraint("CK_Refunds_Lease",
                "([Status] = 1 AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL) OR ([Status] <> 1 AND [LeaseToken] IS NULL AND [LeaseUntil] IS NULL)");
            table.HasCheckConstraint("CK_Refunds_Completion",
                "([Status] = 3 AND [CompletedAt] IS NOT NULL AND [ProviderRefundId] IS NOT NULL) OR ([Status] <> 3 AND [CompletedAt] IS NULL)");
        });
        builder.HasOne(x => x.Payment).WithMany(x => x.Refunds).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.PaymentAttempt).WithMany()
            .HasForeignKey(x => new { x.PaymentId, x.PaymentAttemptId })
            .HasPrincipalKey(x => new { x.PaymentId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.PaymentId, x.PaymentAttemptId });
        builder.HasIndex(x => x.PaymentAttemptId).IsUnique();
        builder.HasIndex(x => x.OrderRefundRequestId).IsUnique().HasFilter("[OrderRefundRequestId] IS NOT NULL");
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.ProviderIdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.ProviderRefundId }).IsUnique().HasFilter("[ProviderRefundId] IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
        builder.HasIndex(x => new { x.Status, x.LeaseUntil });
    }
}
