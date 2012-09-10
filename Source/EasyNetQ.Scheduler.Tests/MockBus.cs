using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable 67

namespace EasyNetQ.Scheduler.Tests
{
    public class MockBus : IBus
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T message)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(string topic, T message)
        {
            throw new NotImplementedException();
        }

        public IPublishChannel OpenPublishChannel()
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            throw new NotImplementedException();
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            throw new NotImplementedException();
        }
        
        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            throw new NotImplementedException();
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void FuturePublish<T>(DateTime timeToRespond, T message)
        {
            throw new NotImplementedException();
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
}

#pragma warning restore 67