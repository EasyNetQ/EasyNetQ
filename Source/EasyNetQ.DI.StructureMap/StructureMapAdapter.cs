using System;
using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public class StructureMapAdapter : IServiceRegister
    {
        private readonly IContainer container;

        public StructureMapAdapter(IContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            
            this.container.Configure(x => x.For<IServiceResolver>().Use(y => new StructureMapResolver(y)).ContainerScoped());
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Configure(c => c.For<TService>().Use<TImplementation>().Transient());
                    return this;
                case Lifetime.Singleton:
                    container.Configure(c => c.For<TService>().Use<TImplementation>().Singleton());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            } 
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.Configure(c => c.For<TService>().Use(instance).Singleton());
            return this;
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        { 
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Configure(x => x.For<TService>().Use(y => factory(y.GetInstance<IServiceResolver>())).Transient());
                    return this;
                case Lifetime.Singleton:
                    container.Configure(x => x.For<TService>().Use(y => factory(y.GetInstance<IServiceResolver>())).Singleton());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            } 
        }

        private class StructureMapResolver : IServiceResolver
        {
            private readonly IContext context;

            public StructureMapResolver(IContext context)
            {
                this.context = context;
            }

            public TService Resolve<TService>() where TService : class
            {
                return context.GetInstance<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}
