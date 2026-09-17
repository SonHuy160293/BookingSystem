using BookingSystem.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence;

public sealed class BookingSystemIdentityDbContext : DbContext
{
    public BookingSystemIdentityDbContext(DbContextOptions<BookingSystemIdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(BookingSystemIdentityDbContext).Assembly);
        builder.ApplyApplicationConventions();
    }
}
