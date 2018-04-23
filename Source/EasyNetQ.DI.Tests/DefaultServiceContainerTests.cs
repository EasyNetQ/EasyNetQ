namespace EasyNetQ.DI.Tests
{
    public class DefaultServiceContainerTests : ContainerAdapterTests
    {
        DefaultServiceContainer container;

        protected override IServiceRegister CreateServiceRegister()
        {
            return this.container = new DefaultServiceContainer();
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return this.container;
        }
    }
}