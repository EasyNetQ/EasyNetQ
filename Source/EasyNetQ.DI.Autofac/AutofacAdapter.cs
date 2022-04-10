using System;
using Autofac;

namespace EasyNetQ.DI.Autofac;

// NOTE: Autofac does not allow to replace registrations
/// <inheritdoc />
public class AutofacAdapter : IServiceRegister
{
    private readonly ContainerBuilder containerBuilder;

    /// <summary>
    ///     Creates an adapter on top of ContainerBuilder
    /// </summary>
    public AutofacAdapter(ContainerBuilder containerBuilder)
    {
        this.containerBuilder = containerBuilder ?? throw new ArgumentNullException(nameof(containerBuilder));

        this.containerBuilder.RegisterType<AutofacResolver>()
            .As<IServiceResolver>()
            .InstancePerLifetimeScope();
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
        {
            throw new NotImplementedException();
        }
        else
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    containerBuilder.RegisterGeneric(implementationType)
                        .As(serviceType)
                        .InstancePerDependency();
                    return this;
                case Lifetime.Singleton:
                    containerBuilder.RegisterGeneric(implementationType)
                        .As(serviceType)
                        .SingleInstance();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
        {
            throw new NotImplementedException();
        }
        else
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    containerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                        .As(serviceType)
                        .InstancePerDependency();
                    return this;
                case Lifetime.Singleton:
                    containerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                        .As(serviceType)
                        .SingleInstance();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (replace)
        {
            throw new NotImplementedException();
        }
        else
        {
            // Autofac has only generic API to register service instance, so there is a bit reflection here
            // containerBuilder.RegisterInstance<TService>(implementationInstance);
            typeof(RegistrationExtensions).GetMethod("RegisterInstance").MakeGenericMethod(serviceType).Invoke(null, new[] { implementationInstance });
        }
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    private class AutofacResolver : IServiceResolver
    {
        protected readonly ILifetimeScope Lifetime;

        public AutofacResolver(ILifetimeScope lifetime)
        {
            Lifetime = lifetime;
        }

        public TService Resolve<TService>() where TService : class
        {
            return Lifetime.Resolve<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new AutofacResolverScope(Lifetime.BeginLifetimeScope());
        }
    }

    private class AutofacResolverScope : AutofacResolver, IServiceResolverScope
    {
        public AutofacResolverScope(ILifetimeScope lifetime) : base(lifetime)
        {
        }

        public void Dispose()
        {
            Lifetime.Dispose();
        }
    }
}
