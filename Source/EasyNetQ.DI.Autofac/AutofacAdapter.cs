using System;
using Autofac;

namespace EasyNetQ.DI.Autofac;

/// <inheritdoc />
/// Autofac doesn't support registrations with replace mechanics.
/// The only way is to create a new ContainerBuilder and copy needed registrations.
/// https://stackoverflow.com/questions/5091101/is-it-possible-to-remove-an-existing-registration-from-autofac-container-builder
public class AutofacAdapter : IServiceRegister
{
    /// <summary>
    ///     Creates an adapter on top of ContainerBuilder
    /// </summary>
    public AutofacAdapter(ContainerBuilder containerBuilder)
    {
        ContainerBuilder = containerBuilder ?? throw new ArgumentNullException(nameof(containerBuilder));

        ContainerBuilder.RegisterType<AutofacResolver>()
            .As<IServiceResolver>()
            .InstancePerLifetimeScope();
    }

    public ContainerBuilder ContainerBuilder { get; }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        switch (lifetime)
        {
            case Lifetime.Transient when serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterGeneric(implementationType)
                    .As(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton when serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterGeneric(implementationType)
                    .As(serviceType)
                    .SingleInstance();
                return this;
            case Lifetime.Transient when !serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterType(implementationType)
                    .As(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton when !serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterType(implementationType)
                    .As(serviceType)
                    .SingleInstance();

                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                ContainerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                    .As(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton:
                ContainerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                    .As(serviceType)
                    .SingleInstance();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        // Autofac has only generic API to register service instance, so there is a bit reflection here
        // containerBuilder.RegisterInstance<TService>(implementationInstance);
        var methodInfo = typeof(RegistrationExtensions).GetMethod("RegisterInstance") ?? throw new MissingMethodException("RegisterInstance is not found");
        methodInfo.MakeGenericMethod(serviceType).Invoke(null, new[] { ContainerBuilder, implementationInstance });
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        switch (lifetime)
        {
            case Lifetime.Transient when serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterGeneric(implementationType)
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton when serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterGeneric(implementationType)
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .SingleInstance();
                return this;
            case Lifetime.Transient when !serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterType(implementationType)
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton when !serviceType.IsGenericTypeDefinition:
                ContainerBuilder.RegisterType(implementationType)
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .SingleInstance();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                ContainerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .InstancePerDependency();
                return this;
            case Lifetime.Singleton:
                ContainerBuilder.Register(c => implementationFactory(c.Resolve<IServiceResolver>()))
                    .As(serviceType)
                    .IfNotRegistered(serviceType)
                    .SingleInstance();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance)
    {
        // Autofac has only generic API to register service instance, so there is a bit reflection here
        // containerBuilder.RegisterInstance<TService>(implementationInstance).IfNotRegistered();
        var registerInstanceMethodInfo = typeof(RegistrationExtensions).GetMethod("RegisterInstance") ?? throw new MissingMethodException("RegisterInstance is not found");
        var ifNotRegisteredMethodInfo = typeof(RegistrationExtensions).GetMethod("IfNotRegistered") ?? throw new MissingMethodException("IfNotRegistered is not found");
        var registration = registerInstanceMethodInfo.MakeGenericMethod(serviceType).Invoke(null, new[] { ContainerBuilder, implementationInstance });
        var genericTypeArguments = registration.GetType().GenericTypeArguments;
        ifNotRegisteredMethodInfo.MakeGenericMethod(genericTypeArguments).Invoke(null, new[] { registration, serviceType });
        return this;
    }

    private class AutofacResolver : IServiceResolver
    {
        protected readonly ILifetimeScope Lifetime;

        public AutofacResolver(ILifetimeScope lifetime) => Lifetime = lifetime;

        public TService Resolve<TService>() where TService : class => Lifetime.Resolve<TService>();

        public IServiceResolverScope CreateScope() => new AutofacResolverScope(Lifetime.BeginLifetimeScope());
    }

    private class AutofacResolverScope : AutofacResolver, IServiceResolverScope
    {
        public AutofacResolverScope(ILifetimeScope lifetime) : base(lifetime)
        {
        }

        public void Dispose() => Lifetime.Dispose();
    }
}
