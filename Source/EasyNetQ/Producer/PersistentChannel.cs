using System;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class PersistentChannel : IPersistentChannel
    {
        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;

        private IModel channel;

        public PersistentChannel(IPersistentConnection connection, IEasyNetQLogger logger)
        {
            this.connection = connection;

            this.connection.Disconnected += () => channel = null;
            this.connection.Connected += () => channel = connection.CreateModel();

            this.logger = logger;
        }

        public IModel Channel
        {
            get
            {
                if (channel == null || !channel.IsOpen)
                {
                    channel = connection.CreateModel();
                }
                return channel;
            }
        }

        public void Dispose()
        {
            if(channel != null) channel.Dispose();
        }

        public void InvokeChannelAction(Action<IModel> channelAction)
        {
            channelAction(Channel);
        }
    }
}