namespace EasyNetQ
{
    public interface IAdvancedPublishConfiguration
    {
        IAdvancedPublishConfiguration WithRoutingKey(string routingKey);
        IAdvancedPublishConfiguration AsMandatory(bool mandatory = true);
        IAdvancedPublishConfiguration WithPublisherConfirms(bool publisherConfirms = true);
    }

    internal class AdvancedPublishConfiguration : IAdvancedPublishConfiguration
    {
        public AdvancedPublishConfiguration(string routingKey, bool mandatory, bool publisherConfirms)
        {
            RoutingKey = routingKey;
            Mandatory = mandatory;
            PublisherConfirms = publisherConfirms;
        }

        public string RoutingKey { get; private set; }
        public bool Mandatory { get; private set; }
        public bool PublisherConfirms { get; private set; }

        public IAdvancedPublishConfiguration WithRoutingKey(string routingKey)
        {
            Preconditions.CheckNotBlank(routingKey, "routingKey");

            RoutingKey = routingKey;
            return this;
        }

        public IAdvancedPublishConfiguration AsMandatory(bool mandatory)
        {
            Mandatory = mandatory;
            return this;
        }

        public IAdvancedPublishConfiguration WithPublisherConfirms(bool publisherConfirms)
        {
            PublisherConfirms = publisherConfirms;
            return this;
        }
    }
}
