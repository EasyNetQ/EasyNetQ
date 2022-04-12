using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.DI.Microsoft;

/// <see cref="IServiceRegister"/> implementation for Microsoft.Extensions.DependencyInjection DI container.
public class ServiceCollectionAdapter : IServiceRegister
{
    private readonly IServiceCollection serviceCollection;

    /// <summary>
    /// Creates an adapter on top of <see cref="IServiceCollection"/>.
    /// </summary>
    public ServiceCollectionAdapter(IServiceCollection serviceCollection)
    {
        this.serviceCollection = serviceCollection;

        this.serviceCollection.TryAddSingleton<IServiceResolver, ServiceProviderAdapter>();
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime, bool replace = true)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        if (replace)
        {
            serviceCollection.Replace(new ServiceDescriptor(serviceType, implementationType, ToLifetime(lifetime)));
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, implementationType, ToLifetime(lifetime)));
        }
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (implementationFactory == null)
            throw new ArgumentNullException(nameof(implementationFactory));

        if (replace)
        {
            serviceCollection.Replace(new ServiceDescriptor(serviceType, PreserveFuncType(implementationFactory), ToLifetime(lifetime)));
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, PreserveFuncType(implementationFactory), ToLifetime(lifetime)));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationInstance == null)
            throw new ArgumentNullException(nameof(implementationInstance));

        if (replace)
        {
            serviceCollection.Replace(new ServiceDescriptor(serviceType, implementationInstance));
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, implementationInstance));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        var descriptor = new ServiceDescriptor(serviceType, implementationType, ToLifetime(lifetime));
        if (mode == RegistrationCompareMode.ServiceType)
            serviceCollection.TryAdd(descriptor);
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
            serviceCollection.TryAddEnumerable(descriptor);
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationFactory == null)
            throw new ArgumentNullException(nameof(implementationFactory));

        var descriptor = new ServiceDescriptor(serviceType, PreserveFuncType(implementationFactory), ToLifetime(lifetime));
        if (mode == RegistrationCompareMode.ServiceType)
            serviceCollection.TryAdd(descriptor);
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
            serviceCollection.TryAddEnumerable(descriptor);
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationInstance == null)
            throw new ArgumentNullException(nameof(implementationInstance));

        var descriptor = new ServiceDescriptor(serviceType, implementationInstance);
        if (mode == RegistrationCompareMode.ServiceType)
            serviceCollection.TryAdd(descriptor);
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
            serviceCollection.TryAddEnumerable(descriptor);
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    private static ServiceLifetime ToLifetime(Lifetime lifetime)
        => lifetime switch
        {
            Lifetime.Singleton => ServiceLifetime.Singleton,
            //Lifetime.Scoped => MSServiceLifetime.Scoped,
            Lifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
        };

    // Without this code, the type of return value will be object
    private static Func<IServiceProvider, object> PreserveFuncType(Func<IServiceResolver, object> implementationFactory)
    {
        Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
        if (typeArguments.Length != 2)
            throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
        var implementationType = typeArguments[1];

        var implementationFactoryAdapterType = typeof(ImplementationFactoryAdapter<>).MakeGenericType(implementationType);
        return (Func<IServiceProvider, object>)Delegate.CreateDelegate(
                typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), implementationType),
                Activator.CreateInstance(implementationFactoryAdapterType, implementationFactory),
                implementationFactoryAdapterType.GetMethod("Resolve"));
    }

    private class ServiceProviderAdapter : IServiceResolver
    {
        private readonly IServiceProvider serviceProvider;

        public ServiceProviderAdapter(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TService Resolve<TService>() where TService : class
        {
            return serviceProvider.GetService<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new MicrosoftServiceResolverScope(serviceProvider);
        }
    }

    private class MicrosoftServiceResolverScope : IServiceResolverScope
    {
        private readonly IServiceScope serviceScope;

        public MicrosoftServiceResolverScope(IServiceProvider serviceProvider)
        {
            serviceScope = serviceProvider.CreateScope();
        }

        public IServiceResolverScope CreateScope()
        {
            return new MicrosoftServiceResolverScope(serviceScope.ServiceProvider);
        }

        public void Dispose()
        {
            serviceScope?.Dispose();
        }

        public TService Resolve<TService>() where TService : class
        {
            return serviceScope.ServiceProvider.GetService<TService>();
        }
    }

    private class ImplementationFactoryAdapter<T>
    {
        private readonly Func<IServiceResolver, object> _implementationFactory;

        public ImplementationFactoryAdapter(Func<IServiceResolver, object> implementationFactory)
        {
            _implementationFactory = implementationFactory;
        }

        public T Resolve(IServiceProvider provider) => (T)_implementationFactory(provider.GetService<IServiceResolver>());
    }
}
