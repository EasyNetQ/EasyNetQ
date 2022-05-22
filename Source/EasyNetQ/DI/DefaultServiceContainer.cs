using System;

namespace EasyNetQ.DI
{
    /// <inheritdoc cref="EasyNetQ.DI.IServiceRegister" />
    public class DefaultServiceContainer : IServiceRegister, IDisposable
    {
        private readonly LightInject.ServiceContainer container = new(c => c.EnablePropertyInjection = false);

        /// <summary>
        ///     Creates DefaultServiceContainer
        /// </summary>
        public DefaultServiceContainer()
        {
            container.Register<IServiceResolver>(x => new LightInjectResolver(x), new LightInject.PerRequestLifeTime());
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>(ToLifetime(lifetime));
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.RegisterInstance(instance);
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            container.Register(x => factory((IServiceResolver)x.GetInstance(typeof(IServiceResolver))), ToLifetime(lifetime));
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register(Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton)
        {
            container.Register(serviceType, implementingType, ToLifetime(lifetime));
            return this;
        }

        /// <summary>
        ///     Resolves instance of service
        /// </summary>
        /// <typeparam name="TService">Type of service to resolve</typeparam>
        public TService Resolve<TService>()
        {
            return (TService)container.GetInstance(typeof(TService));
        }

        /// <inheritdoc />
        public void Dispose() => container.Dispose();

        private static LightInject.ILifetime ToLifetime(Lifetime lifetime)
        {
            return lifetime switch
            {
                Lifetime.Transient => new LightInject.PerRequestLifeTime(),
                Lifetime.Singleton => new LightInject.PerContainerLifetime(),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
            };
        }

        private class LightInjectResolver : IServiceResolver
        {
            private readonly LightInject.IServiceFactory serviceFactory;

            public LightInjectResolver(LightInject.IServiceFactory serviceFactory)
            {
                this.serviceFactory = serviceFactory;
            }

            public TService Resolve<TService>() where TService : class
            {
                return (TService) serviceFactory.GetInstance(typeof(TService));
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}
