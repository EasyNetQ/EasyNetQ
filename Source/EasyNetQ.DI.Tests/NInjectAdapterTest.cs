using EasyNetQ.DI.Ninject;
using Ninject;

namespace EasyNetQ.DI.Tests
{
    public class NInjectAdapterTest : ContainerAdapterTest<NinjectAdapter>
    {
        public NInjectAdapterTest()
            : base(() => new NinjectAdapter(new StandardKernel()))
        {
        }
    }
}