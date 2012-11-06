using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    public interface IPublisherConfirms
    {
        void RegisterCallbacks(IModel channel, Action successCallback, Action failureCallback);
        void SuccessfulPublish(IModel channel, BasicAckEventArgs args);
        void FailedPublish(IModel channel, BasicNackEventArgs args);
    }
}