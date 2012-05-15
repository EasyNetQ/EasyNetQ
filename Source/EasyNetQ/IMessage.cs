using EasyNetQ.SystemMessages;

namespace EasyNetQ
{
    public interface IMessage<T>
    {
        MessageBasicProperties Properties { get; }
        T Body { get; }
    }
}