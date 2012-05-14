namespace EasyNetQ.Topology
{
    public interface IExchange : ITopology
    {
        string Name { get; }
    }
}