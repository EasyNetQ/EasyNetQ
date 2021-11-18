using EasyNetQ.DI;
using EasyNetQ.Logging;
using EasyNetQ.Logging.Microsoft;
using MS = Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace EasyNetQ
{
    /// <summary>
    ///     Register loggers based of Microsoft.Extensions.Logging
    /// </summary>
    public static class ServiceRegisterExtensions
    {
        /// <summary>
        ///     Enables microsoft logging support for EasyNetQ. It should be already registered in DI.
        /// </summary>
        /// <param name="serviceRegister">The register</param>
        public static IServiceRegister EnableMicrosoftLogging(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .Register(typeof(ILogger), typeof(MicrosoftLoggerAdapter))
                .Register(typeof(ILogger<>), typeof(MicrosoftLoggerAdapter<>));
        }
    }
}
