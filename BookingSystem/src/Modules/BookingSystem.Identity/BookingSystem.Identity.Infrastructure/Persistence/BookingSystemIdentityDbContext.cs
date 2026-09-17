using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BookingSystem.Identity.Infrastructure.Persistence;

public sealed class BookingSystemIdentityDbContext : DbContext
{
    public BookingSystemIdentityDbContext(DbContextOptions<BookingSystemIdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemIdentityDbContext).Assembly);
        modelBuilder.ApplyApplicationConventions();
    }
}
