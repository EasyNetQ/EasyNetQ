using EasyNetQ.DI.LightInject;
using LightInject;

namespace EasyNetQ.DI.Tests
{
    public class LightInjectAdapterTests : ContainerAdapterTests
    {
        LightInjectAdapter adapter;

        protected override IServiceRegister CreateServiceRegister()
        {
            var serviceContainer = new ServiceContainer();
           
            return adapter =  new LightInjectAdapter(serviceContainer);
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return adapter;
        }
    }
}