﻿using System;
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

        public ClientCommandDispatcher(IPersistentConnection connection, IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            dispatcher = new Lazy<IClientCommandDispatcher>(
                () => new ClientCommandDispatcherSingleton(connection, persistentChannelFactory));
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.InvokeAsync(channelAction);
        }

        public Task InvokeAsync(Action<IModel> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.InvokeAsync(channelAction);
        }

        public void Dispose()
        {
            if(dispatcher.IsValueCreated) dispatcher.Value.Dispose();
        }
    }
}