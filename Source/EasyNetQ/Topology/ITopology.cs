namespace EasyNetQ.Topology
{
    public interface ITopology
    {
        void Visit(ITopologyVisitor visitor);
    }

    public interface ITopologyVisitor
    {
        void CreateExchange(string exchangeName, ExchangeType exchangeType);
    }
}