using Castle.Windsor;
using EasyNetQ.DI.Windsor;

namespace EasyNetQ.DI.Tests
{
    public class WindsorAdapterTests : ContainerAdapterTests
    {
        WindsorAdapter adapter;

        protected override IServiceRegister CreateServiceRegister()
        {
            return adapter = new WindsorAdapter(new WindsorContainer());
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return adapter;
        }
    }
}