namespace EasyNetQ.Topology
{
    public class Queue : IQueue
    {
        public Queue(string name, bool isExclusive)
        {
            Preconditions.CheckNotBlank(name, "name");
            Name = name;
            IsExclusive = isExclusive;
        }

        public string Name { get; }
        public bool IsExclusive { get; }
    }
}