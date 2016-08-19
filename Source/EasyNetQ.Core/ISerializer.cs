namespace EasyNetQ
{
    public interface ISerializer
    {
        byte[] MessageToBytes<T>(T message) where T : class;
        T BytesToMessage<T>(byte[] bytes);
        object BytesToMessage(string typeName, byte[] bytes);
    }
}