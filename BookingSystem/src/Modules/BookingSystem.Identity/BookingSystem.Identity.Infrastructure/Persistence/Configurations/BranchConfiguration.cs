using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "dbo");

        builder.HasKey(branch => branch.Id);
        builder.HasAlternateKey(branch => new { branch.Id, branch.TenantId });

        builder.Property(branch => branch.TenantId).IsRequired();
        builder.Property(branch => branch.Name).HasMaxLength(250).IsRequired();
        builder.Property(branch => branch.Address).HasMaxLength(250);

        builder.HasIndex(branch => branch.TenantId)
            .HasDatabaseName("IX_Branches_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(branch => branch.TenantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Branches_Tenants_TenantId");
    }
}
