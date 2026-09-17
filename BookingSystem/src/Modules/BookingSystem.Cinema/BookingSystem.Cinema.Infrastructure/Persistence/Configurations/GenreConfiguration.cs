using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres", "cinema");

        builder.HasKey(genre => genre.Id).HasName("PK_Genres");

        builder.Property(genre => genre.Id)
            .HasColumnName("Id")
            .HasColumnType("integer")
            .ValueGeneratedNever();
        builder.Property(genre => genre.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(genre => genre.Slug)
            .HasColumnName("Slug")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(genre => genre.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Genres_Slug");
    }
}
