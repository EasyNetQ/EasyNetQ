using EasyNetQ.DI;
using EasyNetQ.Logging;
using EasyNetQ.Logging.Serilog;
using S = Serilog;

// ReSharper disable once CheckNamespace
namespace EasyNetQ;

/// <summary>
///     Register loggers based of Microsoft.Extensions.Logging
/// </summary>
public static class ServiceRegisterExtensions
{
    /// <summary>
    ///     Enables serilog logging support for EasyNetQ. It should be already registered in DI.
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableSerilogLogging(this IServiceRegister serviceRegister)
    {
        return serviceRegister
            .Register(typeof(ILogger), typeof(SerilogLoggerAdapter))
            .Register(typeof(ILogger<>), typeof(SerilogLoggerAdapter<>));
    }
}
