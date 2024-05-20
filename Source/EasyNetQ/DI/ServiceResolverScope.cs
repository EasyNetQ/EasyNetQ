// namespace EasyNetQ.DI;
//
// /// <inheritdoc />
// public class ServiceResolverScope : IServiceProviderScope
// {
//     private readonly IServiceProvider resolver;
//
//     /// <summary>
//     ///     Create ServiceResolverScope
//     /// </summary>
//     public ServiceResolverScope(IServiceProvider resolver) => this.resolver = resolver;
//
//     /// <inheritdoc />
//     public TService Resolve<TService>() where TService : class => resolver.Resolve<TService>();
//
//     /// <inheritdoc />
//     public IServiceProviderScope CreateScope() => new ServiceResolverScope(this);
//
//     /// <inheritdoc />
//     public void Dispose()
//     {
//     }
// }
