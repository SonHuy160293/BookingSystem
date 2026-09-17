using BookingSystem.SharedKernel.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Payment.Infrastructure.Persistence.Configurations;

internal static class PaymentMapping
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : Entity<Guid>
    {
        builder.ToTable(table, "payment");
        builder.HasKey(entity => entity.Id);
        foreach (var property in builder.Metadata.GetProperties().ToArray())
        {
            Type type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            var mapped = builder.Property(property.Name);
            if (type == typeof(Guid))
            {
                mapped.HasColumnType("uniqueidentifier").ValueGeneratedNever();
            }
            else if (type == typeof(DateTimeOffset))
            {
                mapped.HasColumnType("datetimeoffset(7)");
            }
            else if (type == typeof(decimal))
            {
                if (property.Name == "ObservedAmount")
                {
                    mapped.HasPrecision(24, 6);
                }
                else
                {
                    mapped.HasPrecision(18, 0);
                }
            }
            else if (type.IsEnum)
            {
                mapped.HasConversion(typeof(short)).HasColumnType("smallint");
            }
            else if (type == typeof(string))
            {
                int length = property.Name switch
                {
                    "Provider" => 32,
                    "ProviderAccountKey" => 64,
                    "CurrencyCode" or "ObservedCurrencyCode" => 3,
                    "CreatedBy" or "UpdatedBy" => 150,
                    "ProviderPaymentId" or "ProviderTransactionId" or "ProviderRefundId" or "EventId" or "EventType" => 200,
                    _ => 64
                };
                bool unicode = length is 150 or 200 || property.Name == "ProviderAccountKey";
                mapped.HasMaxLength(length).IsUnicode(unicode);
                if (property.Name is not ("CreatedBy" or "UpdatedBy"))
                {
                    mapped.UseCollation("Latin1_General_100_BIN2");
                }

                if (property.Name.EndsWith("Hash", StringComparison.Ordinal))
                {
                    mapped.IsFixedLength();
                }
            }
        }

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
