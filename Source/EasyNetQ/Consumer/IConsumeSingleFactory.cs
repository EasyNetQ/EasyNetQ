using System;
using EasyNetQ.Producer;

namespace EasyNetQ.Consumer
{
    public interface IConsumeSingleFactory
    {
        IConsumeSingle Create(IPersistentConnection connection);
    }

    class DefaultConsumeSingleFactory : IConsumeSingleFactory
    {
        private readonly IClientCommandDispatcherFactory _commandDispatcherFactory;

        public DefaultConsumeSingleFactory(IClientCommandDispatcherFactory commandDispatcherFactory)
        {
            _commandDispatcherFactory = commandDispatcherFactory;
        }

        public IConsumeSingle Create(IPersistentConnection connection)
        {
            return new DefaultConsumeSingle(_commandDispatcherFactory, connection);
        }
    }
}