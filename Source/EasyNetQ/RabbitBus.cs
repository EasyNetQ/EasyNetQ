using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly SerializeType serializeType;
        private readonly IEasyNetQLogger logger;
		private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;

        private readonly ConcurrentDictionary<int, string> responseQueueNameCache = new ConcurrentDictionary<int, string>();

        public const string RpcExchange = "easy_net_q_rpc";

        public SerializeType SerializeType
        {
            get { return serializeType; }
        }

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public IConventions Conventions
        {
            get { return conventions; }
        }

        public ConcurrentDictionary<int, string> ResponseQueueNameCache
        {
            get { return responseQueueNameCache; }
        }

        public RabbitBus(
            SerializeType serializeType, 
            IEasyNetQLogger logger,
			IConventions conventions, 
            IAdvancedBus advancedBus)
        {
            if(serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if(logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if(conventions == null)
            {
                throw new ArgumentNullException("conventions");
            }

            this.serializeType = serializeType;
            this.logger = logger;
			this.conventions = conventions;
            this.advancedBus = advancedBus;
        }

        public IPublishChannel OpenPublishChannel()
        {
            return new RabbitPublishChannel(this);
        }

        public void CheckMessageType<TMessage>(IBasicProperties properties)
        {
            var typeName = serializeType(typeof (TMessage));
            if (properties.Type != typeName)
            {
                logger.ErrorWrite("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);

                throw new EasyNetQInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);
            }
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            Subscribe(subscriptionId, "#", onMessage);
        }

        public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage)
        {
            Subscribe(subscriptionId, Enumerable.Repeat(topic, 1), onMessage);
        }

        public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage)
        {
            SubscribeAsync<T>(subscriptionId, topics, msg =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    onMessage(msg);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            });
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            SubscribeAsync(subscriptionId, "#", onMessage);
        }

        public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage)
        {
            SubscribeAsync(subscriptionId, Enumerable.Repeat(topic, 1), onMessage);
        }

        public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage)
        {
            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var queueName = GetQueueName<T>(subscriptionId);
            var exchangeName = GetExchangeName<T>();

            var queue = Queue.DeclareDurable(queueName);
            var exchange = Exchange.DeclareTopic(exchangeName);
            queue.BindTo(exchange, topics.ToArray());

            advancedBus.Subscribe<T>(queue, (message, messageRecievedInfo) => onMessage(message.Body));
        }

		private string GetExchangeName<T>()
		{
			return conventions.ExchangeNamingConvention(typeof(T));
		}

		private string GetQueueName<T>(string subscriptionId)
		{
			return conventions.QueueNamingConvention(typeof (T), subscriptionId);
		}

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            if(responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            Func<TRequest, Task<TResponse>> taskResponder = 
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder);
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));

            var exchange = Exchange.DeclareDirect(RpcExchange);
            var queue = Queue.DeclareDurable(requestTypeName);
            queue.BindTo(exchange, requestTypeName);

            advancedBus.Subscribe<TRequest>(queue, (requestMessage, messageRecievedInfo) =>
            {
                var tcs = new TaskCompletionSource<object>();

                responder(requestMessage.Body).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception != null)
                        {
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        // check we're connected
                        while (!advancedBus.IsConnected) Thread.Sleep(100);

                        using (var channel = advancedBus.OpenPublishChannel())
                        {
                            var responseMessage = new Message<TResponse>(task.Result);
                            channel.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, responseMessage);
                        }
                        tcs.SetResult(null);
                    }
                });

                return tcs.Task;
            });
        }

        public event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();
        }

        public event Action Disconnected;

        protected void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        public bool IsConnected
        {
            get { return advancedBus.IsConnected; }
        }

        public IAdvancedBus Advanced
        {
            get { return advancedBus; }
        }

        public void Dispose()
        {
            advancedBus.Dispose();
        }
    }
}