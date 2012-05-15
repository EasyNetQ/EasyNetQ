namespace EasyNetQ.Topology
{
    public interface IBindable : ITopology
    {
        /// <summary>
        /// Bind a destination, either a queue or an exchange, to an exchange.
        /// </summary>
        /// <param name="exchange">The source exchange (where the messages are coming from)</param>
        /// <param name="routingKeys">The routing key(s). Multiple routing keys will create multiple bindings</param>
        void BindTo(IExchange exchange, params string[] routingKeys);     
    }
}