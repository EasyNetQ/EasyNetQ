using System;
using Autofac;

namespace EasyNetQ.DI
{
    public class AutofacAdapter : IContainer, IDisposable
    {
        private ContainerBuilder initialBuilder;
        private Autofac.IContainer container;
        private bool ownsContainer;

        public AutofacAdapter(ContainerBuilder initialBuilder = null)
        {
            this.initialBuilder = initialBuilder ?? new ContainerBuilder();

            this.initialBuilder
                .RegisterInstance(this)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
        }

        public Autofac.IContainer Container 
        { 
            get
            {
                if (container != null) 
                    return container;

                container = initialBuilder.Build();
                initialBuilder = null;
                ownsContainer = true;

                return container;
            }
            set { container = value; }
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            if (serviceCreator == null) 
                throw new ArgumentNullException("serviceCreator");

            var builder = initialBuilder ?? new ContainerBuilder();

            builder
                .Register(c => serviceCreator(this))
                .SingleInstance();

            if (container != null && !container.IsRegistered<TService>())
                builder.Update(container);

            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var builder = initialBuilder ?? new ContainerBuilder();

            builder
                .RegisterType<TImplementation>()
                .As<TService>()
                .SingleInstance();

            if (container != null && !container.IsRegistered<TService>())
                builder.Update(container);
            
            return this;
        }

        public TService Resolve<TService>() where TService : class
        {
            return Container.Resolve<TService>();
        }

        public void Dispose()
        {
            if(ownsContainer && container != null)
                container.Dispose();
        }
    }
}
