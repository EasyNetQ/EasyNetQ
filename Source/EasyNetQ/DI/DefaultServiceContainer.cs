﻿
using System;

namespace EasyNetQ.DI
{
    public class DefaultServiceContainer : IServiceRegister
    {
        private readonly LightInject.ServiceContainer container = new LightInject.ServiceContainer();

        public DefaultServiceContainer()
        {
            container.Register<IServiceResolver>(x => new LightInjectResolver(x), new LightInject.PerRequestLifeTime());
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>(ToLifetime(lifetime));
            return this;
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.RegisterInstance(instance);
            return this;
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            container.Register(x => factory((IServiceResolver)x.GetInstance(typeof(IServiceResolver))), ToLifetime(lifetime));
            return this;
        }

        public TService Resolve<TService>()
        {
            return (TService)container.GetInstance(typeof(TService));
        }

        private static LightInject.ILifetime ToLifetime(Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    return new LightInject.PerRequestLifeTime();
                case Lifetime.Singleton:
                    return new LightInject.PerContainerLifetime();
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
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