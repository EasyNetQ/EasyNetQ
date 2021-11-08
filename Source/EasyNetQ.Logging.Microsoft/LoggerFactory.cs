using MS = Microsoft.Extensions.Logging;
using MSExtensions = Microsoft.Extensions.Logging.LoggerExtensions;
using MSFactoryExtensions = Microsoft.Extensions.Logging.LoggerFactoryExtensions;

namespace EasyNetQ.Logging.Microsoft
{
    /// <summary>
    ///     Creates ILogger
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        ///     Creates ILogger based on category
        /// </summary>
        /// <typeparam name="TCategory"></typeparam>
        ILogger Create<TCategory>();

        /// <summary>
        ///     Creates ILogger based on category
        /// </summary>
        ILogger Create();
    }


    /// <inheritdoc />
    public sealed class LoggerFactory : ILoggerFactory
    {
        private readonly MS.ILoggerFactory loggerFactory;

        /// <summary>
        ///     Creates LoggerFactory on top of Microsoft.Extensions.Logger.ILoggerFactory
        /// </summary>
        /// <param name="loggerFactory"></param>
        public LoggerFactory(MS.ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }


        /// <inheritdoc />
        public ILogger Create<TCategory>()
        {
            return new MicrosoftLoggerAdapter<TCategory>(MSFactoryExtensions.CreateLogger<TCategory>(loggerFactory));
        }

        /// <inheritdoc />
        public ILogger Create()
        {
            return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger("EasyNetQ"));
        }
    }
}
