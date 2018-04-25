using System;
using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public class StructureMapAdapter : IServiceRegister, IServiceResolver
    {
        private readonly IContainer container;

        public StructureMapAdapter(IContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            
            this.container.Configure(c => c.For<IServiceRegister>().Singleton().Use(this));
            this.container.Configure(c => c.For<IServiceResolver>().Singleton().Use(this));
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.GetInstance<TService>();
        }

        public IServiceResolver CreateScope()
        {
            return new EmptyScope(this);
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Configure(c => c.For<TService>().Transient().Use<TImplementation>());
                    return this;
                case Lifetime.Singleton:
                    container.Configure(c => c.For<TService>().Singleton().Use<TImplementation>());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            } 
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.Configure(c => c.For<TService>().Singleton().Use(instance));
            return this;
        }

        public void Dispose()
        {
        }
        
        private class EmptyScope : IServiceResolver
        {
            private readonly IServiceResolver resolver;

            public EmptyScope(IServiceResolver resolver)
            {
                this.resolver = resolver;
            }

            public void Dispose()
            {
            }

            public TService Resolve<TService>() where TService : class
            {
                return resolver.Resolve<TService>();
            }

            public IServiceResolver CreateScope()
            {
                return this;
            }
        }
    }
}
