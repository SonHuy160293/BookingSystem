using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class ScreeningRoom : Entity<Guid>
{
    private ScreeningRoom() { }

    private ScreeningRoom(Guid id, Guid cinemaId, string code, string name)
        : base(id)
    {
        CinemaId = cinemaId;
        ChangeDetails(code, name);
        IsActive = true;
    }

    public Guid CinemaId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public Cinema Cinema { get; private set; } = null!;

    public static ScreeningRoom Create(Guid cinemaId, string code, string name)
    {
        ModelValidation.RequiredId(cinemaId, nameof(CinemaId));

        return new ScreeningRoom(Guid.CreateVersion7(), cinemaId, code, name);
    }

    public void ChangeDetails(string code, string name)
    {
        string normalizedCode = ModelValidation.RequiredText(code, 32, nameof(Code)).ToUpperInvariant();
        string normalizedName = ModelValidation.RequiredText(name, 100, nameof(Name));

        Code = normalizedCode;
        Name = normalizedName;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
