using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(tenant => tenant.Name)
            .HasColumnType("nvarchar(250)")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(tenant => tenant.Slug)
            .HasColumnName("Slug")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsUnicode(false)
            .IsRequired(false);
        builder.Property(tenant => tenant.LogoUrl)
            .HasColumnName("LogoUrl")
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);
        builder.Property(tenant => tenant.LegalName)
            .HasColumnType("nvarchar(250)")
            .HasMaxLength(250);
        builder.Property(tenant => tenant.TaxCode)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsUnicode(false);
        builder.Property(tenant => tenant.ContactEmail)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256);
        builder.Property(tenant => tenant.ContactPhone)
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsUnicode(false);
        builder.Property(tenant => tenant.Address)
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);
        builder.Property(tenant => tenant.IsActive)
            .HasColumnType("bit")
            .IsRequired();

        builder.HasIndex(tenant => tenant.Slug)
            .IsUnique()
            .HasFilter("[Slug] IS NOT NULL")
            .HasDatabaseName("UX_Tenants_Slug");
    }
}
