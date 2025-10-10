using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.DI
{
    internal sealed class MicrosoftDIServiceResolver : IServiceResolver
    {
        private readonly IServiceProvider _serviceProvider;
        public MicrosoftDIServiceResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);

        public TService Resolve<TService>() where TService : class
        {
            return _serviceProvider.GetService<TService>();
        }
    }    
}
