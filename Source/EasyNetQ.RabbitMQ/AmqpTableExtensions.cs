namespace EasyNetQ;

internal static class AmqpTableExtensions
{
    // Our header and argument tables type values as object; RabbitMQ.Client's field tables allow null values
    public static IDictionary<string, object?>? ToAmqpTable(this IDictionary<string, object>? table) => table!;

    public static IDictionary<string, object>? FromAmqpTable(this IDictionary<string, object?>? table) => table!;
}
