A Nice .NET API for AMQP

Goals:
1. Zero or at least minimal configuration.
2. Simple API

    public interface IBus : IDisposable
    {
        void Publish<T>(T message);
        void Subscribe<T>(Action<T> onMessage);

        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse);
        void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder);
    }