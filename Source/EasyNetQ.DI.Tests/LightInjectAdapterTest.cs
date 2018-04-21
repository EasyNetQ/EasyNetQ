using EasyNetQ.DI.LightInject;
using LightInject;

namespace EasyNetQ.DI.Tests
{
    public class LightInjectAdapterTest : ContainerAdapterTest<LightInjectAdapter>
    {
        public LightInjectAdapterTest()
            : base(() => new LightInjectAdapter(new ServiceContainer()))
        {
        }
    }
}