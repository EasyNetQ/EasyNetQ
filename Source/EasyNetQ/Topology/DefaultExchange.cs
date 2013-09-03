namespace EasyNetQ.Topology
{
    public class DefaultExchange : Exchange
    {
        public DefaultExchange() : base("", Topology.ExchangeType.Direct)
        {
        }

        public override void BindTo(IExchange exchange, params string[] routingKeys)
        {
            throw new EasyNetQException("Cannot bind to default exchange");
        }
    }
}