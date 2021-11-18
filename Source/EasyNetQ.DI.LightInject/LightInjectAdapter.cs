using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    /// <inheritdoc />
    public class LightInjectAdapter : IServiceRegister
    {
        private readonly IServiceRegistry serviceRegistry;

        /// <summary>
        ///     Creates an adapter on top of IServiceRegistry
        /// </summary>
        /// <param name="serviceRegistry"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LightInjectAdapter(IServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));

            this.serviceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x), new PerRequestLifeTime());
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            serviceRegistry.Register<TService, TImplementation>(ToLifetime(lifetime));
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            serviceRegistry.RegisterInstance(instance);
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            serviceRegistry.Register(x => factory(x.GetInstance<IServiceResolver>()), ToLifetime(lifetime));
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register(
            Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton
        )
        {
            serviceRegistry.Register(serviceType, implementingType, ToLifetime(lifetime));
            return this;
        }

        private static ILifetime ToLifetime(Lifetime lifetime)
        {
            return lifetime switch
            {
                Lifetime.Transient => new PerRequestLifeTime(),
                Lifetime.Singleton => new PerContainerLifetime(),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
            };
        }

        private class LightInjectResolver : IServiceResolver
        {
            private readonly IServiceFactory serviceFactory;

            public LightInjectResolver(IServiceFactory serviceFactory)
            {
                this.serviceFactory = serviceFactory;
            }

            public TService Resolve<TService>() where TService : class
            {
                return serviceFactory.GetInstance<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}
