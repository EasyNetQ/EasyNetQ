using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public interface IHandlerRunner : IDisposable
{
    [Obsolete("Unused anymore")]
    Task<AckStrategyAsync> InvokeUserMessageHandlerAsync(ConsumerExecutionContext context, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class HandlerRunner : IHandlerRunner
{
    private readonly ILogger<HandlerRunner> logger;

    public HandlerRunner(ILogger<HandlerRunner> logger)
    {
        Preconditions.CheckNotNull(logger, nameof(logger));
       
        this.logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task<AckStrategyAsync> InvokeUserMessageHandlerAsync(
        ConsumerExecutionContext context, CancellationToken cancellationToken
    )
    {
        if (logger.IsDebugEnabled())
        {
            logger.Debug("Received message with receivedInfo={receivedInfo}", context.ReceivedInfo);
        }

        return AckStrategies.NackWithRequeueAsync;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
    }
}
