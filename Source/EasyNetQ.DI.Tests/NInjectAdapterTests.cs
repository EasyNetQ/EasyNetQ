using EasyNetQ.DI.Ninject;
using Ninject;

namespace EasyNetQ.DI.Tests
{
    public class NInjectAdapterTests : ContainerAdapterTests<NinjectAdapter>
    {
        public NInjectAdapterTests()
            : base(new NinjectAdapter(new StandardKernel()), s => s, s => s)
        {
        }
    }
}