using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regions", "cinema");

        builder.HasKey(region => region.Id).HasName("PK_Regions");

        builder.Property(region => region.Id)
            .HasColumnName("Id")
            .HasColumnType("integer")
            .ValueGeneratedNever();
        builder.Property(region => region.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(region => region.Slug)
            .HasColumnName("Slug")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired();

        builder.HasIndex(region => region.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Regions_Slug");
    }
}
