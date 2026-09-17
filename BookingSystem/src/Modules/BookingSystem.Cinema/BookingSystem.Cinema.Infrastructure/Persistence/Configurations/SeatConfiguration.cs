using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats", "cinema", table =>
        {
            table.HasCheckConstraint("CK_Seats_Row", "\"Row\" ~ '^[A-Z]$'");
            table.HasCheckConstraint("CK_Seats_Column", "\"Column\" BETWEEN 1 AND 100");
        });

        builder.HasKey(seat => seat.Id).HasName("PK_Seats");

        builder.Property(seat => seat.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(seat => seat.Row)
            .HasColumnName("Row")
            .HasColumnType("varchar(1)")
            .HasMaxLength(1)
            .IsRequired();
        builder.Property(seat => seat.Column)
            .HasColumnName("Column")
            .HasColumnType("integer")
            .IsRequired();

        builder.Ignore(seat => seat.Label);

        builder.HasIndex(seat => new { seat.Row, seat.Column })
            .IsUnique()
            .HasDatabaseName("UX_Seats_Row_Column");
    }
}
