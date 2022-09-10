using System;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor;

/// <see cref="IServiceRegister"/> implementation for Castle.Windsor DI container.
public class WindsorAdapter : IServiceRegister
{
    /// <summary>
    /// Creates an adapter on top of <see cref="IWindsorContainer"/>.
    /// </summary>
    public WindsorAdapter(IWindsorContainer container)
    {
        Container = container;

        ConfigureContainer(Container);

        Container.Register(Component.For<IServiceResolver>()
            .UsingFactoryMethod(c => new WindsorResolver(c))
            .LifestyleTransient());
    }

    public IWindsorContainer Container { get; }

    /// <summary>
    /// Configures features necessary for collection registrations and overriding registrations.
    /// </summary>
    protected virtual void ConfigureContainer(IWindsorContainer container)
    {
        container.Kernel.Resolver.AddSubResolver(new Castle.MicroKernel.Resolvers.SpecializedResolvers.CollectionResolver(container.Kernel));
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        Container.RemoveHandler(serviceType);
        var registration = Component.For(serviceType)
            .Named(serviceType.FullName)
            .ImplementedBy(implementationType)
            .LifeStyle.Is(ToLifestyleType(lifetime))
            .IsDefault();
        Container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        Container.RemoveHandler(serviceType);
        var registration = Component.For(serviceType)
            .Named(serviceType.FullName)
            .UsingFactoryMethod(x => implementationFactory(x.Resolve<IServiceResolver>()))
            .LifeStyle.Is(ToLifestyleType(lifetime))
            .IsDefault();
        Container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        Container.RemoveHandler(serviceType);
        var registration = Component.For(serviceType)
            .Named(serviceType.FullName)
            .Instance(implementationInstance)
            .LifestyleSingleton()
            .IsDefault();
        Container.Register(registration);
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
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance) =>
        IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationInstance);

    private bool IsServiceRegistered(Type serviceType) => Container.Kernel.HasComponent(serviceType);

    private static LifestyleType ToLifestyleType(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Transient => LifestyleType.Transient,
            Lifetime.Singleton => LifestyleType.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

    private class WindsorResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public WindsorResolver(IKernel kernel) => this.kernel = kernel;

        public TService Resolve<TService>() where TService : class => kernel.Resolve<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
