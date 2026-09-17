using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class Showtime : Entity<Guid>
{
    private readonly List<ShowtimePrice> _prices = [];

    private Showtime() { }

    private Showtime(
        Guid id,
        Guid filmId,
        SeatLayoutVersion layout,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
        : base(id)
    {
        SaleStatus = ShowtimeSaleStatus.Draft;
        ChangeSchedule(filmId, layout, startsAt, endsAt);
    }

    public long Revision { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public Guid FilmId { get; private set; }
    public Guid ScreeningRoomId { get; private set; }
    public Guid SeatLayoutVersionId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public ShowtimeSaleStatus SaleStatus { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public Film Film { get; private set; } = null!;
    public ScreeningRoom ScreeningRoom { get; private set; } = null!;
    public SeatLayoutVersion SeatLayoutVersion { get; private set; } = null!;
    public IReadOnlyCollection<ShowtimePrice> Prices => _prices.AsReadOnly();

    public static Showtime Create(
        Guid filmId,
        SeatLayoutVersion layout,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
        => new(Guid.CreateVersion7(), filmId, layout, startsAt, endsAt);

    public void ChangeSchedule(
        Guid filmId,
        SeatLayoutVersion layout,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(layout);
        ModelValidation.RequiredId(filmId, nameof(FilmId));
        ModelValidation.RequiredId(layout.Id, nameof(SeatLayoutVersionId));
        ModelValidation.RequiredId(layout.ScreeningRoomId, nameof(ScreeningRoomId));

        bool layoutChanged = SeatLayoutVersionId != layout.Id;

        if (layout.Status != SeatLayoutStatus.Published &&
            (layoutChanged || layout.Status != SeatLayoutStatus.Archived))
        {
            throw ModelValidation.Invalid(
                nameof(SeatLayoutVersionId),
                "Newly selected layouts must be published; an already assigned archived layout may be retained.");
        }

        DateTimeOffset normalizedStartsAt = startsAt.ToUniversalTime();
        DateTimeOffset normalizedEndsAt = endsAt.ToUniversalTime();

        if (normalizedEndsAt <= normalizedStartsAt)
        {
            throw ModelValidation.Invalid(nameof(EndsAt), "EndsAt must be later than StartsAt.");
        }

        long nextRevision = checked(Revision + 1);

        if (layoutChanged)
        {
            _prices.Clear();
        }

        if (FilmId != filmId)
        {
            Film = null!;
        }

        if (ScreeningRoomId != layout.ScreeningRoomId)
        {
            ScreeningRoom = null!;
        }

        FilmId = filmId;
        ScreeningRoomId = layout.ScreeningRoomId;
        SeatLayoutVersionId = layout.Id;
        SeatLayoutVersion = layout;
        StartsAt = normalizedStartsAt;
        EndsAt = normalizedEndsAt;
        Revision = nextRevision;
    }

    public void SetPrice(SeatCategory category, decimal amount)
    {
        EnsureDraft();
        ShowtimePrice.ValidateCategory(category);
        long nextRevision = checked(Revision + 1);
        ShowtimePrice? price = _prices.Find(candidate => candidate.SeatCategory == category);

        if (price is null)
        {
            _prices.Add(ShowtimePrice.Create(this, category, amount));
        }
        else
        {
            price.ChangeAmount(amount);
        }

        Revision = nextRevision;
    }

    public void RemovePrice(SeatCategory category)
    {
        EnsureDraft();
        ShowtimePrice.ValidateCategory(category);
        ShowtimePrice? price = _prices.Find(candidate => candidate.SeatCategory == category);

        if (price is not null)
        {
            long nextRevision = checked(Revision + 1);
            _prices.Remove(price);
            Revision = nextRevision;
        }
    }

    /// <summary>
    /// Opens sales after checking prices against the complete set of layout seat assignments.
    /// </summary>
    /// <param name="layout">The assigned layout with its entire Seats assignment collection loaded. Catalog Seat navigations are not required.</param>
    public void OpenSales(SeatLayoutVersion layout)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.Id != SeatLayoutVersionId || layout.ScreeningRoomId != ScreeningRoomId)
        {
            throw ModelValidation.Invalid(
                nameof(SeatLayoutVersionId),
                "The supplied layout must match the showtime's assigned layout and room.");
        }

        if (layout.Status is not (SeatLayoutStatus.Published or SeatLayoutStatus.Archived))
        {
            throw ModelValidation.Invalid(nameof(SeatLayoutVersionId), "The assigned layout must have been published.");
        }

        HashSet<SeatCategory> categories = layout.Seats.Select(assignment => assignment.Category).ToHashSet();

        if (categories.Count == 0)
        {
            throw ModelValidation.Invalid(
                nameof(SeatLayoutVersionId),
                "The assigned layout must include its complete, nonempty seat assignment collection.");
        }

        if (_prices.Count != categories.Count || !categories.SetEquals(_prices.Select(price => price.SeatCategory)))
        {
            throw ModelValidation.Invalid(
                nameof(Prices),
                "Prices must cover exactly the seat categories present in the assigned layout.");
        }

        long nextRevision = checked(Revision + 1);
        SeatLayoutVersion = layout;
        SaleStatus = ShowtimeSaleStatus.OnSale;
        Revision = nextRevision;
    }

    public void CloseSales()
    {
        if (SaleStatus != ShowtimeSaleStatus.OnSale)
        {
            throw ModelValidation.Invalid(nameof(SaleStatus), "Only showtimes on sale can be closed.");
        }

        long nextRevision = checked(Revision + 1);
        SaleStatus = ShowtimeSaleStatus.Closed;
        Revision = nextRevision;
    }

    public void Cancel(string reasonCode, DateTimeOffset cancelledAt)
    {
        if (SaleStatus == ShowtimeSaleStatus.Cancelled)
        {
            return;
        }

        string reason = ModelValidation.RequiredText(reasonCode, 64, nameof(CancellationReasonCode));
        if (reason.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
        {
            throw ModelValidation.Invalid(nameof(CancellationReasonCode), "Cancellation reason must be a safe ASCII code.");
        }

        long nextRevision = checked(Revision + 1);
        CancelledAt = cancelledAt.ToUniversalTime();
        CancellationReasonCode = reason;
        SaleStatus = ShowtimeSaleStatus.Cancelled;
        Revision = nextRevision;
    }

    private void EnsureDraft()
    {
        if (SaleStatus != ShowtimeSaleStatus.Draft)
        {
            throw ModelValidation.Invalid(nameof(SaleStatus), "Only draft showtimes can change their schedule or prices.");
        }
    }
}
