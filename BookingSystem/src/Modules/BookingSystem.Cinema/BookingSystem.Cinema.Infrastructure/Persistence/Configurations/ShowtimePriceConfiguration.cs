using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class ShowtimePriceConfiguration : IEntityTypeConfiguration<ShowtimePrice>
{
    public void Configure(EntityTypeBuilder<ShowtimePrice> builder)
    {
        builder.ToTable("ShowtimePrices", "cinema", table =>
        {
            table.HasCheckConstraint("CK_ShowtimePrices_SeatCategory", "\"SeatCategory\" IN (0, 1, 2)");
            table.HasCheckConstraint("CK_ShowtimePrices_Amount", "\"Amount\" >= 0");
        });

        builder.HasKey(price => price.Id).HasName("PK_ShowtimePrices");

        builder.Property(price => price.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(price => price.ShowtimeId)
            .HasColumnName("ShowtimeId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(price => price.SeatCategory)
            .HasColumnName("SeatCategory")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(price => price.Amount)
            .HasColumnName("Amount")
            .HasColumnType("numeric(18,0)")
            .HasPrecision(18, 0)
            .IsRequired();

        builder.HasOne(price => price.Showtime)
            .WithMany(showtime => showtime.Prices)
            .HasForeignKey(price => price.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ShowtimePrices_Showtimes_ShowtimeId");

        builder.HasIndex(price => new { price.ShowtimeId, price.SeatCategory })
            .IsUnique()
            .HasDatabaseName("UX_ShowtimePrices_ShowtimeId_SeatCategory");
    }
}
