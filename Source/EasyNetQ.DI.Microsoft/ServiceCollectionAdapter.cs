using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.DI.Microsoft;

/// <see cref="IServiceRegister"/> implementation for Microsoft.Extensions.DependencyInjection DI container.
public class ServiceCollectionAdapter : IServiceRegister
{
    /// <summary>
    /// Creates an adapter on top of <see cref="IServiceCollection"/>.
    /// </summary>
    public ServiceCollectionAdapter(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;

        ServiceCollection.TryAddSingleton<IServiceResolver, ServiceProviderAdapter>();
    }

    public IServiceCollection ServiceCollection { get; }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime)
    {
        ServiceCollection.Replace(new ServiceDescriptor(serviceType, implementationType, ToLifetime(lifetime)));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(
        Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton
    )
    {
        ServiceCollection.Replace(new ServiceDescriptor(serviceType, PreserveFuncType(implementationFactory), ToLifetime(lifetime)));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        ServiceCollection.Replace(new ServiceDescriptor(serviceType, implementationInstance));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton
    )
    {
        ServiceCollection.TryAdd(new ServiceDescriptor(serviceType, implementationType, ToLifetime(lifetime)));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton
    )
    {
        var descriptor = new ServiceDescriptor(serviceType, PreserveFuncType(implementationFactory), ToLifetime(lifetime));
        ServiceCollection.TryAdd(descriptor);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance)
    {
        ServiceCollection.TryAdd(new ServiceDescriptor(serviceType, implementationInstance));
        return this;
    }

    private static ServiceLifetime ToLifetime(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Singleton => ServiceLifetime.Singleton,
            Lifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
        };

    // Without this code, the type of return value will be object
    private static Func<IServiceProvider, object> PreserveFuncType(Func<IServiceResolver, object> implementationFactory)
    {
        if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

        var typeArguments = implementationFactory.GetType().GenericTypeArguments;
        if (typeArguments.Length != 2) throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");

        var implementationType = typeArguments[1];
        var implementationFactoryAdapterType = typeof(ImplementationFactoryAdapter<>).MakeGenericType(implementationType);
        var resolveMethodInfo = implementationFactoryAdapterType.GetMethod("Resolve") ?? throw new MissingMethodException();
        return (Func<IServiceProvider, object>)Delegate.CreateDelegate(
            typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), implementationType),
            Activator.CreateInstance(implementationFactoryAdapterType, implementationFactory),
            resolveMethodInfo
        );
    }

    private class ServiceProviderAdapter : IServiceResolver
    {
        private readonly IServiceProvider serviceProvider;

        public ServiceProviderAdapter(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        public TService Resolve<TService>() where TService : class => serviceProvider.GetService<TService>()!;

        public IServiceResolverScope CreateScope() => new MicrosoftServiceResolverScope(serviceProvider);
    }

    private class MicrosoftServiceResolverScope : IServiceResolverScope, IAsyncDisposable
    {
        private readonly IServiceScope serviceScope;

        public MicrosoftServiceResolverScope(IServiceProvider serviceProvider) => serviceScope = serviceProvider.CreateScope();

        public IServiceResolverScope CreateScope() => new MicrosoftServiceResolverScope(serviceScope.ServiceProvider);

        public void Dispose() => serviceScope?.Dispose();

        public ValueTask DisposeAsync()
        {
            if (serviceScope is IAsyncDisposable ad)
            {
                return ad.DisposeAsync();
            }
            Dispose();

            // ValueTask.CompletedTask is only available in net5.0 and later.
            return default;
        }

        public TService Resolve<TService>() where TService : class => serviceScope.ServiceProvider.GetService<TService>()!;
    }

    private class ImplementationFactoryAdapter<T>
    {
        private readonly Func<IServiceResolver, object> implementationFactory;

        public ImplementationFactoryAdapter(Func<IServiceResolver, object> implementationFactory) => this.implementationFactory = implementationFactory;

        // ReSharper disable once UnusedMember.Local
        public T Resolve(IServiceProvider provider) => (T)implementationFactory(provider.GetService<IServiceResolver>()!);
    }
}
