namespace EasyNetQ.Topology
{
    public interface IExchange : IBindable
    {
        string Name { get; }
    }
}