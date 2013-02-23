namespace EasyNetQ
{
    public interface IMessage
    {
        MessageProperties Properties { get; }
        object Body { get; }
    }
    public interface IMessage<T> : IMessage
    {
        T GetBody();
    }
}