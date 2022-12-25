namespace EasyNetQ.DI;

/// <summary>
/// Provides extension methods to configure services within a dependency injection framework.
/// </summary>
public static class ServiceRegisterExtensions
{
    /// <inheritdoc cref="Register{TService}(IServiceRegister, Func{IServiceResolver, TService}, Lifetime)"/>
    public static IServiceRegister Register<TService>(
        this IServiceRegister services, Lifetime lifetime = Lifetime.Singleton
    ) where TService : class
        => services.Register(typeof(TService), typeof(TService), lifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
    /// An instance of <typeparamref name="TImplementation"/> will be created when an instance is needed.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService, TImplementation>(this IServiceRegister services, Lifetime lifetime = Lifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
        => services.Register(typeof(TService), typeof(TImplementation), lifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService>(this IServiceRegister services, Func<IServiceResolver, TService> implementationFactory,
        Lifetime lifetime = Lifetime.Singleton)
        where TService : class
        => services.Register(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), lifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService, TImplementation>(
        this IServiceRegister services,
        Func<IServiceResolver, TImplementation> implementationFactory,
        Lifetime lifetime = Lifetime.Singleton
    )
        where TService : class
        where TImplementation : class, TService
        => services.Register(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), lifetime);

    /// <summary>
    /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService>(
        this IServiceRegister services,
        TService implementationInstance
    ) where TService : class
        => services.Register(typeof(TService), implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance)));

    /// <inheritdoc cref="TryRegister{TService}(IServiceRegister, Func{IServiceResolver, TService}, Lifetime)"/>
    public static IServiceRegister TryRegister<TService, TImplementation>(
        this IServiceRegister services,
        Lifetime lifetime = Lifetime.Singleton
    ) where TService : class
        => services.TryRegister(typeof(TService), typeof(TImplementation), lifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
    /// of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService, TImplementation>(
        this IServiceRegister services,
        Func<IServiceResolver, TImplementation> implementationFactory,
        Lifetime lifetime = Lifetime.Singleton
    ) where TService : class
        where TImplementation : class, TService
        => services.TryRegister(typeof(TService), implementationFactory, lifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
    /// of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService>(
        this IServiceRegister services,
        Func<IServiceResolver, TService> implementationFactory,
        Lifetime lifetime = Lifetime.Singleton
    ) where TService : class
        => services.TryRegister(typeof(TService), implementationFactory, lifetime);

    /// <summary>
    /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider
    /// if a service of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService>(
        this IServiceRegister services,
        TService implementationInstance
    ) where TService : class
        => services.TryRegister(typeof(TService), implementationInstance);
}
