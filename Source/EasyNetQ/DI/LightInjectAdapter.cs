#if LIGHT_INJECT_PACKAGE
using LightInject;
#else
using EasyNetQ.LightInject;
#endif
using System;
using System.Linq;

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
    private readonly IServiceRegistry serviceRegistry;

    /// <summary>
    /// Creates an adapter on top of <see cref="IServiceRegistry"/>.
    /// </summary>
    public LightInjectAdapter(IServiceRegistry serviceRegistry)
    {
        this.serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));

        serviceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x), new PerRequestLifeTime());
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        serviceRegistry.Register(serviceType, implementationType, ToLifetime(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        var serviceRegistration = new ServiceRegistration
        {
            ServiceType = serviceType,
            FactoryExpression = (Func<IServiceFactory, object>)(x => implementationFactory((IServiceResolver)x.GetInstance(typeof(IServiceResolver)))),
            Lifetime = ToLifetime(lifetime),
        };
        serviceRegistry.Register(serviceRegistration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        serviceRegistry.RegisterInstance(serviceType, implementationInstance);
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
        serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType);

    private class LightInjectResolver : IServiceResolver
    {
        private readonly IServiceFactory serviceFactory;

        public LightInjectResolver(IServiceFactory serviceFactory) => this.serviceFactory = serviceFactory;

        public TService Resolve<TService>() where TService : class => serviceFactory.GetInstance<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
