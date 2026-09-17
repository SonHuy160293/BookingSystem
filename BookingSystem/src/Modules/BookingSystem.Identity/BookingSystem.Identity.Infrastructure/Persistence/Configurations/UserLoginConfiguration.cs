using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        builder.ToTable("UserLogins", "dbo");

        builder.HasKey(userLogin => new { userLogin.LoginProvider, userLogin.ProviderKey });

        builder.Property(userLogin => userLogin.LoginProvider).HasMaxLength(450).IsRequired();
        builder.Property(userLogin => userLogin.ProviderKey).HasMaxLength(450).IsRequired();
        builder.Property(userLogin => userLogin.ProviderDisplayName).HasColumnType("nvarchar(max)");
        builder.Property(userLogin => userLogin.UserId).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userLogin => userLogin.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserLogins_Users_UserId");
    }
}
