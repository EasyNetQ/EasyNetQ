using EasyNetQ.DI.LightInject;
using LightInject;

namespace EasyNetQ.DI.Tests
{
    public class LightInjectAdapterTests : ContainerAdapterTests<LightInjectAdapter>
    {
        public LightInjectAdapterTests()
            : base(() => new LightInjectAdapter(new ServiceContainer()))
        {
        }
    }
}