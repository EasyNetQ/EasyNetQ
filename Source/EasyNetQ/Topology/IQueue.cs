namespace EasyNetQ.Topology
{
    public interface IQueue : IBindable
    {
        string Name { get; }
    }
}