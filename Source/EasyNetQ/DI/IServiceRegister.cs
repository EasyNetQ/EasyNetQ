using System;
using System.Collections.Generic;

namespace EasyNetQ.DI;

/// <summary>
/// An interface for registering services with the dependency injection provider.
/// </summary>
public interface IServiceRegister
{
    /// <summary>
    /// Registers the service of type <paramref name="serviceType"/> with the <paramref name="implementationType"/>
    /// with the dependency injection provider. Optionally removes any existing implementation of the same service type.
    /// When not replacing existing registrations, requesting the service type should return the most recent registration,
    /// and requesting an <see cref="IEnumerable{T}"/> of the service type should return all of the registrations.
    /// </summary>
    /// <param name="serviceType">The type of the service to be registered</param>
    /// <param name="implementationType">The implementation type</param>
    /// <param name="lifetime">A lifetime of a container registration</param>
    /// <returns>itself for nice fluent composition</returns>
    IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="Register(Type, Type, Lifetime, bool)"/>
    IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="Register(Type, Type, Lifetime, bool)"/>
    IServiceRegister Register(Type serviceType, object implementationInstance);

    /// <summary>
    /// Registers the service of type <paramref name="serviceType"/> with the dependency
    /// injection provider if a service of the same type (and of the same implementation type
    /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
    /// has not already been registered.
    /// </summary>
    IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton);

    /// <summary>
    /// Registers the service of type <paramref name="serviceType"/> with the dependency
    /// injection provider if a service of the same type (and of the same implementation type
    /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
    /// has not already been registered.
    /// <br/><br/>
    /// With <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>, it is required
    /// that <paramref name="implementationFactory"/> is a strongly typed delegate with a return type
    /// of a specific implementation type.
    /// </summary>
    IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton);

    /// <inheritdoc cref="TryRegister(Type, Type, Lifetime, RegistrationCompareMode)"/>
    IServiceRegister TryRegister(Type serviceType, object implementationInstance);
}
