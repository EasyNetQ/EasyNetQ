namespace EasyNetQ.Topology
{
    public class Queue : IQueue
    {
        public Queue(string name)
        {
            Preconditions.CheckNotBlank(name, "name");
            Name = name;
        }

        public string Name { get; private set; }

        public bool IsSingleUse { get; private set; }

        public IQueue SetAsSingleUse()
        {
            IsSingleUse = true;
            return this;
        }
    }
}