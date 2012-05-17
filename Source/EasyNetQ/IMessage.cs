namespace EasyNetQ
{
    public interface IMessage<T>
    {
        MessageProperties Properties { get; }
        T Body { get; }
        string ConsumerTag { get; set; }
        ulong DeliverTag { get; set; }
        bool Redelivered { get; set; }
        string Exchange { get; set; }
        string RoutingKey { get; set; }
    }
}