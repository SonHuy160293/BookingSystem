using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.ToTable("Showtimes", "cinema", table =>
        {
            table.HasCheckConstraint("CK_Showtimes_Revision", "\"Revision\" > 0");
            table.HasCheckConstraint("CK_Showtimes_Cancellation",
                "(\"SaleStatus\" = 3 AND \"CancelledAt\" IS NOT NULL AND \"CancellationReasonCode\" IS NOT NULL) OR (\"SaleStatus\" <> 3 AND \"CancelledAt\" IS NULL AND \"CancellationReasonCode\" IS NULL)");
            table.HasCheckConstraint("CK_Showtimes_TimeRange", "\"EndsAt\" > \"StartsAt\"");
            table.HasCheckConstraint("CK_Showtimes_SaleStatus", "\"SaleStatus\" IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_Showtimes_CurrencyCode", "\"CurrencyCode\" = 'VND'");
        });

        builder.HasKey(showtime => showtime.Id).HasName("PK_Showtimes");

        builder.Property(showtime => showtime.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(showtime => showtime.Revision).HasColumnType("bigint").IsRequired().IsConcurrencyToken();
        builder.Property(showtime => showtime.CancelledAt).HasColumnType("timestamp with time zone");
        builder.Property(showtime => showtime.CancellationReasonCode).HasColumnType("varchar(64)").HasMaxLength(64);
        builder.Property(showtime => showtime.FilmId)
            .HasColumnName("FilmId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(showtime => showtime.ScreeningRoomId)
            .HasColumnName("ScreeningRoomId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(showtime => showtime.SeatLayoutVersionId)
            .HasColumnName("SeatLayoutVersionId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(showtime => showtime.StartsAt)
            .HasColumnName("StartsAt")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(showtime => showtime.EndsAt)
            .HasColumnName("EndsAt")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(showtime => showtime.SaleStatus)
            .HasColumnName("SaleStatus")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();
        builder.Property(showtime => showtime.CurrencyCode)
            .HasColumnName("CurrencyCode")
            .HasColumnType("varchar(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.HasOne(showtime => showtime.Film)
            .WithMany()
            .HasForeignKey(showtime => showtime.FilmId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Showtimes_Films_FilmId");
        builder.HasOne(showtime => showtime.ScreeningRoom)
            .WithMany()
            .HasForeignKey(showtime => showtime.ScreeningRoomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Showtimes_ScreeningRooms_ScreeningRoomId");
        builder.HasOne(showtime => showtime.SeatLayoutVersion)
            .WithMany()
            .HasForeignKey(showtime => new { showtime.SeatLayoutVersionId, showtime.ScreeningRoomId })
            .HasPrincipalKey(layout => new { layout.Id, layout.ScreeningRoomId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Showtimes_SeatLayoutVersions_LayoutId_RoomId");

        builder.Navigation(showtime => showtime.Prices)
            .HasField("_prices")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(showtime => new { showtime.ScreeningRoomId, showtime.StartsAt })
            .HasDatabaseName("IX_Showtimes_ScreeningRoomId_StartsAt");
        builder.HasIndex(showtime => new { showtime.FilmId, showtime.StartsAt })
            .HasDatabaseName("IX_Showtimes_FilmId_StartsAt");
        builder.HasIndex(showtime => new { showtime.SeatLayoutVersionId, showtime.ScreeningRoomId })
            .HasDatabaseName("IX_Showtimes_SeatLayoutVersionId_ScreeningRoomId");
    }
}
