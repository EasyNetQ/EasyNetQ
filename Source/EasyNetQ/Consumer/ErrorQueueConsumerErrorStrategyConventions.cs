namespace EasyNetQ.Consumer;

/// <summary>
///     Convention for error queue routing key naming
/// </summary>
public delegate string ErrorQueueNameConvention(MessageReceivedInfo receivedInfo);

/// <summary>
///     Convention for error exchange naming
/// </summary>
public delegate string ErrorExchangeNameConvention(MessageReceivedInfo receivedInfo);

/// <summary>
///     Convention for queue type
/// </summary>
public delegate string? ErrorQueueTypeConvention();

public interface IErrorQueueConsumerErrorStrategyConventions
{
    /// <summary>
    ///     Convention for error queue naming
    /// </summary>
    ErrorQueueNameConvention ErrorQueueNamingConvention { get; }

    /// <summary>
    ///     Convention for error exchange naming
    /// </summary>
    ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; }

    /// <summary>
    ///     Convention for error queue type
    /// </summary>
    ErrorQueueTypeConvention ErrorQueueTypeConvention { get; }
}

public class ErrorQueueConsumerErrorStrategyConventions : IErrorQueueConsumerErrorStrategyConventions
{
    /// <inheritdoc />
    public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; } = _ => "EasyNetQ_Default_Error_Queue";
    /// <inheritdoc />
    public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; } = receivedInfo => "ErrorExchange_" + receivedInfo.RoutingKey;
    /// <inheritdoc />
    public ErrorQueueTypeConvention ErrorQueueTypeConvention { get; set; } = () => null;
}
