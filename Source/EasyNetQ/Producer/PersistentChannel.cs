using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
using EasyNetQ.Sprache;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Producer
{
    /// <inheritdoc />
    public class PersistentChannel : IPersistentChannel
    {
        private const int MinRetryTimeoutMs = 50;
        private const int MaxRetryTimeoutMs = 5000;

        private readonly ConnectionConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;

        private volatile IModel initializedChannel;

        /// <summary>
        /// Creates PersistentChannel
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="eventBus">The event's bus</param>
        public PersistentChannel(
            IPersistentConnection connection,
            ConnectionConfiguration configuration,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.configuration = configuration;
            this.connection = connection;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public async Task<T> InvokeChannelActionAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var retryTimeoutMs = MinRetryTimeoutMs;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return channelAction(initializedChannel ??= CreateChannel());
                }
                catch (OperationInterruptedException exception) when(!NeedRethrow(exception))
                {
                }
                catch (EasyNetQException)
                {
                }

                await Task.Delay(retryTimeoutMs, cancellationToken).ConfigureAwait(false);
                retryTimeoutMs = Math.Min(retryTimeoutMs * 2, MaxRetryTimeoutMs);
            }
        }

        /// <inheritdoc />
        public void Dispose() => initializedChannel?.Dispose();

        private IModel CreateChannel()
        {
            var channel = connection.CreateModel();
            WireUpChannelEvents(channel);
            eventBus.Publish(new PublishChannelCreatedEvent(channel));
            return channel;
        }

        private void WireUpChannelEvents(IModel channel)
        {
            if (configuration.PublisherConfirms)
            {
                channel.ConfirmSelect();

                channel.BasicAcks += OnAck;
                channel.BasicNacks += OnNack;
            }

            channel.BasicReturn += OnReturn;

            if (channel is IRecoverable recoverable)
                recoverable.Recovery += OnChannelRestored;
            else
                throw new NotSupportedException("Non-recoverable channel is not supported");
        }

        private void OnChannelRestored(object sender, EventArgs e)
        {
            eventBus.Publish(new PublishChannelCreatedEvent((IModel)sender));
        }

        private void OnReturn(object sender, BasicReturnEventArgs args)
        {
            var returnedMessageEvent = new ReturnedMessageEvent(
                args.Body.ToArray(),
                new MessageProperties(args.BasicProperties),
                new MessageReturnedInfo(args.Exchange, args.RoutingKey, args.ReplyText)
            );
            eventBus.Publish(returnedMessageEvent);
        }

        private void OnAck(object sender, BasicAckEventArgs args)
        {
            eventBus.Publish(MessageConfirmationEvent.Ack((IModel)sender, args.DeliveryTag, args.Multiple));
        }

        private void OnNack(object sender, BasicNackEventArgs args)
        {
            eventBus.Publish(MessageConfirmationEvent.Nack((IModel)sender, args.DeliveryTag, args.Multiple));
        }

        private static bool NeedRethrow(OperationInterruptedException exception)
        {
            try
            {
                var amqpException = AmqpExceptionGrammar.ParseExceptionString(exception.Message);
                return amqpException.Code != AmqpException.ConnectionClosed;
            }
            catch (ParseException)
            {
                return true;
            }
        }
    }
}
