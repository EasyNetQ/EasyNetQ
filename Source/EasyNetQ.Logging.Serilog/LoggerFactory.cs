using S = Serilog;

namespace EasyNetQ.Logging.Serilog
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
        private readonly S.ILogger logger;

        /// <summary>
        ///     Creates LoggerFactory on top of Serilog.ILogger
        /// </summary>
        /// <param name="logger"></param>
        public LoggerFactory(S.ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public ILogger Create<TCategory>() => new SerilogLoggerAdapter<TCategory>(logger);

        /// <inheritdoc />
        public ILogger Create() => new SerilogLoggerAdapter(logger);
    }
}
