using EasyNetQ.DI;
using EasyNetQ.Logging;
using EasyNetQ.Logging.Microsoft;

// ReSharper disable once CheckNamespace
namespace EasyNetQ;

/// <summary>
/// Registers loggers based on Microsoft.Extensions.Logging
/// </summary>
public static class ServiceRegisterExtensions
{
    /// <summary>
    /// Enables microsoft logging support for EasyNetQ.
    /// It is assumed that a caller has already registered Microsoft.Extensions.Logging types.
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableMicrosoftLogging(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register(typeof(ILogger<>), typeof(MicrosoftLoggerAdapter<>));
    }
}
