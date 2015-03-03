using EasyNetQ.Producer;

namespace EasyNetQ
{
    public interface IPersistentDispatcher
    {
        IPersistentConnection Connection { get; }
        IClientCommandDispatcher CommandDispatcher { get; }
    }
}