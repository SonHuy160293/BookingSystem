using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = BookingSystem.Payment.Domain.Models.Payment;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        PaymentMapping.Configure(builder, "Payments");
        builder.ToTable("Payments", "payment", table =>
        {
            table.HasCheckConstraint("CK_Payments_Status", "[Status] IN (0,1,2)");
            table.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0 AND [CurrencyCode] = 'VND'");
            table.HasCheckConstraint("CK_Payments_Counts", "[AttemptCount] >= 0 AND [RefundCount] >= 0 AND [ReconciliationCaseCount] >= 0 AND [Revision] > 0");
            table.HasCheckConstraint("CK_Payments_State",
                "([Status] = 0 AND [AcceptedAttemptId] IS NULL AND [PaidAt] IS NULL AND [ClosedAt] IS NULL) OR "
                + "([Status] = 1 AND [AcceptedAttemptId] IS NOT NULL AND [PaidAt] IS NOT NULL AND [ClosedAt] IS NULL) OR "
                + "([Status] = 2 AND [AcceptedAttemptId] IS NULL AND [PaidAt] IS NULL AND [ClosedAt] IS NOT NULL)");
        });
        builder.HasOne(x => x.AcceptedAttempt).WithMany()
            .HasForeignKey(x => new { x.Id, x.AcceptedAttemptId })
            .HasPrincipalKey(x => new { x.PaymentId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(x => x.Attempts).HasField("_attempts").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Refunds).HasField("_refunds").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ReconciliationCases).HasField("_reconciliationCases").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => new { x.Id, x.AcceptedAttemptId });
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}
