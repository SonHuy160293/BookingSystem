using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class SeatReservation : Entity<Guid>
{
    private readonly List<ReservedSeat> _seats = [];
    private SeatReservation() { }

    public Guid ShowtimeId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? GuestId { get; private set; }
    public string? GuestAccessTokenHash { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConvertedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public int SeatCount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public ShowtimeInventory Showtime { get; private set; } = null!;
    public IReadOnlyCollection<ReservedSeat> Seats => _seats.AsReadOnly();

    internal static SeatReservation Create(ShowtimeInventory showtime, IReadOnlyCollection<ShowtimeSeat> seats,
        Guid? customerId, Guid? guestId, string? guestAccessTokenHash, DateTimeOffset now)
    {
        ModelValidation.Owner(customerId, guestId, guestAccessTokenHash);
        ModelValidation.Require(seats.Count > 0 && seats.All(s => s.ShowtimeId == showtime.Id)
            && seats.Select(s => s.Id).Distinct().Count() == seats.Count, nameof(seats), "Distinct seats from one showtime are required.");
        DateTimeOffset expiresAt = now.ToUniversalTime().AddMinutes(5);
        if (expiresAt > showtime.StartsAt)
        {
            expiresAt = showtime.StartsAt;
        }

        ModelValidation.Require(expiresAt > now, nameof(now), "Showtime has already started.");
        var reservation = new SeatReservation
        {
            Id = Guid.CreateVersion7(),
            ShowtimeId = showtime.Id,
            Showtime = showtime,
            CustomerId = customerId,
            GuestId = guestId,
            GuestAccessTokenHash = guestId.HasValue ? ModelValidation.Hash(guestAccessTokenHash!, nameof(guestAccessTokenHash)) : null,
            Status = ReservationStatus.Active,
            SeatCount = seats.Count,
            ExpiresAt = expiresAt,
            TotalAmount = ModelValidation.Money(seats.Sum(s => s.UnitPrice), nameof(TotalAmount))
        };
        foreach (var seat in seats)
        {
            reservation._seats.Add(ReservedSeat.Create(reservation, seat));
        }

        return reservation;
    }

    internal void EnsureConvertible(DateTimeOffset now)
        => ModelValidation.Require(Status == ReservationStatus.Active && now < ExpiresAt && _seats.Count > 0 && _seats.Count == SeatCount,
            nameof(Status), "A complete, active and unexpired reservation is required.");

    internal void MarkConverted(DateTimeOffset now)
    {
        EnsureConvertible(now);
        ConvertedAt = now.ToUniversalTime();
        Status = ReservationStatus.Converted;
    }

    internal void MarkConfirmed(DateTimeOffset now)
    {
        ModelValidation.Require(Status == ReservationStatus.Converted && now < ExpiresAt,
            nameof(Status), "Reservation cannot be confirmed.");
        ConfirmedAt = now.ToUniversalTime();
        Status = ReservationStatus.Confirmed;
    }

    internal void EnsureReleaseAllowed(DateTimeOffset now, bool expired)
    {
        ModelValidation.Require(Status is ReservationStatus.Active or ReservationStatus.Converted,
            nameof(Status), "Only an unsettled reservation can be released.");
        ModelValidation.Require(!expired || now >= ExpiresAt, nameof(now), "Reservation has not expired.");
    }

    internal void MarkReleased(DateTimeOffset now, bool expired)
    {
        EnsureReleaseAllowed(now, expired);
        ReleasedAt = now.ToUniversalTime();
        Status = expired ? ReservationStatus.Expired : ReservationStatus.Released;
    }
}
