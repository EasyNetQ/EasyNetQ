using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public static class LightInjectExtensions
    {
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer)
        {
            new LightInjectAdapter(serviceContainer).RegisterDefaultServices();
            return serviceContainer;
        }
    }
}