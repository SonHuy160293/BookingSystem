using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class ShowtimeInventory : Entity<Guid>
{
    private readonly List<ShowtimeSeat> _seats = [];

    private ShowtimeInventory() { }

    public Guid TenantId { get; private set; }
    public Guid CinemaId { get; private set; }
    public Guid FilmId { get; private set; }
    public Guid ScreeningRoomId { get; private set; }
    public Guid SeatLayoutVersionId { get; private set; }
    public string FilmTitle { get; private set; } = null!;
    public string CinemaName { get; private set; } = null!;
    public string RoomName { get; private set; } = null!;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public long SourceRevision { get; private set; }
    public long AvailabilityRevision { get; private set; }
    public int ExpectedSeatCount { get; private set; }
    public InventoryStatus Status { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public IReadOnlyCollection<ShowtimeSeat> Seats => _seats.AsReadOnly();

    public static ShowtimeInventory Create(Guid showtimeId, Guid tenantId, Guid cinemaId, Guid filmId,
        Guid roomId, Guid layoutId, string filmTitle, string cinemaName, string roomName,
        DateTimeOffset startsAt, DateTimeOffset endsAt, long sourceRevision, int expectedSeatCount)
    {
        ModelValidation.Require(endsAt > startsAt, nameof(endsAt), "End time must follow start time.");
        ModelValidation.Require(sourceRevision > 0 && expectedSeatCount >= 0 && expectedSeatCount <= 2600,
            nameof(sourceRevision), "Source revision and seat count are invalid.");
        return new ShowtimeInventory
        {
            Id = ModelValidation.Id(showtimeId, nameof(showtimeId)),
            TenantId = ModelValidation.Id(tenantId, nameof(tenantId)),
            CinemaId = ModelValidation.Id(cinemaId, nameof(cinemaId)),
            FilmId = ModelValidation.Id(filmId, nameof(filmId)),
            ScreeningRoomId = ModelValidation.Id(roomId, nameof(roomId)),
            SeatLayoutVersionId = ModelValidation.Id(layoutId, nameof(layoutId)),
            FilmTitle = ModelValidation.Text(filmTitle, 200, nameof(filmTitle)),
            CinemaName = ModelValidation.Text(cinemaName, 200, nameof(cinemaName)),
            RoomName = ModelValidation.Text(roomName, 200, nameof(roomName)),
            StartsAt = startsAt.ToUniversalTime(),
            EndsAt = endsAt.ToUniversalTime(),
            SourceRevision = sourceRevision,
            ExpectedSeatCount = expectedSeatCount,
            Status = InventoryStatus.Initializing
        };
    }

    public ShowtimeSeat AddSeat(Guid layoutSeatId, Guid catalogSeatId, string row, int column,
        OrderSeatCategory category, decimal unitPrice)
    {
        ModelValidation.Require(Status == InventoryStatus.Initializing, nameof(Status), "Only initializing inventory can receive seat snapshots.");
        ModelValidation.Require(_seats.Count < ExpectedSeatCount, nameof(Seats), "The expected seat count has been reached.");
        ModelValidation.Require(!_seats.Any(s => s.SeatLayoutSeatId == layoutSeatId || s.SeatId == catalogSeatId),
            nameof(Seats), "The position is already present.");
        ShowtimeSeat seat = ShowtimeSeat.Create(this, layoutSeatId, catalogSeatId, row, column, category, unitPrice);
        ModelValidation.Require(!_seats.Any(s => s.Row == seat.Row && s.Column == seat.Column), nameof(Seats), "The position is already present.");
        _seats.Add(seat);
        return seat;
    }

    public void ApplySourceState(long sourceRevision, InventoryStatus status, DateTimeOffset? cancelledAt = null, string? reasonCode = null)
    {
        ModelValidation.Require(sourceRevision > 0 && Enum.IsDefined(status) && status != InventoryStatus.Initializing,
            nameof(status), "A valid source revision and sale state are required.");
        if (Status == InventoryStatus.Cancelled || sourceRevision < SourceRevision
            || (sourceRevision == SourceRevision && Status != InventoryStatus.Initializing))
        {
            return;
        }

        string? reason = status == InventoryStatus.Cancelled
            ? ModelValidation.Code(reasonCode!, nameof(reasonCode)) : null;
        ModelValidation.Require(status != InventoryStatus.Cancelled || cancelledAt.HasValue, nameof(cancelledAt), "Cancellation timestamp is required.");
        if (status == InventoryStatus.OnSale)
        {
            ModelValidation.Require(_seats.Count > 0 && _seats.Count == ExpectedSeatCount, nameof(Seats), "The full seat snapshot is required.");
            ModelValidation.Require(Status != InventoryStatus.Closed, nameof(Status), "Closed inventory cannot reopen.");
        }

        long revision = checked(AvailabilityRevision + 1);
        SourceRevision = sourceRevision;
        Status = status;
        CancelledAt = status == InventoryStatus.Cancelled ? cancelledAt!.Value.ToUniversalTime() : null;
        CancellationReasonCode = reason;
        AvailabilityRevision = revision;
    }

    public SeatReservation Reserve(IEnumerable<Guid> seatIds, Guid? customerId, Guid? guestId,
        string? guestAccessTokenHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(seatIds);
        ModelValidation.Require(Status == InventoryStatus.OnSale && now < StartsAt, nameof(Status), "Showtime is not available for new reservations.");
        Guid[] ids = seatIds.ToArray();
        ModelValidation.Require(ids.Length > 0 && ids.Distinct().Count() == ids.Length, nameof(seatIds), "Select distinct, nonempty seats.");
        var selected = ids.Select(FindSeat).ToArray();
        ModelValidation.Require(selected.All(s => s.Status == SeatAllocationStatus.Available), nameof(seatIds), "A selected seat is unavailable.");
        long revision = checked(AvailabilityRevision + 1);
        SeatReservation reservation = SeatReservation.Create(this, selected, customerId, guestId, guestAccessTokenHash, now);
        foreach (var seat in selected)
        {
            seat.Hold(reservation);
        }

        AvailabilityRevision = revision;
        return reservation;
    }

    public void ReleaseReservation(SeatReservation reservation, DateTimeOffset now, bool expired)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ModelValidation.Require(reservation.ShowtimeId == Id, nameof(reservation), "Reservation belongs to another showtime.");
        if (reservation.Status is ReservationStatus.Expired or ReservationStatus.Released)
        {
            return;
        }

        reservation.EnsureReleaseAllowed(now, expired);
        ShowtimeSeat[] selected = reservation.Seats.Select(s => FindSeat(s.ShowtimeSeatId)).ToArray();
        ModelValidation.Require(selected.Length == reservation.SeatCount && selected.Length > 0 && selected.All(s => s.IsHeldBy(reservation.Id)),
            nameof(reservation), "The complete reservation must still own its selected seats.");
        long revision = checked(AvailabilityRevision + 1);
        foreach (var seat in selected)
        {
            seat.Release(reservation.Id);
        }

        reservation.MarkReleased(now, expired);
        AvailabilityRevision = revision;
    }

    internal bool CanFulfill(SeatReservation reservation, DateTimeOffset now)
        => reservation.ShowtimeId == Id && (Status is InventoryStatus.OnSale or InventoryStatus.Closed)
            && now < StartsAt && now < reservation.ExpiresAt && reservation.Status == ReservationStatus.Converted
            && reservation.Seats.Count > 0 && reservation.Seats.Count == reservation.SeatCount && reservation.Seats.All(reserved =>
                _seats.Any(seat => seat.Id == reserved.ShowtimeSeatId && seat.IsHeldBy(reservation.Id)
                    && seat.HoldExpiresAt == reservation.ExpiresAt));

    internal void Sell(SeatReservation reservation, BookingOrder order, DateTimeOffset now)
    {
        ModelValidation.Require(order.ReservationId == reservation.Id && order.ShowtimeId == Id && CanFulfill(reservation, now),
            nameof(reservation), "Reservation is not eligible for fulfillment.");
        long revision = checked(AvailabilityRevision + 1);
        foreach (var reserved in reservation.Seats)
        {
            FindSeat(reserved.ShowtimeSeatId).MarkSold(reservation.Id, order);
        }

        reservation.MarkConfirmed(now);
        AvailabilityRevision = revision;
    }

    public void BlockSeat(Guid seatId, bool blocked)
    {
        ShowtimeSeat seat = FindSeat(seatId);
        ModelValidation.Require(Status is InventoryStatus.OnSale or InventoryStatus.Closed, nameof(Status), "Inventory is not active.");
        ModelValidation.Require(seat.Status is SeatAllocationStatus.Available or SeatAllocationStatus.Blocked, nameof(seatId), "Held or sold seats cannot be blocked.");
        long revision = checked(AvailabilityRevision + 1);
        seat.SetBlocked(blocked);
        AvailabilityRevision = revision;
    }

    internal ShowtimeSeat FindSeat(Guid id)
        => _seats.Find(seat => seat.Id == id)
            ?? throw new BookingSystem.SharedKernel.Exceptions.ValidationException(
                [new BookingSystem.SharedKernel.Exceptions.ValidationError(nameof(id), "Seat does not belong to this showtime.")]);
}
