using BookingSystem.Identity.Domain.Enums;
using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ToTable("PermissionDefinitions", "dbo", table =>
        {
            table.HasCheckConstraint("CK_PermissionDefinitions_ScopeType", "[ScopeType] IN ('PLATFORM', 'TENANT', 'BRANCH')");
        });

        builder.HasKey(permissionDefinition => permissionDefinition.Id);

        builder.Property(permissionDefinition => permissionDefinition.Function)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.Action)
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.Value)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.GroupName)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.Description)
            .HasMaxLength(500);
        builder.Property(permissionDefinition => permissionDefinition.DisplayOrder)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.IsEnabled)
            .IsRequired();
        builder.Property(permissionDefinition => permissionDefinition.ScopeType)
            .HasConversion(
                scopeType => scopeType.ToString().ToUpperInvariant(),
                value => Enum.Parse<RbacScopeType>(value, true))
            .HasColumnType("varchar(8)")
            .HasMaxLength(8)
            .IsUnicode(false)
            .IsRequired();
    }
}
