namespace EasyNetQ
{
    public interface IMessage<T>
    {
        MessageProperties Properties { get; }
        T Body { get; }
    }
}