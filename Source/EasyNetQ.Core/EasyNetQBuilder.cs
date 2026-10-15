using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

internal sealed class EasyNetQBuilder(IServiceCollection services) : IEasyNetQBuilder
{
    public IServiceCollection Services { get; } = services;
}
