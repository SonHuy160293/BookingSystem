using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

internal static class AuthTimestampConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder)
       
    where TEntity : class
    {
        var converter = new ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var property in typeof(TEntity).GetProperties().Where(property => property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime? )))
        {
            builder.Property(property.Name).HasColumnType("datetime2(7)").HasConversion(converter);
        }
    }
}
