using EasyNetQ.Logging;

namespace EasyNetQ.Consumer;

public interface IHandlerRunner
{
    Task<AckStrategy> InvokeUserMessageHandlerAsync(ConsumerExecutionContext context, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IHandlerRunner"/>
public class HandlerRunner : IHandlerRunner
{
    private readonly IConsumerErrorStrategy consumerErrorStrategy;
    private readonly ILogger logger;

    public HandlerRunner(ILogger<IHandlerRunner> logger, IConsumerErrorStrategy consumerErrorStrategy)
    {
        this.logger = logger;
        this.consumerErrorStrategy = consumerErrorStrategy;
    }

    /// <inheritdoc />
    public virtual async Task<AckStrategy> InvokeUserMessageHandlerAsync(
        ConsumerExecutionContext context, CancellationToken cancellationToken
    )
    {
        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Received message with receivedInfo={receivedInfo}", context.ReceivedInfo);
        }

        try
        {
            try
            {
                return await context.Handler(context.Body, context.Properties, context.ReceivedInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return await consumerErrorStrategy.HandleConsumerCancelledAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return await consumerErrorStrategy.HandleConsumerErrorAsync(context, exception, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Consumer error strategy has failed");
            return AckStrategies.NackWithRequeue;
        }
    }
}
