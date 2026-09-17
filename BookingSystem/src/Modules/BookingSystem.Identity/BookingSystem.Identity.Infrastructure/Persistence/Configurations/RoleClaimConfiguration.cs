using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.ToTable("RoleClaims", "dbo");

        builder.HasKey(roleClaim => roleClaim.Id);

        builder.Property(roleClaim => roleClaim.Id).UseIdentityColumn();
        builder.Property(roleClaim => roleClaim.RoleId).IsRequired();
        builder.Property(roleClaim => roleClaim.ClaimType).HasColumnType("nvarchar(max)");
        builder.Property(roleClaim => roleClaim.ClaimValue).HasColumnType("nvarchar(max)");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(roleClaim => roleClaim.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RoleClaims_Roles_RoleId");
    }
}
