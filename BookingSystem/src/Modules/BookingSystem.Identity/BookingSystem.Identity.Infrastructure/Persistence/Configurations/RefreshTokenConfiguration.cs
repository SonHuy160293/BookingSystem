using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "dbo");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasColumnType("binary(32)").IsRequired();
        builder.Property(token => token.RowVersion).IsRowVersion();
        AuthTimestampConfiguration.Configure(builder);
        builder.HasOne<AuthSession>().WithMany().HasForeignKey(token => token.SessionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("UX_RefreshTokens_TokenHash");
        builder.HasIndex(token => token.SessionId).HasDatabaseName("IX_RefreshTokens_SessionId");
    }
}
