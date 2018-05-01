using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public static class SimpleInjectorExtensions
    {
        public static Container RegisterEasyNetQ(this Container container)
        {
            new SimpleInjectorAdapter(container).RegisterDefaultServices();
            return container;
        }
    }
}