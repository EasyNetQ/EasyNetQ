using Autofac;

namespace EasyNetQ.DI.Autofac
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder)
        {
            new AutofacAdapter(containerBuilder).RegisterDefaultServices();
            return containerBuilder;
        }
    }
}