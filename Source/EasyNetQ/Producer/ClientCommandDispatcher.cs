using System;
using System.Threading;
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

        public ClientCommandDispatcher(ConnectionConfiguration configuration, IPersistentConnection connection, IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            dispatcher = new Lazy<IClientCommandDispatcher>(
                () => new ClientCommandDispatcherSingleton(configuration, connection, persistentChannelFactory));
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.InvokeAsync(channelAction, cancellationToken);
        }

        public Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.InvokeAsync(channelAction, cancellationToken);
        }

        public void Dispose()
        {
            if (dispatcher.IsValueCreated) dispatcher.Value.Dispose();
        }
    }
}
