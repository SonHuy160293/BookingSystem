using System.Globalization;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class Seat : Entity<Guid>
{
    private Seat() { }

    private Seat(Guid id, string row, int column)
        : base(id)
    {
        Row = row;
        Column = column;
    }

    public string Row { get; private set; } = null!;
    public int Column { get; private set; }
    public string Label => Row + Column.ToString(CultureInfo.InvariantCulture);

    public static Seat Create(Guid id, string row, int column)
    {
        ModelValidation.RequiredId(id, nameof(Id));
        string normalizedRow = ModelValidation.RequiredText(row, 1, nameof(Row));

        if (normalizedRow.Length != 1 || !char.IsAsciiLetter(normalizedRow[0]))
        {
            throw ModelValidation.Invalid(nameof(Row), "Row must be one ASCII letter from A to Z.");
        }

        if (column is < 1 or > 100)
        {
            throw ModelValidation.Invalid(nameof(Column), "Column must be between 1 and 100.");
        }

        return new Seat(id, normalizedRow.ToUpperInvariant(), column);
    }
}
