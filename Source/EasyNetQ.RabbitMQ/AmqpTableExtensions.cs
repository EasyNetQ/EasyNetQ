namespace EasyNetQ;

internal static class AmqpTableExtensions
{
    // Topology arguments are non-null by our API; RabbitMQ.Client's field tables also allow null values
    public static IDictionary<string, object?>? ToAmqpTable(this IDictionary<string, object>? table) => table!;
}
