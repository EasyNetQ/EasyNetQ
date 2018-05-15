﻿using System;
using StructureMap;
using StructureMap.Pipeline;

namespace EasyNetQ.DI.StructureMap
{
    public class StructureMapAdapter : IServiceRegister
    {
        private readonly IRegistry registry;

        public StructureMapAdapter(IRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

            this.registry.For<IServiceResolver>(Lifecycles.Container).Use<StructureMapResolver>();
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    registry.For<TService>(Lifecycles.Transient).Use<TImplementation>();
                    return this;
                case Lifetime.Singleton:
                    registry.For<TService>(Lifecycles.Singleton).Use<TImplementation>();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            } 
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            registry.For<TService>(Lifecycles.Singleton).Use(instance);
            return this;
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        { 
            switch (lifetime)
            {
                case Lifetime.Transient:
                    registry.For<TService>(Lifecycles.Transient).Use(y => factory(y.GetInstance<IServiceResolver>()));
                    return this;
                case Lifetime.Singleton:
                    registry.For<TService>(Lifecycles.Singleton).Use(y => factory(y.GetInstance<IServiceResolver>()));
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            } 
        }

        private class StructureMapResolver : IServiceResolver
        {
            private readonly IContainer container;

            public StructureMapResolver(IContainer container)
            {
                this.container = container;
            }

            public TService Resolve<TService>() where TService : class
            {
                return container.GetInstance<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}
