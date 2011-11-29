namespace EasyNetQ
{
    public interface ISerializer
    {
        byte[] MessageToBytes<T>(T message);
        T BytesToMessage<T>(byte[] bytes);
    }
}