using System;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor;

/// <see cref="IServiceRegister"/> implementation for Castle.Windsor DI container.
public class WindsorAdapter : IServiceRegister
{
    private readonly IWindsorContainer container;

    /// <summary>
    /// Creates an adapter on top of <see cref="IWindsorContainer"/>.
    /// </summary>
    public WindsorAdapter(IWindsorContainer container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));

        ConfigureContainer(container);

        this.container.Register(Component.For<IServiceResolver>()
            .UsingFactoryMethod(c => new WindsorResolver(c))
            .LifestyleTransient());
    }

    /// <summary>
    /// Configures features necessary for collection registrations and overriding registrations.
    /// </summary>
    protected virtual void ConfigureContainer(IWindsorContainer container)
    {
        container.Kernel.Resolver.AddSubResolver(new Castle.MicroKernel.Resolvers.SpecializedResolvers.CollectionResolver(container.Kernel));
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            container.RemoveHandler(serviceType);

        var registration = replace
            ? Component.For(serviceType)
                .Named(serviceType.FullName)
                .ImplementedBy(implementationType)
                .LifeStyle.Is(GetLifestyleType(lifetime))
                .IsDefault()
            : Component.For(serviceType)
                .Named(Guid.NewGuid().ToString())
                .ImplementedBy(implementationType)
                .LifeStyle.Is(GetLifestyleType(lifetime))
                .IsDefault();

        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            container.RemoveHandler(serviceType);

        var registration = replace
            ? Component.For(serviceType)
                .Named(serviceType.FullName)
                .UsingFactoryMethod(x => implementationFactory(x.Resolve<IServiceResolver>()))
                .LifeStyle.Is(GetLifestyleType(lifetime))
                .IsDefault()
            : Component.For(serviceType)
                .Named(Guid.NewGuid().ToString())
                .UsingFactoryMethod(x => implementationFactory(x.Resolve<IServiceResolver>()))
                .LifeStyle.Is(GetLifestyleType(lifetime))
                .IsDefault();

        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (replace)
            container.RemoveHandler(serviceType);

        var registration = replace
            ? Component.For(serviceType)
                .Named(serviceType.FullName)
                .Instance(implementationInstance)
                .LifestyleSingleton()
                .IsDefault()
            : Component.For(serviceType)
                .Named(Guid.NewGuid().ToString())
                .Instance(implementationInstance)
                .LifestyleSingleton()
                .IsDefault();

        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!container.Kernel.HasComponent(serviceType))
                Register(serviceType, implementationType, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            if (!container.HasComponentWithImplementation(serviceType, implementationType))
                Register(serviceType, implementationType, lifetime);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!container.Kernel.HasComponent(serviceType))
                Register(serviceType, implementationFactory, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
            if (typeArguments.Length != 2)
                throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
            var implementationType = typeArguments[1];
            if (!container.HasComponentWithImplementation(serviceType, implementationType))
                Register(serviceType, implementationFactory, lifetime);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!container.Kernel.HasComponent(serviceType))
                Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            if (!container.HasComponentWithImplementation(serviceType, implementationType))
                Register(serviceType, implementationInstance);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    private class WindsorResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public WindsorResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public TService Resolve<TService>() where TService : class
        {
            return kernel.Resolve<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }
    }

    private LifestyleType GetLifestyleType(Lifetime lifetime)
    {
        return lifetime switch
        {
            Lifetime.Transient => LifestyleType.Transient,
            Lifetime.Singleton => LifestyleType.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
    }
}
