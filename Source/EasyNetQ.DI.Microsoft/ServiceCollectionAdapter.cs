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

    private static ServiceLifetime TranslateLifetime(Lifetime lifetime)
          => lifetime switch
          {
              Lifetime.Singleton => ServiceLifetime.Singleton,
              //Lifetime.Scoped => MSServiceLifetime.Scoped,
              Lifetime.Transient => ServiceLifetime.Transient,
              _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
          };

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime, bool replace = true)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        if (replace)
        {
            serviceCollection.Replace(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(lifetime)));
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(lifetime)));
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
            serviceCollection.Replace(new ServiceDescriptor(serviceType, x => implementationFactory(x.GetRequiredService<IServiceResolver>()), TranslateLifetime(lifetime)));
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, x => implementationFactory(x.GetRequiredService<IServiceResolver>()), TranslateLifetime(lifetime)));
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

        var descriptor = new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(lifetime));
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

        var descriptor = new ServiceDescriptor(serviceType, x => implementationFactory(x.GetRequiredService<IServiceResolver>()), TranslateLifetime(lifetime));
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
}
