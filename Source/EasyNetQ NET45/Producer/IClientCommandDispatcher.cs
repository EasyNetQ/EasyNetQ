using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Responsible for invoking client commands.
    /// </summary>
    public interface IClientCommandDispatcher : IDisposable
    {
        T Invoke<T>(Func<IModel, T> channelAction);
        void Invoke(Action<IModel> channelAction);
        Task<T> InvokeAsync<T>(Func<IModel, T> channelAction);
        Task InvokeAsync(Action<IModel> channelAction);
    }
}