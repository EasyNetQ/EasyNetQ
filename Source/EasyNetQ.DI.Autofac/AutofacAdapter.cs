using System;
using Autofac;

namespace EasyNetQ.DI.Autofac
{
    public class AutofacAdapter : IServiceRegister
    {
        private readonly ContainerBuilder containerBuilder;

        public AutofacAdapter(ContainerBuilder containerBuilder)
        {
            this.containerBuilder = containerBuilder ?? throw new ArgumentNullException(nameof(containerBuilder));

            this.containerBuilder.Register(c => new AutofacResolver(c.Resolve<ILifetimeScope>()))
                                 .As<IServiceResolver>()
                                 .InstancePerLifetimeScope();
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    containerBuilder.RegisterType<TImplementation>()
                                    .As<TService>()
                                    .InstancePerDependency();
                    return this;
                case Lifetime.Singleton:
                    containerBuilder.RegisterType<TImplementation>()
                                    .As<TService>()
                                    .SingleInstance();
                    
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            containerBuilder.RegisterInstance(instance);
            return this;
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
}
