using System;

namespace EasyNetQ
{
    /// <summary>
    /// Provides service instances
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Get an instance of the requested services. Note all services are singletons; multiple calls
        /// to Resolve will all return the same instance.
        /// </summary>
        /// <typeparam name="TService">The type of serivce to return</typeparam>
        /// <returns>The single instance of the service</returns>
        TService Resolve<TService>() where TService : class;
    }

    /// <summary>
    /// Register services
    /// </summary>
    public interface IServiceRegister
    {
        /// <summary>
        /// Register a service. Note that the first registration wins. All subsequent registrations
        /// will be ignored.
        /// </summary>
        /// <typeparam name="TService">The type of the service to be registered</typeparam>
        /// <param name="serviceCreator">A function that can create an instance of the service</param>
        /// <returns>itself for nice fluent composition</returns>
        IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class;
    }
}