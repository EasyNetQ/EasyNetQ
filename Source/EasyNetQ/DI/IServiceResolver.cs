namespace EasyNetQ.DI
{
    /// <summary>
    /// Provides service instances
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Get an instance of the requested services. Note all services are singletons; multiple calls
        /// to Resolve will all return the same instance.
        /// </summary>
        /// <typeparam name="TService">The type of serivce to return</typeparam>
        /// <returns>The single instance of the service</returns>
        TService Resolve<TService>() where TService : class;

        IServiceResolverScope CreateScope();
    }
}