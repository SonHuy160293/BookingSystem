namespace BookingSystem.SharedKernel.Security;

public static class PermissionCatalog
{
    public static readonly IReadOnlyCollection<PermissionCatalogItem> DashboardPermissions =
    [
        Create(Functions.Dashboard, ActionType.View, Functions.DashboardViewDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> UserPermissions =
    [
        Create(Functions.Users, ActionType.View, Functions.UsersViewDescription),
        Create(Functions.Users, ActionType.Create, Functions.UsersCreateDescription),
        Create(Functions.Users, ActionType.Update, Functions.UsersUpdateDescription),
        Create(Functions.Users, ActionType.Delete, Functions.UsersDeleteDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> RolePermissions =
    [
        Create(Functions.Roles, ActionType.View, Functions.RolesViewDescription),
        Create(Functions.Roles, ActionType.Create, Functions.RolesCreateDescription),
        Create(Functions.Roles, ActionType.Update, Functions.RolesUpdateDescription),
        Create(Functions.Roles, ActionType.Delete, Functions.RolesDeleteDescription),
        Create(Functions.Roles, ActionType.Assign, Functions.RolesAssignDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> CategoryPermissions =
    [
        Create(Functions.Categories, ActionType.View, Functions.CategoriesViewDescription),
        Create(Functions.Categories, ActionType.Create, Functions.CategoriesCreateDescription),
        Create(Functions.Categories, ActionType.Update, Functions.CategoriesUpdateDescription),
        Create(Functions.Categories, ActionType.Delete, Functions.CategoriesDeleteDescription),
        Create(Functions.Categories, ActionType.Import, Functions.CategoriesImportDescription),
        Create(Functions.Categories, ActionType.Export, Functions.CategoriesExportDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> ProductPermissions =
    [
        Create(Functions.Products, ActionType.View, Functions.ProductsViewDescription),
        Create(Functions.Products, ActionType.Create, Functions.ProductsCreateDescription),
        Create(Functions.Products, ActionType.Update, Functions.ProductsUpdateDescription),
        Create(Functions.Products, ActionType.Delete, Functions.ProductsDeleteDescription),
        Create(Functions.Products, ActionType.Import, Functions.ProductsImportDescription),
        Create(Functions.Products, ActionType.Export, Functions.ProductsExportDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> OrderPermissions =
    [
        Create(Functions.Orders, ActionType.View, Functions.OrdersViewDescription),
        Create(Functions.Orders, ActionType.Create, Functions.OrdersCreateDescription),
        Create(Functions.Orders, ActionType.Update, Functions.OrdersUpdateDescription),
        Create(Functions.Orders, ActionType.Delete, Functions.OrdersDeleteDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogItem> MenuPermissions =
    [
        Create(Functions.Menus, ActionType.View, Functions.MenusViewDescription),
        Create(Functions.Menus, ActionType.Create, Functions.MenusCreateDescription),
        Create(Functions.Menus, ActionType.Update, Functions.MenusUpdateDescription),
        Create(Functions.Menus, ActionType.Delete, Functions.MenusDeleteDescription)
    ];

    public static readonly IReadOnlyCollection<PermissionCatalogGroup> Groups =
    [
        new(Functions.Dashboard, Functions.DashboardGroupName, DashboardPermissions),
        new(Functions.Users, Functions.UsersGroupName, UserPermissions),
        new(Functions.Roles, Functions.RolesGroupName, RolePermissions),
        new(Functions.Categories, Functions.CategoriesGroupName, CategoryPermissions),
        new(Functions.Products, Functions.ProductsGroupName, ProductPermissions),
        new(Functions.Orders, Functions.OrdersGroupName, OrderPermissions),
        new(Functions.Menus, Functions.MenusGroupName, MenuPermissions)
    ];

    public static string[] GetAdministrativePermissionValues()
        => Groups
            .SelectMany(group => group.Permissions)
            .Select(permission => permission.Value)
            .ToArray();

    public static string ToValue(string function, ActionType action)
        => $"{function}.{action}";

    private static PermissionCatalogItem Create(string function, ActionType action, string description)
        => new(action, $"{action} {function}", ToValue(function, action), description);
}

public sealed record PermissionCatalogGroup(
    string Function,
    string GroupName,
    IReadOnlyCollection<PermissionCatalogItem> Permissions);

public sealed record PermissionCatalogItem(
    ActionType Action,
    string Name,
    string Value,
    string? Description);
