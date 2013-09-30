using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Invokes client commands on a single channel. All commands are marshalled onto
    /// a single thread.
    /// </summary>
    public class ClientCommandDispatcher : IClientCommandDispatcher
    {
        private readonly Lazy<IClientCommandDispatcher> dispatcher;

        public ClientCommandDispatcher(IPersistentConnection connection, IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(logger, "logger");

            dispatcher = new Lazy<IClientCommandDispatcher>(() =>
                                                            new ClientCommandDispatcherSingleton(connection, logger));
        }

        public Task<T> Invoke<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.Invoke(channelAction);
        }

        public void Dispose()
        {
            if(dispatcher.IsValueCreated) dispatcher.Value.Dispose();
        }
    }
}