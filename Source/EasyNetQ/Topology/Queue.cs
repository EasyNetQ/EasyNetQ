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

        public string Name { get; private set; }

        public bool IsSingleUse { get; private set; }

        public IQueue SetAsSingleUse()
        {
            IsSingleUse = true;
            IsExclusive = true;
            return this;
        }

        public bool IsExclusive { get; private set; }
    }
}