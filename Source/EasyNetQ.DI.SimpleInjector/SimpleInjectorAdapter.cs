using SimpleInjector;
using System;

namespace EasyNetQ.DI.SimpleInjector;

// https://simpleinjector.readthedocs.io/en/latest/using.html
/// <inheritdoc cref="IServiceRegister" />
public class SimpleInjectorAdapter : IServiceRegister, IServiceResolver
{
    private readonly Container container;

    /// <summary>
    ///     Creates an adapter on top of Container
    /// </summary>
    public SimpleInjectorAdapter(Container container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
        this.container.RegisterInstance<IServiceResolver>(this);
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                if (replace)
                {
                    container.Register(serviceType, implementationType, Lifestyle.Transient);
                    container.Collection.Register(serviceType, new[] { serviceType }); // forward resolving in case of collection
                }
                else
                {
                    container.Collection.Append(serviceType, implementationType, Lifestyle.Transient);
                }
                return this;

            case Lifetime.Singleton:
                if (replace)
                {
                    container.Register(serviceType, implementationType, Lifestyle.Singleton);
                    container.Collection.Register(serviceType, new[] { serviceType }); // forward resolving in case of collection
                }
                else
                {
                    container.Collection.Append(serviceType, implementationType, Lifestyle.Singleton);
                }
                return this;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        //TODO: forward for collection, problem with collection design
        // https://docs.simpleinjector.org/en/latest/using.html#collections
        switch (lifetime)
        {
            case Lifetime.Transient:
                if (replace)
                    container.Register(serviceType, () => implementationFactory(this), Lifestyle.Transient);
                else
                    container.Collection.Append(() => implementationFactory(this), Lifestyle.Transient);
                return this;

            case Lifetime.Singleton:
                if (replace)
                    container.Register(serviceType, () => implementationFactory(this), Lifestyle.Singleton);
                else
                    container.Collection.Append(() => implementationFactory(this), Lifestyle.Singleton);
                return this;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        //TODO: forward for collection
        if (replace)
            container.RegisterInstance(serviceType, implementationInstance);
        else
            container.Collection.AppendInstance(serviceType, implementationInstance);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (producer == null)
                Register(serviceType, implementationType, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            if (producer == null || producer.Registration.ImplementationType != implementationType)
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
        var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (producer == null)
                Register(serviceType, implementationFactory, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
            if (typeArguments.Length != 2)
                throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
            var implementationType = typeArguments[1];
            if (producer == null || producer.Registration.ImplementationType != implementationType)
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
        var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (producer == null)
                Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            if (producer == null || producer.Registration.ImplementationType != implementationType)
                Register(serviceType, implementationInstance);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    /// <inheritdoc />
    public TService Resolve<TService>() where TService : class
    {
        //TODO: GetInstance does not return registered types (we interested in last registration)
        //TODO: for collections, but GetAllInstances does
        // https://docs.simpleinjector.org/en/latest/using.html#resolving-instances
        return container.GetInstance<TService>();
    }

    /// <inheritdoc />
    public IServiceResolverScope CreateScope()
    {
        return new ServiceResolverScope(this);
    }
}
