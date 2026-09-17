using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence;
using BookingSystem.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace Company.Project.Infrastructure.Seed;

public static class PermissionDefinitionSeeder
{
    public static async Task SyncAsync(BookingSystemIdentityDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var definitions = await dbContext.PermissionDefinitions
            .ToDictionaryAsync(permission => permission.Value, StringComparer.Ordinal, cancellationToken);
        var catalog = PermissionCatalog.Groups
            .SelectMany((group, groupIndex) => group.Permissions.Select((permission, permissionIndex) => new
            {
                group.Function,
                Action = permission.Action.ToString(),
                permission.Name,
                permission.Value,
                group.GroupName,
                permission.Description,
                DisplayOrder = (groupIndex * 100) + permissionIndex
            }))
            .ToArray();
        var catalogValues = catalog
            .Select(permission => permission.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in catalog)
        {
            if (definitions.TryGetValue(permission.Value, out var existing))
            {
                existing.Sync(
                    permission.Function,
                    permission.Action,
                    permission.Name,
                    permission.GroupName,
                    permission.Description,
                    permission.DisplayOrder);
                continue;
            }

            await dbContext.PermissionDefinitions.AddAsync(
                PermissionDefinition.Create(
                    permission.Function,
                    permission.Action,
                    permission.Name,
                    permission.Value,
                    permission.GroupName,
                    permission.Description,
                    permission.DisplayOrder),
                cancellationToken);
        }

        foreach (var definition in definitions.Values)
        {
            if (!catalogValues.Contains(definition.Value))
            {
                if (definition.Value.EndsWith(".Manage", StringComparison.Ordinal))
                {
                    dbContext.PermissionDefinitions.Remove(definition);
                    continue;
                }

                definition.Disable();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
