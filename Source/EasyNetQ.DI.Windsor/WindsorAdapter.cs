using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace EasyNetQ.DI
{
    public class WindsorAdapter : IContainer, IDisposable
    {
        private readonly IWindsorContainer windsorContainer;

        public WindsorAdapter(IWindsorContainer windsorContainer)
        {
            this.windsorContainer = windsorContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return windsorContainer.Resolve<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator)
            where TService : class
        {
            if(!windsorContainer.Kernel.HasComponent(typeof(TService)))
            {
                windsorContainer.Register(
                    Component.For<TService>().UsingFactoryMethod(() => serviceCreator(this)).LifeStyle.Singleton
                    );
            }
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            if(!windsorContainer.Kernel.HasComponent(typeof(TService)))
            {
                windsorContainer.Register(
                    Component.For<TService>().ImplementedBy<TImplementation>().LifeStyle.Singleton
                    );
            }
            return this;
        }

        public void Dispose()
        {
            windsorContainer.Dispose();
        }
    }
}