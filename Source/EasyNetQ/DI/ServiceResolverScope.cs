namespace EasyNetQ.DI
{
    public class ServiceResolverScope : IServiceResolverScope
    {
        private readonly IServiceResolver resolver;
        
        public ServiceResolverScope(IServiceResolver resolver)
        {
            this.resolver = resolver;
        }

        public TService Resolve<TService>() where TService : class
        {
            return resolver.Resolve<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }

        public void Dispose()
        {
        }
    }
}