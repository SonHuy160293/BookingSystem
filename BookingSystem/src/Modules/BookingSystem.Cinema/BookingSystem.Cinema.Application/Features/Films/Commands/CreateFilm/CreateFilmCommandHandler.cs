using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.Cinema.Application.Mappings;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Films.Commands.CreateFilm;

public sealed class CreateFilmCommandHandler : ICommandHandler<CreateFilmCommand, FilmDto>
{
    private readonly IFilmRepository _repository;
    private readonly ICinemaAccessService _access;

    public CreateFilmCommandHandler(IFilmRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<FilmDto>> HandleAsync(CreateFilmCommand command, CancellationToken cancellationToken)
    {
        await _access.RequireFilmAccessAsync(ActionType.Create, cancellationToken);
        var request = command.Request;
        var genres = await _repository.GetGenresAsync(request.GenreIds.Distinct().ToArray(), cancellationToken);
        var film = Film.Create(
            request.Title,
            request.DurationMinutes,
            request.ReleaseDate,
            request.EnglishTitle,
            request.Synopsis,
            request.EnglishSynopsis,
            request.AgeClassificationCode);
        film.ChangeMedia(request.PosterUrl, request.BannerUrl, request.TrailerUrl, request.TrailerVideoUrl);
        film.SetCountryCodes(request.CountryCodes);
        film.SetFormatTags(request.FormatTags);
        film.SetCaptionOptions(request.CaptionOptions);
        film.SetGenres(genres);
        await _repository.AddAsync(film, cancellationToken);

        return Result.Success(FilmMapping.ToDto(film));
    }
}
