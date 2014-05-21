namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        public string Name { get; private set; }

        public static IExchange GetDefault()
        {
            return new Exchange("");
        }

        public Exchange(string name)
        {
            Preconditions.CheckNotNull(name, "name");
            Name = name;
        }
    }
}