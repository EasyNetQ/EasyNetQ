using System;
using System.Collections.Generic;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor;

/// <inheritdoc />
public class WindsorAdapter : IServiceRegister
{
    private readonly IWindsorContainer container;

    /// <summary>
    ///     Creates an adapter on top of IWindsorContainer
    /// </summary>
    public WindsorAdapter(IWindsorContainer container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));

        this.container.Register(Component.For<IServiceResolver>()
            .UsingFactoryMethod(c => new WindsorResolver(c))
            .LifestyleTransient());
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        if (container.Kernel.HasComponent(typeof(TService))) return this;

        var registration = Component.For<TService>()
            .ImplementedBy<TImplementation>()
            .LifeStyle.Is(GetLifestyleType(lifetime));
        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        if (container.Kernel.HasComponent(typeof(TService))) return this;

        var registration = Component.For<TService>()
            .Instance(instance)
            .LifestyleSingleton();
        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        if (container.Kernel.HasComponent(typeof(TService))) return this;

        var registration = Component.For<TService>()
            .UsingFactoryMethod(x => factory(x.Resolve<IServiceResolver>()))
            .LifeStyle.Is(GetLifestyleType(lifetime));
        container.Register(registration);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(
        Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton
    )
    {
        if (container.Kernel.HasComponent(serviceType)) return this;

        var registration = Component.For(serviceType)
            .ImplementedBy(implementingType)
            .LifeStyle.Is(GetLifestyleType(lifetime));
        container.Register(registration);
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
            var type = typeof(TService);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return kernel.ResolveAll(type.GenericTypeArguments[0]) as TService;

            return kernel.Resolve<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }
    }

    private static LifestyleType GetLifestyleType(Lifetime lifetime)
    {
        return lifetime switch
        {
            Lifetime.Transient => LifestyleType.Transient,
            Lifetime.Singleton => LifestyleType.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
    }
}
