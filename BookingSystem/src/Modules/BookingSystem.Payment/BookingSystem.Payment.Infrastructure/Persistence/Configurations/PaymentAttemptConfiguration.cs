using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        PaymentMapping.Configure(builder, "PaymentAttempts");
        builder.HasAlternateKey(x => new { x.PaymentId, x.Id });
        builder.HasAlternateKey(x => new { x.Id, x.Provider, x.ProviderAccountKey });
        builder.ToTable("PaymentAttempts", "payment", table =>
        {
            table.HasCheckConstraint("CK_PaymentAttempts_Status", "[Status] IN (0,1,2,3,4,5)");
            table.HasCheckConstraint("CK_PaymentAttempts_Amount", "[Amount] > 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_PaymentAttempts_Active",
                "([Status] IN (0,1,2) AND [IsActive] = 1 AND [CompletedAt] IS NULL) OR ([Status] IN (3,4,5) AND [IsActive] = 0 AND [CompletedAt] IS NOT NULL)");
            table.HasCheckConstraint("CK_PaymentAttempts_Success",
                "[Status] <> 3 OR ([ProviderPaymentId] IS NOT NULL AND [ProviderTransactionId] IS NOT NULL AND [FailureCode] IS NULL)");
        });
        builder.HasOne(x => x.Payment).WithMany(x => x.Attempts).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.PaymentId, "UX_PaymentAttempts_Active").IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(x => new { x.PaymentId, x.StartedAt });
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.ProviderIdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.ProviderPaymentId }).IsUnique().HasFilter("[ProviderPaymentId] IS NOT NULL");
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.ProviderTransactionId }).IsUnique().HasFilter("[ProviderTransactionId] IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.StartedAt });
    }
}
