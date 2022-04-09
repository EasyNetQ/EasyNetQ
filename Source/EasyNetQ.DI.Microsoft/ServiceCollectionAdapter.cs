using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.DI.Microsoft;

/// <inheritdoc />
public class ServiceCollectionAdapter : IServiceRegister, ICollectionServiceRegister
{
    private readonly IServiceCollection serviceCollection;

    /// <summary>
    ///     Creates an adapter on top of IServiceCollection
    /// </summary>
    public ServiceCollectionAdapter(IServiceCollection serviceCollection)
    {
        this.serviceCollection = serviceCollection;

        this.serviceCollection.AddSingleton<IServiceResolver, ServiceProviderAdapter>();
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.Replace(ServiceDescriptor.Transient<TService, TImplementation>());
                break;
            case Lifetime.Singleton:
                serviceCollection.Replace(ServiceDescriptor.Singleton<TService, TImplementation>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        return this;
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService, TImplementation>(Lifetime lifetime)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.AddTransient<TService, TImplementation>();
                return this;
            case Lifetime.Singleton:
                serviceCollection.AddSingleton<TService, TImplementation>();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        serviceCollection.Replace(ServiceDescriptor.Singleton(instance));
        return this;
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(TService instance)
    {
        serviceCollection.AddSingleton(instance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.Replace(ServiceDescriptor.Transient(x => factory(x.GetService<IServiceResolver>())));
                break;
            case Lifetime.Singleton:
                serviceCollection.Replace(ServiceDescriptor.Singleton(x => factory(x.GetService<IServiceResolver>())));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        return this;
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.AddTransient(x => factory(x.GetService<IServiceResolver>()));
                return this;
            case Lifetime.Singleton:
                serviceCollection.AddSingleton(x => factory(x.GetService<IServiceResolver>()));
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(
        Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton
    )
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.AddTransient(serviceType, implementingType);
                return this;
            case Lifetime.Singleton:
                serviceCollection.AddSingleton(serviceType, implementingType);
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
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
