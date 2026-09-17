using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class SeatLayoutSeatConfiguration : IEntityTypeConfiguration<SeatLayoutSeat>
{
    public void Configure(EntityTypeBuilder<SeatLayoutSeat> builder)
    {
        builder.ToTable("SeatLayoutSeats", "cinema", table =>
        {
            table.HasCheckConstraint("CK_SeatLayoutSeats_Category", "\"Category\" IN (0, 1, 2)");
        });

        builder.HasKey(assignment => assignment.Id).HasName("PK_SeatLayoutSeats");

        builder.Property(assignment => assignment.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(assignment => assignment.SeatLayoutVersionId)
            .HasColumnName("SeatLayoutVersionId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(assignment => assignment.SeatId)
            .HasColumnName("SeatId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(assignment => assignment.Category)
            .HasColumnName("Category")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.HasOne(assignment => assignment.SeatLayoutVersion)
            .WithMany(layout => layout.Seats)
            .HasForeignKey(assignment => assignment.SeatLayoutVersionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SeatLayoutSeats_SeatLayoutVersions_SeatLayoutVersionId");

        builder.HasOne(assignment => assignment.Seat)
            .WithMany()
            .HasForeignKey(assignment => assignment.SeatId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SeatLayoutSeats_Seats_SeatId");

        builder.HasIndex(assignment => new { assignment.SeatLayoutVersionId, assignment.SeatId })
            .IsUnique()
            .HasDatabaseName("UX_SeatLayoutSeats_SeatLayoutVersionId_SeatId");
        builder.HasIndex(assignment => assignment.SeatId)
            .HasDatabaseName("IX_SeatLayoutSeats_SeatId");
    }
}
