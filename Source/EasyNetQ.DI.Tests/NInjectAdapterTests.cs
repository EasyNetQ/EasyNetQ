using EasyNetQ.DI.Ninject;
using Ninject;

namespace EasyNetQ.DI.Tests
{
    public class NInjectAdapterTests : ContainerAdapterTests
    {
        NinjectAdapter adapter;

        public NInjectAdapterTests()
        {
        }

        protected override IServiceRegister CreateServiceRegister()
        {
            return this.adapter = new NinjectAdapter(new StandardKernel());
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return this.adapter;
        }
    }
}