using EasyNetQ.DI.SimpleInjector;
using SimpleInjector;

namespace EasyNetQ.DI.Tests
{
    public class SimpleInjectAdapterTests : ContainerAdapterTests<SimpleInjectorAdapter>
    {
        public SimpleInjectAdapterTests() 
            : base(new SimpleInjectorAdapter(new Container { Options = { AllowOverridingRegistrations = true } }), s => s, s => s)
        {
        }
    }
}