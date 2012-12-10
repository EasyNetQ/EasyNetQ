namespace EasyNetQ.Management.Client.Model
{
    public class BindingInfo
    {
        public string RoutingKey { get; private set; }
        public InputArguments Arguments { get; private set; }

        public BindingInfo(string routingKey, InputArguments arguments)
        {
            RoutingKey = routingKey;
            Arguments = arguments;
        }

        public BindingInfo(string routingKey)
            : this(routingKey, new InputArguments())
        {
        }
    }
}