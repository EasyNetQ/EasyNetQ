namespace EasyNetQ.DI
{
    /// <summary>
    /// Provides service instances and creates separate scopes
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Get an instance of the requested services
        /// </summary>
        /// <typeparam name="TService">The type of service to return</typeparam>
        /// <returns>The instance of the service</returns>
        TService Resolve<TService>() where TService : class;

        /// <summary>
        /// Begin a new scope. Component instances created via the new scope
        /// will be disposed along with it
        /// </summary>
        /// <returns>A new scope</returns>
        IServiceResolverScope CreateScope();
    }
}