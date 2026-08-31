using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

public interface IEasyNetQBuilder
{
    IServiceCollection Services { get; }
}
