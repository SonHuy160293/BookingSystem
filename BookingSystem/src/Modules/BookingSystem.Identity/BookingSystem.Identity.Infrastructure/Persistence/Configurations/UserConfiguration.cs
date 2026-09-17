using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName).HasMaxLength(200);
        builder.Property(user => user.JobTitle).HasMaxLength(100);
        builder.Property(user => user.Configuration).HasMaxLength(4000);
        builder.Property(user => user.IsEnabled).IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(256);
        builder.Property(user => user.NormalizedUserName).HasMaxLength(256);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).HasMaxLength(256);
        builder.Property(user => user.EmailConfirmed).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnType("nvarchar(max)");
        builder.Property(user => user.SecurityStamp).HasColumnType("nvarchar(max)");
        builder.Property(user => user.ConcurrencyStamp).HasColumnType("nvarchar(max)");
        builder.Property(user => user.PhoneNumber).HasColumnType("nvarchar(max)");
        builder.Property(user => user.PhoneNumberConfirmed).IsRequired();
        builder.Property(user => user.TwoFactorEnabled).IsRequired();
        builder.Property(user => user.LockoutEnd).HasColumnType("datetimeoffset(7)");
        builder.Property(user => user.LockoutEnabled).IsRequired();
        builder.Property(user => user.AccessFailedCount).IsRequired();
        builder.Property(user => user.AvatarUrl).HasMaxLength(500);
    }
}
