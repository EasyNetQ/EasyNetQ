namespace EasyNetQ.Management.Client.Model
{
    public class BindingInfo
    {
        public string routing_key { get; private set; }
        public InputArguments arguments { get; private set; }

        public BindingInfo(string routingKey, InputArguments arguments)
        {
            routing_key = routingKey;
            this.arguments = arguments;
        }

        public BindingInfo(string routingKey) : this(routingKey, new InputArguments())
        {
        }
    }
}