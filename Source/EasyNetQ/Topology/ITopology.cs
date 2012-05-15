namespace EasyNetQ.Topology
{
    public interface ITopology
    {
        void Visit(ITopologyVisitor visitor);
    }
}