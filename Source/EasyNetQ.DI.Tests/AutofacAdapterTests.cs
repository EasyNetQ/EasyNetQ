using Autofac;
using EasyNetQ.DI.Autofac;

namespace EasyNetQ.DI.Tests
{
    public class AutofacAdapterTests : ContainerAdapterTests
    {
        ContainerBuilder builder;
        IContainer container;

        protected override IServiceRegister CreateServiceRegister()
        {
            builder = new ContainerBuilder();
            return new AutofacAdapter(builder);
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            this.container = builder.Build();
            
            return container.Resolve<IServiceResolver>();
        }
    }
}