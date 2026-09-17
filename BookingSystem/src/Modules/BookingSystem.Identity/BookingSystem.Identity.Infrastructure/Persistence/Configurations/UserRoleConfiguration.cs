using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "dbo", table =>
        {
            table.HasCheckConstraint("CK_UserRoles_BranchRequiresTenant", "[BranchId] IS NULL OR [TenantId] IS NOT NULL");
        });

        builder.HasKey(userRole => userRole.Id);

        builder.Property(userRole => userRole.Id).ValueGeneratedNever();
        builder.Property(userRole => userRole.UserId).IsRequired();
        builder.Property(userRole => userRole.RoleId).IsRequired();
        builder.Property(userRole => userRole.TenantId).IsRequired(false);
        builder.Property(userRole => userRole.BranchId).IsRequired(false);

        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId, userRole.TenantId, userRole.BranchId })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("UX_UserRoles_UserId_RoleId_TenantId_BranchId");
        builder.HasIndex(userRole => userRole.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");
        builder.HasIndex(userRole => userRole.TenantId)
            .HasDatabaseName("IX_UserRoles_TenantId");
        builder.HasIndex(userRole => new { userRole.BranchId, userRole.TenantId })
            .HasDatabaseName("IX_UserRoles_BranchId_TenantId");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRoles_Users_UserId");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRoles_Roles_RoleId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(userRole => userRole.TenantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserRoles_Tenants_TenantId");

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(userRole => new { userRole.BranchId, userRole.TenantId })
            .HasPrincipalKey(branch => new { branch.Id, branch.TenantId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserRoles_Branches_BranchId_TenantId");
    }
}
