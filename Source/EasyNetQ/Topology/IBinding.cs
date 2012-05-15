namespace EasyNetQ.Topology
{
    public interface IBinding : ITopology
    {
        IBindable Bindable { get; }
        IExchange Exchange { get; }
        string[] RoutingKeys { get; }
    }
}