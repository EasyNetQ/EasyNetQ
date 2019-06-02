using System;

namespace EasyNetQ.DI
{
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
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <param name="lifetime">A lifetime of a container registration</param>
        /// <returns>itself for nice fluent composition</returns>
        IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Register a service. Note that the first registration wins. All subsequent registrations
        /// will be ignored.
        /// </summary>
        /// <typeparam name="TService">The type of the service to be registered</typeparam>
        /// <param name="instance">The instance of the service</param>
        /// <returns>itself for nice fluent composition</returns>
        IServiceRegister Register<TService>(TService instance)
            where TService : class;

        /// <summary>
        /// Register a service. Note that the first registration wins. All subsequent registrations
        /// will be ignored.
        /// </summary>
        /// <typeparam name="TService">The type of the service to be registered</typeparam>
        /// <param name="factory">The instance factory of the service</param>
        /// <param name="lifetime">A lifetime of a container registration</param>
        /// <returns>itself for nice fluent composition</returns>
        IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton)
            where TService : class;
    }
}
