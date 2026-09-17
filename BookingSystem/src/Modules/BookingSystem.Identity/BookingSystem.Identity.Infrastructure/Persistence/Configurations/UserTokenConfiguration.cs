using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens", "dbo");

        builder.HasKey(userToken => new { userToken.UserId, userToken.LoginProvider, userToken.Name });

        builder.Property(userToken => userToken.UserId).IsRequired();
        builder.Property(userToken => userToken.LoginProvider).HasMaxLength(450).IsRequired();
        builder.Property(userToken => userToken.Name).HasMaxLength(450).IsRequired();
        builder.Property(userToken => userToken.Value).HasColumnType("nvarchar(max)");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userToken => userToken.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserTokens_Users_UserId");
    }
}
