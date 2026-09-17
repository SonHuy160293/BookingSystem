using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class CinemaConfiguration : IEntityTypeConfiguration<CinemaEntity>
{
    public void Configure(EntityTypeBuilder<CinemaEntity> builder)
    {
        builder.ToTable("Cinemas", "cinema", table =>
        {
            table.HasCheckConstraint("CK_Cinemas_PublicationStatus", "\"PublicationStatus\" IN (0, 1, 2)");
            table.HasCheckConstraint("CK_Cinemas_OperatingStatus", "\"OperatingStatus\" IN (0, 1, 2)");
            table.HasCheckConstraint("CK_Cinemas_Latitude", "\"Latitude\" BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_Cinemas_Longitude", "\"Longitude\" BETWEEN -180 AND 180");
            table.HasCheckConstraint("CK_Cinemas_Coordinates", "(\"Latitude\" IS NULL) = (\"Longitude\" IS NULL)");
        });

        builder.HasKey(cinema => cinema.Id).HasName("PK_Cinemas");

        builder.Property(cinema => cinema.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(cinema => cinema.TenantId)
            .HasColumnName("TenantId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(cinema => cinema.RegionId)
            .HasColumnName("RegionId")
            .HasColumnType("integer")
            .IsRequired();
        builder.Property(cinema => cinema.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(cinema => cinema.Slug)
            .HasColumnName("Slug")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(cinema => cinema.Address)
            .HasColumnName("Address")
            .HasColumnType("varchar(1000)")
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(cinema => cinema.Description)
            .HasColumnName("Description")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(cinema => cinema.Latitude)
            .HasColumnName("Latitude")
            .HasColumnType("numeric(10,7)")
            .HasPrecision(10, 7)
            .IsRequired(false);
        builder.Property(cinema => cinema.Longitude)
            .HasColumnName("Longitude")
            .HasColumnType("numeric(10,7)")
            .HasPrecision(10, 7)
            .IsRequired(false);
        builder.Property(cinema => cinema.PublicationStatus)
            .HasColumnName("PublicationStatus")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(cinema => cinema.OperatingStatus)
            .HasColumnName("OperatingStatus")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(cinema => cinema.OnlineBookingEnabled)
            .HasColumnName("OnlineBookingEnabled")
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasOne(cinema => cinema.Region)
            .WithMany()
            .HasForeignKey(cinema => cinema.RegionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Cinemas_Regions_RegionId");

        builder.HasIndex(cinema => cinema.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Cinemas_Slug");
        builder.HasIndex(cinema => new { cinema.TenantId, cinema.RegionId })
            .HasDatabaseName("IX_Cinemas_TenantId_RegionId");
        builder.HasIndex(cinema => cinema.RegionId)
            .HasDatabaseName("IX_Cinemas_RegionId");
    }
}
