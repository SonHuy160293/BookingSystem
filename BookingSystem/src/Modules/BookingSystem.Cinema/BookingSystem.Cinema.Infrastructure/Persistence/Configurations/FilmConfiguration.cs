using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class FilmConfiguration : IEntityTypeConfiguration<Film>
{
    public void Configure(EntityTypeBuilder<Film> builder)
    {
        builder.ToTable("Films", "cinema", table =>
        {
            table.HasCheckConstraint("CK_Films_DurationMinutes", "\"DurationMinutes\" > 0");
            table.HasCheckConstraint("CK_Films_CountryCodes", "jsonb_typeof(\"CountryCodes\") = 'array'");
            table.HasCheckConstraint("CK_Films_FormatTags", "jsonb_typeof(\"FormatTags\") = 'array'");
            table.HasCheckConstraint("CK_Films_CaptionOptions", "jsonb_typeof(\"CaptionOptions\") = 'array'");
        });

        builder.HasKey(film => film.Id).HasName("PK_Films");

        builder.Property(film => film.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(film => film.Title)
            .HasColumnName("Title")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(film => film.EnglishTitle)
            .HasColumnName("EnglishTitle")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);
        builder.Property(film => film.Synopsis)
            .HasColumnName("Synopsis")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(film => film.EnglishSynopsis)
            .HasColumnName("EnglishSynopsis")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(film => film.DurationMinutes)
            .HasColumnName("DurationMinutes")
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(film => film.ReleaseDate)
            .HasColumnName("ReleaseDate")
            .HasColumnType("date")
            .IsRequired(false);
        builder.Property(film => film.AgeClassificationCode)
            .HasColumnName("AgeClassificationCode")
            .HasColumnType("varchar(16)")
            .HasMaxLength(16)
            .IsRequired(false);
        builder.Property(film => film.PosterUrl)
            .HasColumnName("PosterUrl")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(film => film.BannerUrl)
            .HasColumnName("BannerUrl")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(film => film.TrailerUrl)
            .HasColumnName("TrailerUrl")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(film => film.TrailerVideoUrl)
            .HasColumnName("TrailerVideoUrl")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Ignore(film => film.CountryCodes);
        builder.Ignore(film => film.FormatTags);
        builder.Ignore(film => film.CaptionOptions);

        builder.PrimitiveCollection<string[]>("_countryCodes")
            .HasField("_countryCodes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CountryCodes")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.PrimitiveCollection<string[]>("_formatTags")
            .HasField("_formatTags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("FormatTags")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.PrimitiveCollection<string[]>("_captionOptions")
            .HasField("_captionOptions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CaptionOptions")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.Navigation(film => film.Genres)
            .HasField("_genres")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(film => new { film.ReleaseDate, film.Id })
            .HasDatabaseName("IX_Films_ReleaseDate_Id");
    }
}
