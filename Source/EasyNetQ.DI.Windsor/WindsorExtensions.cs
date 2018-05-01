using Castle.Windsor;

namespace EasyNetQ.DI.Windsor
{
    public static class WindsorExtensions
    {
        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container)
        {
            new WindsorAdapter(container).RegisterDefaultServices();
            return container;
        }
    }
}