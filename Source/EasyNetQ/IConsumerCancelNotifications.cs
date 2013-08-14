namespace EasyNetQ
{
    public interface IConsumerCancelNotifications
    {
        /// <summary>
        /// Event is fired in case of: "Consumer Cancel Notification"
        /// http://www.rabbitmq.com/consumer-cancel.html
        /// </summary>
        event BasicCancelEventHandler BasicCancel;
    }
}