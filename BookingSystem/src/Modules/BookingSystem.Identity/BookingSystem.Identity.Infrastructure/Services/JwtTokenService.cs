using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookingSystem.Identity.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(User user, IReadOnlyList<UserRole> userRoles, Guid sessionId,
        DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userRoles);

        var section = _configuration.GetSection("Jwt");
        var secret = section["SecretKey"];
        var issuer = section["Issuer"];
        var audience = section["Audience"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32
            || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)
            || sessionId == Guid.Empty || expiresAt <= issuedAt || expiresAt - issuedAt > TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException("JWT requires a secret of at least 32 bytes, issuer, audience, and a positive access token lifetime.");
        }

        var token = new JwtSecurityToken(
            issuer, audience, BuildClaims(user, userRoles, sessionId, issuedAt),
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Claim[] BuildClaims(User user, IReadOnlyList<UserRole> userRoles, Guid sessionId, DateTimeOffset issuedAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(CustomClaims.SessionId, sessionId.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        foreach (var assignment in userRoles)
        {
            if (!string.IsNullOrWhiteSpace(assignment.Role.Name))
            {
                claims.Add(new Claim(ClaimTypes.Role, assignment.Role.Name));
            }

            claims.AddRange(GetPermissions(assignment).Select(permission => new Claim(CustomClaims.Permission, permission)));
        }

        if (userRoles.Count > 0)
        {
            // Summary only: permissions still belong to the individual assignments below.
            var highestScope = userRoles.Any(assignment => assignment.Role.ScopeType == RbacScopeType.Platform)
                ? RbacScopeType.Platform
                : userRoles.Any(assignment => assignment.Role.ScopeType == RbacScopeType.Tenant)
                    ? RbacScopeType.Tenant : RbacScopeType.Branch;
            claims.Add(new Claim("ScopeType", highestScope.ToString().ToUpperInvariant()));
        }

        // Only explicit branch grants: a user's profile BranchId is not an authorization grant.
        var branchIds = userRoles.Where(assignment => assignment.Role.ScopeType == RbacScopeType.Branch && assignment.BranchId.HasValue)
            .Select(assignment => assignment.BranchId!.Value).Distinct().Order().ToArray();
        claims.Add(new Claim(CustomClaims.BranchIds, JsonSerializer.Serialize(branchIds), JsonClaimValueTypes.JsonArray));

        // Branch membership does not imply tenant-wide access.
        var tenantIds = userRoles.Where(assignment => assignment.Role.ScopeType == RbacScopeType.Tenant && assignment.TenantId.HasValue)
            .Select(assignment => assignment.TenantId!.Value).Distinct().Order().ToArray();
        claims.Add(new Claim(CustomClaims.TenantIds, JsonSerializer.Serialize(tenantIds), JsonClaimValueTypes.JsonArray));

        var assignments = userRoles.Select(assignment => new
        {
            assignment.RoleId,
            ScopeType = assignment.Role.ScopeType.ToString().ToUpperInvariant(),
            assignment.TenantId,
            assignment.BranchId,
            Permissions = GetPermissions(assignment)
        }).ToArray();
        claims.Add(new Claim("RoleAssignments", JsonSerializer.Serialize(assignments), JsonClaimValueTypes.JsonArray));

        return claims.DistinctBy(claim => (claim.Type, claim.Value)).ToArray();
    }

    private static string[] GetPermissions(UserRole assignment)
        => assignment.Role.RoleClaims
            .Where(claim => claim.ClaimType == CustomClaims.Permission && !string.IsNullOrWhiteSpace(claim.ClaimValue))
            .Select(claim => claim.ClaimValue!)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
}
