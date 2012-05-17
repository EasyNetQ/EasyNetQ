using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    /// An in-memory implementation of IBus.
    /// Use this for unit testing your serivces and sagas.
    /// </summary>
    public class InMemoryBus : IBus
    {
        private readonly Dictionary<Type, List<object>> subscriptions = new Dictionary<Type, List<object>>(); 
        private readonly Dictionary<Type, List<object>> asyncSubscriptions = new Dictionary<Type, List<object>>();

        public Dictionary<Type, List<object>> Subscriptions
        {
            get { return subscriptions; }
        }

        public Dictionary<Type, List<object>> AsyncSubscriptions
        {
            get { return asyncSubscriptions; }
        }

        public void Dispose()
        {
            // nothing to do here
        }

        public void Publish<T>(T message)
        {
            var messageType = typeof (T);
            if (subscriptions.ContainsKey(messageType))
            {
                foreach (var subscribeAction in subscriptions[messageType])
                {
                    ((Action<T>) subscribeAction)(message);
                }
            }
            if (asyncSubscriptions.ContainsKey(messageType))
            {
                foreach (var subscribeAction in asyncSubscriptions[messageType])
                {
                    var task = ((Func<T, Task>)subscribeAction)(message);
                    task.Wait();
                }
            }
        }

        public IPublishChannel OpenPublishChannel()
        {
            return new InMemoryPublishChannel(this);
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            var messageType = typeof (T);
            if (!subscriptions.ContainsKey(messageType))
            {
                subscriptions.Add(messageType, new List<object>());
            }
            subscriptions[messageType].Add(onMessage);
        }

        public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            var messageType = typeof(T);
            if (!asyncSubscriptions.ContainsKey(messageType))
            {
                asyncSubscriptions.Add(messageType, new List<object>());
            }
            asyncSubscriptions[messageType].Add(onMessage);
        }

        public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            // TODO: deal with multiple calls to Request (cache and resue onResponse)
            Subscribe("id", onResponse);
            Publish(request);
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            Subscribe<TRequest>("id", request =>
            {
                var response = responder(request);
                Publish(response);
            });
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            Subscribe<TRequest>("id", request =>
            {
                var task = responder(request);
                task.Wait();
                Publish(task.Result);
            });
        }

        public event Action Connected;
        public event Action Disconnected;

        public bool IsConnected
        {
            get { return true; }
        }

        public IAdvancedBus Advanced
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InMemoryPublishChannel : IPublishChannel
    {
        private readonly InMemoryBus inMemoryBus;

        public InMemoryPublishChannel(InMemoryBus inMemoryBus)
        {
            this.inMemoryBus = inMemoryBus;
        }

        public void Dispose()
        {
            
        }

        public void Publish<T>(T message)
        {
            inMemoryBus.Publish(message);
        }

        public void Publish<T>(string topic, T message)
        {
            throw new NotImplementedException();
        }

        public void FuturePublish<T>(DateTime timeToRespond, T message)
        {
            throw new NotImplementedException();
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            inMemoryBus.Request(request, onResponse);
        }
    }
}