﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
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

        private readonly AsyncLock mutex = new AsyncLock();
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly PersistentChannelOptions options;

        private volatile IModel initializedChannel;

        /// <summary>
        /// Creates PersistentChannel
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connection">The connection</param>
        /// <param name="eventBus">The event's bus</param>
        public PersistentChannel(PersistentChannelOptions options, IPersistentConnection connection, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connection = connection;
            this.eventBus = eventBus;
            this.options = options;
        }

        /// <inheritdoc />
        public async Task<T> InvokeChannelActionAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            using var releaser = await mutex.AcquireAsync(cancellationToken).ConfigureAwait(false);

            var retryTimeoutMs = MinRetryTimeoutMs;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return channelAction(initializedChannel ??= CreateChannel());
                }
                catch (OperationInterruptedException exception)
                {
                    var verdict = GetAmqpExceptionVerdict(exception.ShutdownReason);
                    if (verdict.NeedCloseChannel)
                    {
                        CloseChannel(initializedChannel);
                        initializedChannel = null;
                    }

                    if (verdict.NeedRethrow)
                        throw;
                }
                catch (EasyNetQException)
                {
                }

                await Task.Delay(retryTimeoutMs, cancellationToken).ConfigureAwait(false);
                retryTimeoutMs = Math.Min(retryTimeoutMs * 2, MaxRetryTimeoutMs);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            mutex.Dispose();
            CloseChannel(initializedChannel);
            initializedChannel = null;
        }

        private IModel CreateChannel()
        {
            var channel = connection.CreateModel();
            AttachChannelEvents(channel);
            return channel;
        }

        private void CloseChannel(IModel channel)
        {
            if (channel == null)
                return;

            channel.Close();
            DetachChannelEvents(channel);
            channel.Dispose();
        }

        private void AttachChannelEvents(IModel channel)
        {
            if (options.PublisherConfirms)
            {
                channel.ConfirmSelect();

                channel.BasicAcks += OnAck;
                channel.BasicNacks += OnNack;
            }

            channel.BasicReturn += OnReturn;
            channel.ModelShutdown += OnChannelShutdown;

            if (channel is IRecoverable recoverable)
                recoverable.Recovery += OnChannelRecovered;
            else
                throw new NotSupportedException("Non-recoverable channel is not supported");
        }

        private void DetachChannelEvents(IModel channel)
        {
            if (channel is IRecoverable recoverable)
                recoverable.Recovery -= OnChannelRecovered;

            channel.ModelShutdown -= OnChannelShutdown;
            channel.BasicReturn -= OnReturn;

            if (!options.PublisherConfirms)
                return;

            channel.BasicNacks -= OnNack;
            channel.BasicAcks -= OnAck;
        }

        private void OnChannelRecovered(object sender, EventArgs e)
        {
            eventBus.Publish(new ChannelRecoveredEvent((IModel)sender));
        }

        private void OnChannelShutdown(object sender, ShutdownEventArgs e)
        {
            eventBus.Publish(new ChannelShutdownEvent((IModel)sender));
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

        private static AmqpExceptionVerdict GetAmqpExceptionVerdict(ShutdownEventArgs shutdownReason)
        {
            return shutdownReason?.ReplyCode switch
            {
                AmqpErrorCodes.ConnectionClosed => new AmqpExceptionVerdict(false, false),
                AmqpErrorCodes.AccessRefused => new AmqpExceptionVerdict(true, true),
                AmqpErrorCodes.NotFound => new AmqpExceptionVerdict(true, true),
                AmqpErrorCodes.ResourceLocked => new AmqpExceptionVerdict(true, true),
                AmqpErrorCodes.PreconditionFailed => new AmqpExceptionVerdict(true, true),
                _ => new AmqpExceptionVerdict(true, false)
            };
        }

        private readonly struct AmqpExceptionVerdict
        {
            public AmqpExceptionVerdict(bool needRethrow, bool needCloseChannel)
            {
                NeedRethrow = needRethrow;
                NeedCloseChannel = needCloseChannel;
            }

            public bool NeedRethrow { get; }
            public bool NeedCloseChannel { get; }
        }
    }
}
