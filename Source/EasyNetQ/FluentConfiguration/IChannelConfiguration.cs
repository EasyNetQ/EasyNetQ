namespace EasyNetQ.FluentConfiguration
{
    public interface IChannelConfiguration
    {
        /// <summary>
        /// Set publisher confirms on.
        /// You must set a success and failure callback using the publish configuration if publish confirms are on.
        /// 
        /// channel.Publish(myMessage, x => x.OnSuccess(() => { ... }).OnFailure(() => { ... }));
        /// 
        /// </summary>
        /// <returns></returns>
        IChannelConfiguration WithPublisherConfirms();
    }

    public class ChannelConfiguration : IChannelConfiguration
    {
        public bool PublisherConfirmsOn { get; private set; }

        public ChannelConfiguration()
        {
            PublisherConfirmsOn = false;
        }

        public IChannelConfiguration WithPublisherConfirms()
        {
            PublisherConfirmsOn = true;
            return this;
        }
    }
}