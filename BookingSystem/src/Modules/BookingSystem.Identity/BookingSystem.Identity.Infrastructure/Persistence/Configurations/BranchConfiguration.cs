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

        builder.Property(branch => branch.Name).HasMaxLength(250).IsRequired();
        builder.Property(branch => branch.Address).HasMaxLength(250);
    }
}
