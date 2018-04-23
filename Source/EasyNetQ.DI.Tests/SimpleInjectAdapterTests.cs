using EasyNetQ.DI.SimpleInjector;
using SimpleInjector;

namespace EasyNetQ.DI.Tests
{
    public class SimpleInjectAdapterTests : ContainerAdapterTests
    {
        SimpleInjectorAdapter adapter;

        protected override IServiceRegister CreateServiceRegister()
        {
            var container = new Container { Options = { AllowOverridingRegistrations = true } };

            return this.adapter = new SimpleInjectorAdapter(container);
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return this.adapter;
        }
    }
}