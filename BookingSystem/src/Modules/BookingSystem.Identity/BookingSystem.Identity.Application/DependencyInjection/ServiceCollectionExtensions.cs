using BookingSystem.SharedKernel.Behaviors;
using BookingSystem.SharedKernel.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Identity.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services
            .AddApplicationCqrs()
            .AddApplicationPipelineBehaviors();

    public static IServiceCollection AddApplicationCqrs(this IServiceCollection services)
        => services.AddSelfMadeCqrs(cfg =>
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly));

    public static IServiceCollection AddApplicationPipelineBehaviors(this IServiceCollection services)
        => services
            .AddValidationPipelineBehavior()
            .AddLoggingBehavior()
            .AddPerformanceBehavior()
            .AddTracingBehavior()
            .AddTransactionBehavior();

    public static IServiceCollection AddValidationBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(ValidationDefaultBehavior<,>));

    public static IServiceCollection AddValidationPipelineBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(ValidationPipelineBehavior<,>));

    public static IServiceCollection AddLoggingBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

    public static IServiceCollection AddPerformanceBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(PerformancePipelineBehavior<,>));

    public static IServiceCollection AddTracingBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(TracingPipelineBehavior<,>));

    public static IServiceCollection AddTransactionBehavior(this IServiceCollection services)
        => services.AddPipelineBehavior(typeof(TransactionPipelineBehavior<,>));
}
