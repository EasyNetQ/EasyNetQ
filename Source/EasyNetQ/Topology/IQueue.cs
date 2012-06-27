namespace EasyNetQ.Topology
{
    public interface IQueue : IBindable
    {
        string Name { get; }
        bool IsSingleUse { get; }
        IQueue SetAsSingleUse();
    }
}