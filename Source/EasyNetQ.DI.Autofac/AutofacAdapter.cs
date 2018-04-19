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

            this.containerBuilder.RegisterInstance((IServiceRegister) this);
            this.containerBuilder.Register(c => new AutofacResolver(c))
                                 .As<IServiceResolver>()
                                 .SingleInstance();
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
            private readonly IComponentContext componentContext;

            public AutofacResolver(IComponentContext componentContext)
            {
                this.componentContext = componentContext;
            }

            public TService Resolve<TService>() where TService : class
            {
                return componentContext.Resolve<TService>();
            }
        }
    }
}
