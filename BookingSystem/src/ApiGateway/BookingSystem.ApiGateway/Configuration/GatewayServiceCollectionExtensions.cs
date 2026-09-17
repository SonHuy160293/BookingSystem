using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using BookingSystem.ApiGateway.Health;
using BookingSystem.ApiGateway.Middleware;
using BookingSystem.ApiGateway.Routing;
using BookingSystem.ApiGateway.Security;
using BookingSystem.AspNetCore.DependencyInjection;
using BookingSystem.AspNetCore.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;

namespace BookingSystem.ApiGateway.Configuration;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGateway(
        this IServiceCollection services, IConfiguration configuration, IConfiguration proxyConfiguration)
    {
        services.AddOptions<GatewayOptions>()
            .Bind(configuration.GetSection(GatewayOptions.SectionName))
            .Validate(options => options.MaxRequestBodyBytes > 0, "Gateway body limit must be positive.")
            .Validate(options => options.ConnectionTimeoutSeconds > 0
                && options.ActivityTimeoutSeconds >= options.ConnectionTimeoutSeconds
                && options.RequestTimeoutSeconds >= options.ActivityTimeoutSeconds, "Gateway timeouts must be positive and ordered.")
            .Validate(options => options.AllowedHosts.Length > 0
                && options.AllowedHosts.All(host => host != "*" && !string.IsNullOrWhiteSpace(host)
                    && (IPAddress.TryParse(host, out _) || Uri.CheckHostName(host) == UriHostNameType.Dns)),
                "Gateway must define explicit allowed hosts.")
            .Validate(options => options.KnownProxies.All(proxy => IPAddress.TryParse(proxy, out _)),
                "Gateway trusted proxy addresses must be valid IP addresses.")
            .Validate(options => options.RateLimits.Count == GatewayPolicies.RateLimitNames.Length
                && GatewayPolicies.RateLimitNames.All(name => options.RateLimits.TryGetValue(name, out var limit)
                    && limit.PermitLimit > 0 && limit.WindowSeconds > 0),
                "Gateway rate limits must define positive limits for every policy.")
            .ValidateOnStart();

        services.AddConfiguredCors(configuration);
        services.AddOptions<CorsOptions>()
            .Validate(options => !options.AllowCredentials && options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var address)
                && address.Scheme is "http" or "https"
                && string.Equals(origin, address.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)),
                "Gateway CORS requires explicit HTTP origins without credentials.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
                "Gateway JWT signing key must contain at least 32 UTF-8 bytes.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer)
                && !string.IsNullOrWhiteSpace(options.Audience), "Gateway JWT issuer and audience are required.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SecretKey)),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    ClockSkew = TimeSpan.Zero
                };
                options.EventsType = typeof(GatewayJwtBearerEvents);
            });
        services.AddScoped<GatewayJwtBearerEvents>();
        services.AddAuthorization(options =>
        {
            var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();
            options.AddPolicy(GatewayPolicies.Jwt, policy);
            options.DefaultPolicy = policy;
            options.FallbackPolicy = policy;
        });

        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<GatewayOptions>>((options, gateway) =>
        {
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.ForwardedHeaders = gateway.Value.KnownProxies.Length == 0 ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            foreach (string proxy in gateway.Value.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (string host in gateway.Value.AllowedHosts)
            {
                options.AllowedHosts.Add(host);
            }
        });
        services.AddHttpsRedirection(options => options.HttpsPort = 443);

        var gatewayOptions = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>() ?? new GatewayOptions();
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(
            options => options.Limits.MaxRequestBodySize = gatewayOptions.MaxRequestBodyBytes);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (string name in GatewayPolicies.RateLimitNames)
            {
                var limit = gatewayOptions.RateLimits[name];
                options.AddPolicy(name, context =>
                {
                    var endpoint = GatewayEndpointMetadata.From(context);
                    bool publicAuth = name is GatewayPolicies.Login or GatewayPolicies.Refresh;
                    string? subject = publicAuth ? null : context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    var address = context.Connection.RemoteIpAddress;
                    string ip = address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4().ToString()
                        : address?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(name + ":" + endpoint.RouteId + ":" + (subject ?? ip),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limit.PermitLimit,
                            Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
            }

            options.OnRejected = async (context, _) =>
            {
                TimeSpan retry = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value : TimeSpan.FromSeconds(60);
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests);
            };
        });
        services.AddRequestTimeouts(options => options.AddPolicy(GatewayPolicies.Timeout, new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(gatewayOptions.RequestTimeoutSeconds),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
            WriteTimeoutResponse = context => GatewayErrorWriter.WriteAsync(context, StatusCodes.Status504GatewayTimeout)
        }));
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<GatewayReadinessCheck>("upstreams", tags: ["ready"]);
        services.AddReverseProxy()
            .LoadFromConfig(proxyConfiguration)
            .AddServiceDiscoveryDestinationResolver()
            .AddConfigFilter<GatewayProxyConfigFilter>()
            .AddTransforms(GatewayTransforms.Apply)
            .ConfigureHttpClient((_, handler) =>
            {
                handler.ConnectTimeout = TimeSpan.FromSeconds(gatewayOptions.ConnectionTimeoutSeconds);
                handler.AllowAutoRedirect = false;
                handler.UseCookies = false;
            });
        services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("Yarp.ReverseProxy"));
        return services;
    }
}
