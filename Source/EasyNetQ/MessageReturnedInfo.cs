namespace EasyNetQ
{
    /// <summary>
    /// Contains an information about a message returned from broker
    /// </summary>
    public readonly struct MessageReturnedInfo
    {
        /// <summary>
        ///     The exchange the returned message was original published to
        /// </summary>
        public string Exchange { get; }

        /// <summary>
        ///     The routing key used when the message was originally published
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Human-readable text from the broker describing the reason for the return
        /// </summary>
        public string ReturnReason { get; }

        /// <summary>
        ///     Creates MessageReturnedInfo
        /// </summary>
        public MessageReturnedInfo(
            string exchange,
            string routingKey,
            string returnReason
        )
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");
            Preconditions.CheckNotNull(returnReason, "returnReason");

            Exchange = exchange;
            RoutingKey = routingKey;
            ReturnReason = returnReason;
        }
    }
}
