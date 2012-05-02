namespace EasyNetQ
{
    public interface IRawByteBus
    {
        void RawPublish(string exchangeName, byte[] messageBody);
        void RawPublish(string exchangeName, string topic, byte[] messageBody);
    }
}