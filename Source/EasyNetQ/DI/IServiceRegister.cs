namespace EasyNetQ.DI;

/// <summary>
/// An interface for registering services with the dependency injection provider.
/// </summary>
public interface IServiceRegister
{
    /// <summary>
    /// Registers (or replaces if already registered) the service of type <paramref name="serviceType"/> with the <paramref name="implementationType"/>
    /// with the dependency injection provider
    /// </summary>
    /// <param name="serviceType">The type of the service to be registered</param>
    /// <param name="implementationType">The implementation type</param>
    /// <param name="lifetime">A lifetime of a container registration</param>
    /// <returns>itself for nice fluent composition</returns>
    IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="Register(Type, Type, Lifetime)"/>
    IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="Register(Type, Type, Lifetime)"/>
    IServiceRegister Register(Type serviceType, object implementationInstance);

    /// <summary>
    /// Tries to register (if it is not registered) the service of type <paramref name="serviceType"/> with the <paramref name="implementationType"/>
    /// with the dependency injection provider
    /// </summary>
    /// <param name="serviceType">The type of the service to be registered</param>
    /// <param name="implementationType">The implementation type</param>
    /// <param name="lifetime">A lifetime of a container registration</param>
    /// <returns>itself for nice fluent composition</returns>
    IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="TryRegister(Type, Type, Lifetime)"/>
    IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="TryRegister(Type, Type, Lifetime)"/>
    IServiceRegister TryRegister(Type serviceType, object implementationInstance);
}
