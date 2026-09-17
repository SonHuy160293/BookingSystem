namespace BookingSystem.ApiGateway.Configuration;

public static class GatewayPolicies
{
    public const string Jwt = "GatewayJwt";
    public const string Cors = "ConfiguredCors";
    public const string Timeout = "GatewayTimeout";
    public const string Login = "GatewayLogin";
    public const string Refresh = "GatewayRefresh";
    public const string AuthManagement = "GatewayAuthManagement";
    public const string Read = "GatewayRead";
    public const string Write = "GatewayWrite";

    public static readonly string[] RateLimitNames = [Login, Refresh, AuthManagement, Read, Write];
}
