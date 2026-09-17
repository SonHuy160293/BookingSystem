using System.Reflection;
using BookingSystem.SharedKernel.Cqrs;
using BookingSystem.SharedKernel.Abstractions.Message;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.SharedKernel.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSelfMadeCqrs(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddSelfMadeCqrs(options =>
        {
            foreach (var assembly in assemblies)
            {
                options.RegisterServicesFromAssembly(assembly);
            }
        });

    public static IServiceCollection AddSelfMadeCqrs(this IServiceCollection services, Action<SelfMadeCqrsOptions> configure)
    {
        var options = new SelfMadeCqrsOptions();
        configure(options);

        services.AddScoped<CqrsMediator>();
        services.AddScoped<ISender>(serviceProvider => serviceProvider.GetRequiredService<CqrsMediator>());
        services.AddScoped<IPublisher>(serviceProvider => serviceProvider.GetRequiredService<CqrsMediator>());

        foreach (var implementationType in options.Assemblies.SelectMany(GetConcreteTypes))
        {
            foreach (var serviceType in implementationType.GetInterfaces().Where(IsCqrsService))
            {
                services.AddScoped(serviceType, implementationType);
            }
        }

        foreach (var behaviorType in options.Behaviors)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        }

        return services;
    }

    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type behaviorType)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        return services;
    }

    private static IEnumerable<Type> GetConcreteTypes(Assembly assembly)
        => assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Select(type => type.AsType());

    private static bool IsCqrsService(Type serviceType)
        => serviceType.IsGenericType
           && serviceType.GetGenericTypeDefinition() is var definition
           && (definition == typeof(IRequestHandler<,>)
               || definition == typeof(ICommandHandler<>)
               || definition == typeof(ICommandHandler<,>)
               || definition == typeof(IQueryHandler<,>)
               || definition == typeof(INotificationHandler<>)
               || definition == typeof(IValidator<>));
}

public sealed class SelfMadeCqrsOptions
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<Type> _behaviors = [];

    internal IReadOnlyCollection<Assembly> Assemblies => _assemblies;
    internal IReadOnlyCollection<Type> Behaviors => _behaviors;

    public SelfMadeCqrsOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    public SelfMadeCqrsOptions AddPipelineBehavior(Type behaviorType)
    {
        if (!behaviorType.IsGenericTypeDefinition || behaviorType.GetGenericArguments().Length != 2)
        {
            throw new ArgumentException("Pipeline behavior must be an open generic type with two generic arguments.", nameof(behaviorType));
        }

        _behaviors.Add(behaviorType);
        return this;
    }
}
