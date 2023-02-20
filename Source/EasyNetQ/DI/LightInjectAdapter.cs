#if LIGHT_INJECT_PACKAGE
using LightInject;
#else
using EasyNetQ.LightInject;
#endif

#if LIGHT_INJECT_PACKAGE
namespace EasyNetQ.DI.LightInject;
#else
namespace EasyNetQ.DI;
#endif

/// <see cref="IServiceRegister"/> implementation for LightInject DI container.
#if LIGHT_INJECT_PACKAGE
public
#else
internal
#endif
    class LightInjectAdapter : IServiceRegister
{
    /// <summary>
    /// Creates an adapter on top of <see cref="IServiceRegistry"/>.
    /// </summary>
    public LightInjectAdapter(IServiceRegistry serviceRegistry)
    {
        ServiceRegistry = serviceRegistry;

        ServiceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x), new PerRequestLifeTime());
    }

    public IServiceRegistry ServiceRegistry { get; }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        ServiceRegistry.Register(serviceType, implementationType, ToLifetime(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(
        Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton
    )
    {
        var serviceRegistration = new ServiceRegistration
        {
            ServiceType = serviceType,
            FactoryExpression = (Func<IServiceFactory, object>)(x =>
                implementationFactory((IServiceResolver)x.GetInstance(typeof(IServiceResolver)))),
            Lifetime = ToLifetime(lifetime),
        };
        ServiceRegistry.Register(serviceRegistration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        ServiceRegistry.RegisterInstance(serviceType, implementationInstance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton
    ) => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationType, lifetime);

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton
    ) => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationFactory, lifetime);

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, object implementationInstance
    ) => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationInstance);

    private static ILifetime ToLifetime(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Transient => new PerRequestLifeTime(),
            Lifetime.Singleton => new PerContainerLifetime(),
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

    private bool IsServiceRegistered(Type serviceType) =>
        ServiceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType);

    private sealed class LightInjectResolver : IServiceResolver
    {
        private readonly IServiceFactory serviceFactory;

        public LightInjectResolver(IServiceFactory serviceFactory) => this.serviceFactory = serviceFactory;

        public TService Resolve<TService>() where TService : class => serviceFactory.GetInstance<TService>();

        public IServiceResolverScope CreateScope() => new LightInjectScope(serviceFactory);
    }

    private sealed class LightInjectScope : IServiceResolverScope, IAsyncDisposable
    {
        private readonly Scope scope;

        public LightInjectScope(IServiceFactory serviceFactory) => scope = serviceFactory.BeginScope();

        public TService Resolve<TService>() where TService : class => scope.GetInstance<TService>();

        public IServiceResolverScope CreateScope() => new LightInjectScope(scope);

        public void Dispose() => scope.Dispose();

        public ValueTask DisposeAsync()
        {
            if (scope is IAsyncDisposable asyncDisposable)
                return asyncDisposable.DisposeAsync();

            scope.Dispose();
            return default;
        }
    }
}
