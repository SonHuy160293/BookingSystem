using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims", "dbo");

        builder.HasKey(userClaim => userClaim.Id);

        builder.Property(userClaim => userClaim.Id).UseIdentityColumn();
        builder.Property(userClaim => userClaim.UserId).IsRequired();
        builder.Property(userClaim => userClaim.ClaimType).HasColumnType("nvarchar(max)");
        builder.Property(userClaim => userClaim.ClaimValue).HasColumnType("nvarchar(max)");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userClaim => userClaim.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserClaims_Users_UserId");
    }
}
