using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class SeatLayoutVersion : Entity<Guid>
{
    private readonly List<SeatLayoutSeat> _seats = [];

    private SeatLayoutVersion() { }

    private SeatLayoutVersion(Guid id, Guid screeningRoomId, int versionNumber)
        : base(id)
    {
        ScreeningRoomId = screeningRoomId;
        VersionNumber = versionNumber;
        Status = SeatLayoutStatus.Draft;
    }

    public Guid ScreeningRoomId { get; private set; }
    public int VersionNumber { get; private set; }
    public SeatLayoutStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public ScreeningRoom ScreeningRoom { get; private set; } = null!;
    public IReadOnlyCollection<SeatLayoutSeat> Seats => _seats.AsReadOnly();

    public static SeatLayoutVersion Create(Guid screeningRoomId, int versionNumber)
    {
        ModelValidation.RequiredId(screeningRoomId, nameof(ScreeningRoomId));
        ModelValidation.PositiveId(versionNumber, nameof(VersionNumber));

        return new SeatLayoutVersion(Guid.CreateVersion7(), screeningRoomId, versionNumber);
    }

    public SeatLayoutSeat AddSeat(Seat seat, SeatCategory category)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(seat);
        ModelValidation.RequiredId(seat.Id, nameof(SeatLayoutSeat.SeatId));

        if (_seats.Any(assignment => assignment.SeatId == seat.Id))
        {
            throw ModelValidation.Invalid(nameof(Seats), "This seat position is already selected in this layout.");
        }

        SeatLayoutSeat assignment = SeatLayoutSeat.Create(this, seat, category);
        _seats.Add(assignment);
        return assignment;
    }

    public void ChangeSeatCategory(Guid layoutSeatId, SeatCategory category)
    {
        EnsureDraft();
        SeatLayoutSeat assignment = FindSeat(layoutSeatId);
        assignment.ChangeCategory(category);
    }

    public void RemoveSeat(Guid layoutSeatId)
    {
        EnsureDraft();
        SeatLayoutSeat assignment = FindSeat(layoutSeatId);
        _seats.Remove(assignment);
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureDraft();

        if (_seats.Count == 0)
        {
            throw ModelValidation.Invalid(nameof(Seats), "A seat layout must contain at least one seat assignment before publication.");
        }

        DateTimeOffset normalizedPublishedAt = publishedAt.ToUniversalTime();

        PublishedAt = normalizedPublishedAt;
        Status = SeatLayoutStatus.Published;
    }

    public void Archive()
    {
        if (Status != SeatLayoutStatus.Published)
        {
            throw ModelValidation.Invalid(nameof(Status), "Only a published seat layout can be archived.");
        }

        Status = SeatLayoutStatus.Archived;
    }

    private void EnsureDraft()
    {
        if (Status != SeatLayoutStatus.Draft)
        {
            throw ModelValidation.Invalid(nameof(Status), "Only a draft seat layout can be changed.");
        }
    }

    private SeatLayoutSeat FindSeat(Guid layoutSeatId)
    {
        ModelValidation.RequiredId(layoutSeatId, nameof(SeatLayoutSeat.Id));

        return _seats.Find(assignment => assignment.Id == layoutSeatId)
            ?? throw ModelValidation.Invalid(nameof(Seats), "The seat assignment does not belong to this layout.");
    }
}
