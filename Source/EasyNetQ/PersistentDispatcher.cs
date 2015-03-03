using EasyNetQ.Producer;

namespace EasyNetQ
{
    class PersistentDispatcher : IPersistentDispatcher
    {
        private readonly IPersistentConnection _connection;
        private readonly IClientCommandDispatcher _clientCommandDispatcher;

        internal PersistentDispatcher(IPersistentConnection connection, IClientCommandDispatcher clientCommandDispatcher)
        {
            _connection = connection;
            _clientCommandDispatcher = clientCommandDispatcher;
        }

        public IPersistentConnection Connection
        {
            get { return _connection; }
        }

        public IClientCommandDispatcher CommandDispatcher
        {
            get { return _clientCommandDispatcher; }
        }
    }
}