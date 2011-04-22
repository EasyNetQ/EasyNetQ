using System;

namespace EasyNetQ
{
    public interface IBus : IDisposable
    {
        void Publish<T>(T message);
        void Subscribe<T>(string subscriptionId, Action<T> onMessage);

        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse);
        void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder);
    }
}