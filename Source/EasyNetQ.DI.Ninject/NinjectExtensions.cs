using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public static class NinjectExtensions
    {
        public static IKernel RegisterEasyNetQ(this IKernel serviceContainer)
        {
            new NinjectAdapter(serviceContainer).RegisterDefaultServices();
            return serviceContainer;
        }
    }
}