namespace EasyNetQ.Topology
{
    public class TopicExchange : Exchange
    {
        public TopicExchange(string exchangeName) : base(exchangeName)
        {
        }

        public override void Visit(ITopologyVisitor visitor)
        {
            visitor.CreateExchange(Name, ExchangeType.Topic);
            foreach (var binding in bindings)
            {
                binding.Visit(visitor);
            }
        }
    }
}