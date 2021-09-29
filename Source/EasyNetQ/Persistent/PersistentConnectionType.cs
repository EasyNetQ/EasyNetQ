namespace EasyNetQ.Persistent
{
    /// <summary>
    ///     A purpose of connection
    /// </summary>
    public enum PersistentConnectionType
    {
        /// <summary>
        ///     For publishing messages
        /// </summary>
        Producer,
        /// <summary>
        ///     For consuming messages
        /// </summary>
        Consumer
    }
}
