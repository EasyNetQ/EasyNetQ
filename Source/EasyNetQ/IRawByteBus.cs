namespace EasyNetQ
{
    public interface IRawByteBus
    {
        void RawPublish(string exchangeName, byte[] messageBody);
    }
}