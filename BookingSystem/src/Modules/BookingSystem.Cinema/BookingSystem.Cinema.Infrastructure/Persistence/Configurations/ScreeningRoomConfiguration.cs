using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Configurations;

public sealed class ScreeningRoomConfiguration : IEntityTypeConfiguration<ScreeningRoom>
{
    public void Configure(EntityTypeBuilder<ScreeningRoom> builder)
    {
        builder.ToTable("ScreeningRooms", "cinema");

        builder.HasKey(room => room.Id).HasName("PK_ScreeningRooms");

        builder.Property(room => room.Id)
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(room => room.CinemaId)
            .HasColumnName("CinemaId")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(room => room.Code)
            .HasColumnName("Code")
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(room => room.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(room => room.IsActive)
            .HasColumnName("IsActive")
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasOne(room => room.Cinema)
            .WithMany()
            .HasForeignKey(room => room.CinemaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ScreeningRooms_Cinemas_CinemaId");

        builder.HasIndex(room => new { room.CinemaId, room.Code })
            .IsUnique()
            .HasDatabaseName("UX_ScreeningRooms_CinemaId_Code");
    }
}
