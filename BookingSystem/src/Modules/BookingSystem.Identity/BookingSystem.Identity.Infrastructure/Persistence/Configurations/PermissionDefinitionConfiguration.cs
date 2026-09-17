using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ToTable("PermissionDefinitions", "dbo");

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
    }
}
