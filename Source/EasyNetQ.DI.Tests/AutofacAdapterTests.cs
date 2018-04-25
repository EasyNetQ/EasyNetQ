using Autofac;
using EasyNetQ.DI.Autofac;

namespace EasyNetQ.DI.Tests
{
    public class AutofacAdapterTests : ContainerAdapterTests<ContainerBuilder>
    {
        public AutofacAdapterTests()
            : base(new ContainerBuilder(), s => new AutofacAdapter(s), s => s.Build().Resolve<IServiceResolver>())
        {
        }
    }
}