using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence.Configurations;

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("AuthSessions", "dbo");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.ClientId).HasMaxLength(128).IsRequired();
        builder.Property(session => session.DeviceName).HasMaxLength(128);
        builder.Property(session => session.SecurityStampAtLogin).HasColumnType("nvarchar(max)");
        builder.Property(session => session.RevokedReason).HasMaxLength(64);
        builder.Property(session => session.RowVersion).IsRowVersion();
        AuthTimestampConfiguration.Configure(builder);
        builder.HasOne<User>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(session => session.UserId).HasDatabaseName("IX_AuthSessions_UserId");
        builder.HasIndex(session => new { session.AbsoluteExpiresAt, session.UserId }).HasDatabaseName("IX_AuthSessions_Cleanup");

        // Scalar pointer: a reverse FK would make initial session/token insertion cyclic.
        builder.Property(session => session.CurrentRefreshTokenId).IsRequired();
    }
}
