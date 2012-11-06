using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;

#pragma warning disable 67

namespace EasyNetQ.Scheduler.Tests
{
    public class MockBus : IBus
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IPublishChannel OpenPublishChannel()
        {
            throw new NotImplementedException();
        }

        public IPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            throw new NotImplementedException();
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure)
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