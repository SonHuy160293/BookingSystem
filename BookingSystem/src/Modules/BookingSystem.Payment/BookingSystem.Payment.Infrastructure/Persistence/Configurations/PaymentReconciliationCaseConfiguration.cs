using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentReconciliationCaseConfiguration : IEntityTypeConfiguration<PaymentReconciliationCase>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationCase> builder)
    {
        PaymentMapping.Configure(builder, "PaymentReconciliationCases");
        builder.ToTable("PaymentReconciliationCases", "payment", table =>
        {
            table.HasCheckConstraint("CK_ReconciliationCases_Status", "[Status] IN (0,1,2)");
            table.HasCheckConstraint("CK_ReconciliationCases_Reason", "[Reason] IN (0,1,2,3)");
            table.HasCheckConstraint("CK_ReconciliationCases_Attempts", "[AttemptCount] >= 0");
            table.HasCheckConstraint("CK_ReconciliationCases_Lease",
                "([LeaseToken] IS NULL AND [LeaseUntil] IS NULL) OR ([Status] = 0 AND [LeaseToken] IS NOT NULL AND [LeaseUntil] IS NOT NULL)");
            table.HasCheckConstraint("CK_ReconciliationCases_Resolution",
                "([Status] = 2 AND [ResolvedAt] IS NOT NULL) OR ([Status] <> 2 AND [ResolvedAt] IS NULL)");
        });
        builder.HasOne(x => x.Payment).WithMany(x => x.ReconciliationCases).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.PaymentAttempt).WithMany()
            .HasForeignKey(x => new { x.PaymentId, x.PaymentAttemptId })
            .HasPrincipalKey(x => new { x.PaymentId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.PaymentId, x.PaymentAttemptId });
        builder.HasIndex(x => new { x.PaymentAttemptId, x.Reason }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
        builder.HasIndex(x => new { x.Status, x.LeaseUntil });
    }
}
