namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        public string Name { get; private set; }

        public Exchange(string name)
        {
            Preconditions.CheckNotNull(name, "name");
            Name = name;
        }
    }
}