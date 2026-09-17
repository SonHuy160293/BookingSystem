using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class FilmGenreConfiguration : IEntityTypeConfiguration<FilmGenre>
{
    public void Configure(EntityTypeBuilder<FilmGenre> builder)
    {
        builder.ToTable("FilmGenres", "cinema");

        builder.HasKey(filmGenre => new { filmGenre.FilmId, filmGenre.GenreId })
            .HasName("PK_FilmGenres");

        builder.Property(filmGenre => filmGenre.FilmId)
            .HasColumnName("FilmId")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(filmGenre => filmGenre.GenreId)
            .HasColumnName("GenreId")
            .HasColumnType("integer")
            .ValueGeneratedNever();

        builder.HasOne(filmGenre => filmGenre.Film)
            .WithMany(film => film.Genres)
            .HasForeignKey(filmGenre => filmGenre.FilmId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FilmGenres_Films_FilmId");
        builder.HasOne(filmGenre => filmGenre.Genre)
            .WithMany()
            .HasForeignKey(filmGenre => filmGenre.GenreId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FilmGenres_Genres_GenreId");

        builder.HasIndex(filmGenre => new { filmGenre.GenreId, filmGenre.FilmId })
            .HasDatabaseName("IX_FilmGenres_GenreId_FilmId");
    }
}
