namespace EasyNetQ.SystemMessages;

/// <summary>
/// A wrapper for errored messages
/// </summary>
public class Error
{
    public Error(
        string routingKey,
        string exchange,
        string queue,
        string exception,
        string message,
        DateTime dateTime,
        MessageProperties basicProperties
    )
    {
        RoutingKey = routingKey;
        Exchange = exchange;
        Queue = queue;
        Exception = exception;
        Message = message;
        DateTime = dateTime;
        BasicProperties = basicProperties;
    }

    public string RoutingKey { get; set; }
    public string Exchange { get; set; }
    public string Queue { get; set; }
    public string Exception { get; set; }
    public string Message { get; set; }
    public DateTime DateTime { get; set; }
    public MessageProperties BasicProperties { get; set; }
}
