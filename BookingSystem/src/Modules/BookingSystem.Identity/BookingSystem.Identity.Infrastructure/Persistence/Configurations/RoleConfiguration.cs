using BookingSystem.Identity.Domain.Enums;
using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Roles_ScopeType", "[ScopeType] IN ('PLATFORM', 'TENANT', 'BRANCH')");
            table.HasCheckConstraint("CK_Roles_PlatformTenant", "[ScopeType] <> 'PLATFORM' OR [TenantId] IS NULL");
        });

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Description).HasMaxLength(250);
        builder.Property(role => role.Name).HasMaxLength(256);
        builder.Property(role => role.NormalizedName).HasMaxLength(256);
        builder.Property(role => role.ConcurrencyStamp).HasColumnType("nvarchar(max)");
        builder.Property(role => role.TenantId)
            .IsRequired(false)
            .HasComment("NULL means a reusable role definition available across the platform; ScopeType determines assignment scope.");
        builder.Property(role => role.ScopeType)
            .HasConversion(
                scopeType => scopeType.ToString().ToUpperInvariant(),
                value => Enum.Parse<RbacScopeType>(value, true))
            .HasColumnType("varchar(8)")
            .HasMaxLength(8)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(role => new { role.TenantId, role.ScopeType, role.NormalizedName })
            .IsUnique()
            .HasFilter("[NormalizedName] IS NOT NULL")
            .HasDatabaseName("UX_Roles_TenantId_ScopeType_NormalizedName");
        builder.HasIndex(role => role.TenantId)
            .HasDatabaseName("IX_Roles_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(role => role.TenantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Roles_Tenants_TenantId");
    }
}
