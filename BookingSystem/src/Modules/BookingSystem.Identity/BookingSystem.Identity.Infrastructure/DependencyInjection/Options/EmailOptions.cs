namespace BookingSystem.Identity.Infrastructure.DependencyInjection.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; init; } = EmailProviders.Logging;

    public string FromAddress { get; init; } = "no-reply@eventualshop.local";

    public string FromName { get; init; } = "no-reply@eventualshop.local";

    public string? ReplyToAddress { get; init; }

    public SmtpEmailOptions Smtp { get; init; } = new();
}

public sealed class SmtpEmailOptions
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 30;
}

public static class EmailProviders
{
    public const string Logging = "Logging";

    public const string Smtp = "Smtp";
}
