namespace EasyNetQ.Topology
{
    public abstract class Exchange : IExchange
    {
        public static IExchange CreateDirect(string exchangeName)
        {
            return new DirectExchange(exchangeName);
        }

        protected Exchange(string name)
        {
            Name = name;
        }

        public abstract void Visit(ITopologyVisitor visitor);

        public string Name { get; private set; }

        public static IExchange CreateTopic(string exchangeName)
        {
            return new TopicExchange(exchangeName);
        }
    }

    public class TopicExchange : Exchange
    {
        public TopicExchange(string exchangeName) : base(exchangeName)
        {
        }

        public override void Visit(ITopologyVisitor visitor)
        {
            visitor.CreateExchange(Name, ExchangeType.Topic);
        }
    }
}