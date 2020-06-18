namespace EasyNetQ.DI
{
    /// <inheritdoc />
    public class ServiceResolverScope : IServiceResolverScope
    {
        private readonly IServiceResolver resolver;

        public ServiceResolverScope(IServiceResolver resolver)
        {
            this.resolver = resolver;
        }

        /// <inheritdoc />
        public TService Resolve<TService>() where TService : class
        {
            return resolver.Resolve<TService>();
        }

        /// <inheritdoc />
        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
