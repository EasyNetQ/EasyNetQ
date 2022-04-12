using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ninject;
using Ninject.Activation;
using Ninject.Infrastructure;
using Ninject.Planning.Bindings;

namespace EasyNetQ.DI.Ninject;

/// <see cref="IServiceRegister"/> implementation for Ninject DI container.
public class NinjectAdapter : IServiceRegister
{
    private readonly IKernel kernel;
    private long counter;

    /// <summary>
    /// Creates an adapter on top of <see cref="IKernel"/>.
    /// </summary>
    public NinjectAdapter(IKernel kernel)
    {
        this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

        if (!this.kernel.GetBindings(typeof(IServiceResolver)).Any())
            this.kernel.Rebind<IServiceResolver>().ToMethod(x => new NinjectResolver(x.Kernel)).InTransientScope().WithMetadata("type", typeof(NinjectResolver));
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            kernel.Rebind(serviceType).To(implementationType).InScope(ToScope(lifetime)).WithMetadata("type", implementationType);
        else
            kernel.Bind(serviceType).To(implementationType).InScope(ToScope(lifetime)).Named(GetBindingName()).WithMetadata("type", implementationType);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;

        if (typeArguments.Length != 2)
            throw new InvalidOperationException("implementationFactory should have 2 generic type arguments");

        if (replace)
            kernel.Rebind(serviceType).ToMethod(x => implementationFactory(x.Kernel.Get<IServiceResolver>())).InScope(ToScope(lifetime)).WithMetadata("type", typeArguments[1]);
        else
            kernel.Bind(serviceType).ToMethod(x => implementationFactory(x.Kernel.Get<IServiceResolver>())).InScope(ToScope(lifetime)).Named(GetBindingName()).WithMetadata("type", typeArguments[1]);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (replace)
            kernel.Rebind(serviceType).ToConstant(implementationInstance).WithMetadata("type", implementationInstance.GetType());
        else
            kernel.Bind(serviceType).ToConstant(implementationInstance).Named(GetBindingName()).WithMetadata("type", implementationInstance.GetType());

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!kernel.GetBindings(serviceType).Any())
                Register(serviceType, implementationType, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            if (!kernel.GetBindings(serviceType).Any(b => GetImplementationType(b) == implementationType))
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
            if (!kernel.GetBindings(serviceType).Any())
                Register(serviceType, implementationFactory, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
            if (typeArguments.Length != 2)
                throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
            var implementationType = typeArguments[1];
            if (!kernel.GetBindings(serviceType).Any(b => GetImplementationType(b) == implementationType))
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
            if (!kernel.GetBindings(serviceType).Any())
                Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            if (!kernel.GetBindings(serviceType).Any(b => GetImplementationType(b) == implementationType))
                Register(serviceType, implementationInstance);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    private static Func<IContext, object> ToScope(Lifetime lifetime)
        => lifetime switch
        {
            Lifetime.Singleton => StandardScopeCallbacks.Singleton,
            //Lifetime.Scoped => MSServiceLifetime.Scoped,
            Lifetime.Transient => StandardScopeCallbacks.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

    private static Type GetImplementationType(IBinding binding)
    {
        return binding.Metadata.Get<Type>("type");
    }

    private string GetBindingName()
    {
        return "ENQ_" + Interlocked.Increment(ref counter);
    }

    private class NinjectResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public TService Resolve<TService>() where TService : class
        {
            try
            {
                return kernel.Get<TService>();
            }
            catch (ActivationException e) when (e.Message.Contains("More than one matching bindings are available."))
            {
                // fallback to resolve the last registration from all available for this service if there is no default one
                var t = kernel.GetBindings(typeof(TService)).ToArray();
                var binding = kernel.GetBindings(typeof(TService))
                     .Where(b => b.Metadata.Name.StartsWith("ENQ_"))
                     .OrderByDescending(r => r.Metadata.Name, RegistrationsComparer.Instance)
                     .FirstOrDefault();

                if (binding == null)
                    throw;

                return kernel.Get<TService>(binding.Metadata.Name);
            }
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }

        private sealed class RegistrationsComparer : IComparer<string>
        {
            public static readonly RegistrationsComparer Instance = new();

            public int Compare(string x, string y)
            {
                return long.Parse(x.Substring(4)).CompareTo(long.Parse(y.Substring(4)));
            }
        }
    }
}
