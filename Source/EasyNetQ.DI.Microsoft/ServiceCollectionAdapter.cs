using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.DI.Microsoft;

/// <inheritdoc />
public class ServiceCollectionAdapter : IServiceRegister
{
    private readonly IServiceCollection serviceCollection;

    /// <summary>
    ///     Creates an adapter on top of IServiceCollection
    /// </summary>
    public ServiceCollectionAdapter(IServiceCollection serviceCollection)
    {
        this.serviceCollection = serviceCollection;

        this.serviceCollection.TryAddSingleton<IServiceResolver, ServiceProviderAdapter>();
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.TryAddTransient<TService, TImplementation>();
                return this;
            case Lifetime.Singleton:
                serviceCollection.TryAddSingleton<TService, TImplementation>();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        serviceCollection.TryAddSingleton(instance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                serviceCollection.TryAddTransient(x => factory(x.GetService<IServiceResolver>()));
                return this;
            case Lifetime.Singleton:
                serviceCollection.TryAddSingleton(x => factory(x.GetService<IServiceResolver>()));
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
                serviceCollection.TryAddTransient(serviceType, implementingType);
                return this;
            case Lifetime.Singleton:
                serviceCollection.TryAddSingleton(serviceType, implementingType);
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

        public MicrosoftServiceResolverScope(IServiceProvider serviceProvider) => serviceScope = serviceProvider.CreateScope();

        public IServiceResolverScope CreateScope() => new MicrosoftServiceResolverScope(serviceScope.ServiceProvider);

        public void Dispose() => serviceScope.Dispose();

        public TService Resolve<TService>() where TService : class => serviceScope.ServiceProvider.GetService<TService>();
    }
}
