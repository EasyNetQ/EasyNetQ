namespace EasyNetQ.Topology
{
    public class Exchange : IExchange
    {
        private static readonly Exchange defaultExchange = new Exchange("");

        public string Name { get; private set; }

        public static IExchange GetDefault()
        {
            return defaultExchange;
        }

        public Exchange(string name)
        {
            Preconditions.CheckNotNull(name, "name");
            Name = name;
        }
    }
}