using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Default implementation of EasyNetQ's send-receive pattern
/// </summary>
public class DefaultSendReceive : ISendReceive
{
    private readonly ConnectionConfiguration configuration;
    private readonly IConventions conventions;
    private readonly IAdvancedBus advancedBus;
    private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

    /// <summary>
    ///     Creates DefaultSendReceive
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="conventions">The conventions</param>
    /// <param name="advancedBus">The advanced bus</param>
    /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
    public DefaultSendReceive(
        ConnectionConfiguration configuration,
        IConventions conventions,
        IAdvancedBus advancedBus,
        IMessageDeliveryModeStrategy messageDeliveryModeStrategy
    )
    {
        this.configuration = configuration;
        this.conventions = conventions;
        this.advancedBus = advancedBus;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(
        string queue, T message, Action<ISendConfiguration> configure, CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var sendConfiguration = new SendConfiguration();
        configure(sendConfiguration);

        var properties = new MessageProperties
        {
            Priority = sendConfiguration.Priority ?? 0,
            Headers = sendConfiguration.MessageHeaders,
            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
        };
        await advancedBus.PublishAsync(
            Exchange.DefaultName, queue, null, sendConfiguration.PublisherConfirms, new Message<T>(message, properties), cts.Token
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IDisposable> ReceiveAsync(
        string queue,
        Action<IReceiveRegistration> addHandlers,
        Action<IReceiveConfiguration> configure,
        CancellationToken cancellationToken
    ) => ReceiveInternalAsync(queue, addHandlers, configure, cancellationToken);

    private async Task<IDisposable> ReceiveInternalAsync(
        string queueName,
        Action<IReceiveRegistration> addHandlers,
        Action<IReceiveConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var receiveConfiguration = new ReceiveConfiguration(configuration.PrefetchCount);
        configure(receiveConfiguration);

        var queue = await advancedBus.QueueDeclareAsync(
            queue: queueName,
            durable: receiveConfiguration.Durable,
            autoDelete: receiveConfiguration.AutoDelete,
            arguments: receiveConfiguration.QueueArguments,
            cancellationToken: cts.Token
        ).ConfigureAwait(false);

        return advancedBus.Consume(
            queue,
            c => addHandlers(new HandlerAdder(c)),
            c => c.WithPrefetchCount(receiveConfiguration.PrefetchCount)
                .WithPriority(receiveConfiguration.Priority)
                .WithExclusive(receiveConfiguration.IsExclusive)
                .WithConsumerTag(conventions.ConsumerTagConvention())
        );
    }

    private sealed class HandlerAdder : IReceiveRegistration
    {
        private readonly IHandlerRegistration handlerRegistration;

        public HandlerAdder(IHandlerRegistration handlerRegistration)
        {
            this.handlerRegistration = handlerRegistration;
        }

        public IReceiveRegistration Add<T>(Func<T, CancellationToken, Task> onMessage)
        {
            handlerRegistration.Add<T>((message, _, c) => onMessage(message.Body!, c));
            return this;
        }
    }
}
