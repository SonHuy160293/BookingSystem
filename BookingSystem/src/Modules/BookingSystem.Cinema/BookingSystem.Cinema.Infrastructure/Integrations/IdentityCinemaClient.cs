using System.Net;
using System.Net.Http.Json;
using BookingSystem.Cinema.Application.Abstractions.Integrations;
using BookingSystem.Cinema.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Cinema.Infrastructure.Integrations;

public sealed class IdentityCinemaClient : IIdentityCinemaClient
{
    private readonly HttpClient _client;

    public IdentityCinemaClient(HttpClient client)
    {
        _client = client;
    }

    public Task<CinemaAccessDto> GetAccessAsync(Guid userId, string permission, CancellationToken cancellationToken)
        => GetAsync<CinemaAccessDto>($"api/v1/integrations/CinemaAccess/users/{userId}?permission={Uri.EscapeDataString(permission)}", cancellationToken);

    public Task<BranchAccessDto> GetBranchAsync(Guid branchId, CancellationToken cancellationToken)
        => GetAsync<BranchAccessDto>($"api/v1/integrations/CinemaAccess/branches/{branchId}", cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Branch.NotFound", "Branch was not found.");
        }

        // Authentication, timeout and upstream failures never become permissive grants.
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<IdentityResponse<T>>(cancellationToken);
        if (envelope is not { Success: true, Data: not null })
        {
            throw new HttpRequestException("Identity returned an invalid access response.");
        }

        return envelope.Data;
    }

    private sealed class IdentityResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
    }
}
