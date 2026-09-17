using BookingSystem.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderEventConfiguration : IEntityTypeConfiguration<PaymentProviderEvent>
{
    public void Configure(EntityTypeBuilder<PaymentProviderEvent> builder)
    {
        PaymentMapping.Configure(builder, "PaymentProviderEvents");
        builder.ToTable("PaymentProviderEvents", "payment", table =>
        {
            table.HasCheckConstraint("CK_ProviderEvents_Status", "[Status] IN (0,1,2,3)");
            table.HasCheckConstraint("CK_ProviderEvents_PayloadHash", "LEN([PayloadHash]) = 64 AND [PayloadHash] NOT LIKE '%[^0-9a-f]%'");
            table.HasCheckConstraint("CK_ProviderEvents_Verified", "[VerifiedAt] >= [ReceivedAt]");
            table.HasCheckConstraint("CK_ProviderEvents_Observed",
                "([ObservedAmount] IS NULL AND [ObservedCurrencyCode] IS NULL) OR ([ObservedAmount] IS NOT NULL AND [ObservedAmount] >= 0 AND [ObservedCurrencyCode] IS NOT NULL)");
            table.HasCheckConstraint("CK_ProviderEvents_Processed",
                "([Status] = 1 AND [ProcessedAt] IS NOT NULL AND [AttemptId] IS NOT NULL) OR ([Status] <> 1 AND [ProcessedAt] IS NULL)");
        });
        builder.HasOne(x => x.Attempt).WithMany()
            .HasForeignKey(x => new { x.AttemptId, x.Provider, x.ProviderAccountKey })
            .HasPrincipalKey(x => new { x.Id, x.Provider, x.ProviderAccountKey }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.Provider, x.ProviderAccountKey, x.EventId }).IsUnique();
        builder.HasIndex(x => new { x.AttemptId, x.Provider, x.ProviderAccountKey });
        builder.HasIndex(x => new { x.Status, x.ReceivedAt });
    }
}
