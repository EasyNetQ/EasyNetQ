namespace EasyNetQ;

/// <summary>
///     What the transport should do with a delivered message once the consume pipeline has run
/// </summary>
public enum AckDecision : byte
{
    /// <summary>
    ///     Positive acknowledgement: the message is done
    /// </summary>
    Ack = 0,

    /// <summary>
    ///     Negative acknowledgement, the broker should redeliver the message
    /// </summary>
    NackRequeue,

    /// <summary>
    ///     Negative acknowledgement without redelivery (dead-lettered or dropped by the broker)
    /// </summary>
    NackDiscard,

    /// <summary>
    ///     The pipeline already dealt with the delivery itself; the transport must not ack or nack
    /// </summary>
    Handled
}
