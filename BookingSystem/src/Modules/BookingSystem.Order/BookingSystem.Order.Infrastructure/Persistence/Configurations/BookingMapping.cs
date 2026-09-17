using BookingSystem.SharedKernel.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

internal static class BookingMapping
{
    public const string OwnerCheck = "([CustomerId] IS NOT NULL AND [GuestId] IS NULL AND [GuestAccessTokenHash] IS NULL) OR ([CustomerId] IS NULL AND [GuestId] IS NOT NULL AND [GuestAccessTokenHash] IS NOT NULL)";
    public const string GuestHashCheck = "[GuestAccessTokenHash] IS NULL OR (LEN([GuestAccessTokenHash]) = 64 AND [GuestAccessTokenHash] NOT LIKE '%[^0-9a-f]%')";
    public const string PositionCheck = "[Row] LIKE '[A-Z]' AND [Column] BETWEEN 1 AND 100";

    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : Entity<Guid>
    {
        builder.ToTable(table, "ordering");
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
                mapped.HasPrecision(18, 0);
            }
            else if (type.IsEnum)
            {
                mapped.HasConversion(typeof(short)).HasColumnType("smallint");
            }
            else if (type == typeof(string))
            {
                int length = property.Name switch
                {
                    "Row" => 1,
                    "CurrencyCode" => 3,
                    "ContactEmail" => 320,
                    "CreatedBy" or "UpdatedBy" => 150,
                    "FilmTitle" or "CinemaName" or "RoomName" => 200,
                    _ => 64
                };
                bool ascii = length is 1 or 3 or 64;
                mapped.HasMaxLength(length).IsUnicode(!ascii);
                if (ascii)
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
