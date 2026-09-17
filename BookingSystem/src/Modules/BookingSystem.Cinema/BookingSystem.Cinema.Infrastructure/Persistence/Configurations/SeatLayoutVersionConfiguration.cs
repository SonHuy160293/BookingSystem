using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class SeatLayoutVersionConfiguration : IEntityTypeConfiguration<SeatLayoutVersion>
{
    public void Configure(EntityTypeBuilder<SeatLayoutVersion> builder)
    {
        builder.ToTable("SeatLayoutVersions", "cinema", table =>
        {
            table.HasCheckConstraint("CK_SeatLayoutVersions_VersionNumber", "\"VersionNumber\" > 0");
            table.HasCheckConstraint("CK_SeatLayoutVersions_Status", "\"Status\" IN (0, 1, 2)");
            table.HasCheckConstraint(
                "CK_SeatLayoutVersions_PublishedAt",
                "(\"Status\" = 0 AND \"PublishedAt\" IS NULL) OR (\"Status\" IN (1, 2) AND \"PublishedAt\" IS NOT NULL)");
        });

        builder.HasKey(layout => layout.Id).HasName("PK_SeatLayoutVersions");
        builder.HasAlternateKey(layout => new { layout.Id, layout.ScreeningRoomId })
            .HasName("AK_SeatLayoutVersions_Id_ScreeningRoomId");

        builder.Property(layout => layout.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(layout => layout.ScreeningRoomId)
            .HasColumnName("ScreeningRoomId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(layout => layout.VersionNumber)
            .HasColumnName("VersionNumber")
            .HasColumnType("integer")
            .IsRequired();
        builder.Property(layout => layout.Status)
            .HasColumnName("Status")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(layout => layout.PublishedAt)
            .HasColumnName("PublishedAt")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasOne(layout => layout.ScreeningRoom)
            .WithMany()
            .HasForeignKey(layout => layout.ScreeningRoomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SeatLayoutVersions_ScreeningRooms_ScreeningRoomId");

        builder.Navigation(layout => layout.Seats)
            .HasField("_seats")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(layout => new { layout.ScreeningRoomId, layout.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_SeatLayoutVersions_ScreeningRoomId_VersionNumber");
    }
}
