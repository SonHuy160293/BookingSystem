using BookingSystem.EntityFrameworkCore.Reliability;
using BookingSystem.Cinema.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Infrastructure.Persistence;

public sealed class BookingSystemCinemaDbContext : DbContext
{
    public BookingSystemCinemaDbContext(DbContextOptions<BookingSystemCinemaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Film> Films => Set<Film>();
    public DbSet<FilmGenre> FilmGenres => Set<FilmGenre>();
    public DbSet<CinemaEntity> Cinemas => Set<CinemaEntity>();
    public DbSet<ScreeningRoom> ScreeningRooms => Set<ScreeningRoom>();
    public DbSet<SeatLayoutVersion> SeatLayoutVersions => Set<SeatLayoutVersion>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatLayoutSeat> SeatLayoutSeats => Set<SeatLayoutSeat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<ShowtimePrice> ShowtimePrices => Set<ShowtimePrice>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingSystemCinemaDbContext).Assembly);
        modelBuilder.ApplyReliabilityConfigurations(ReliabilityDatabaseProvider.PostgreSql);
        modelBuilder.ApplyApplicationConventions();
    }
}
