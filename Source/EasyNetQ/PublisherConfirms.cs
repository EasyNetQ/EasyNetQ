using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    public class PublisherConfirms : IPublisherConfirms
    {
        private readonly IDictionary<ulong, CallbackSet> callbacks = new Dictionary<ulong, CallbackSet>();

        public void RegisterCallbacks(IModel channel, Action successCallback, Action failureCallback)
        {
            if(channel == null)
            {
                throw new ArgumentNullException("channel");
            }
            if(successCallback == null)
            {
                throw new ArgumentNullException("successCallback");
            }
            if(failureCallback == null)
            {
                throw new ArgumentNullException("failureCallback");
            }

            var sequenceNumber = channel.NextPublishSeqNo;
            if (callbacks.ContainsKey(sequenceNumber))
            {
                throw new EasyNetQException("Duplicate NextPublishSeqNo. publish channels are not thread safe. Are you sharing a channel between threads?");
            }

            callbacks.Add(sequenceNumber, new CallbackSet(successCallback, failureCallback));
        }

        public void SuccessfulPublish(IModel channel, BasicAckEventArgs args)
        {
            if(args == null)
            {
                throw new ArgumentNullException("args");
            }

            ProcessArgsAndRun(args.Multiple, args.DeliveryTag, x => x.Success());
        }

        public void FailedPublish(IModel channel, BasicNackEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            ProcessArgsAndRun(args.Multiple, args.DeliveryTag, x => x.Failure());
        }

        private void ProcessArgsAndRun(bool multiple, ulong deliveryTag, Action<CallbackSet> callbackSetAction)
        {
            if (multiple)
            {
                var sequenceNumbers = callbacks
                    .Where(keyValuePair => keyValuePair.Key <= deliveryTag)
                    .Select(x => x.Key)
                    .ToArray();

                foreach (var sequenceNumber in sequenceNumbers)
                {
                    RunForSequenceNumber(sequenceNumber, callbackSetAction);
                }
            }
            else
            {
                RunForSequenceNumber(deliveryTag, callbackSetAction);
            }
        }

        private void RunForSequenceNumber(ulong sequenceNumber, Action<CallbackSet> callbackSetAction)
        {
            if (!callbacks.ContainsKey(sequenceNumber))
            {
                throw new EasyNetQException("Delivery callback has unrecorded sequence number");
            }

            var callbackSet = callbacks[sequenceNumber];
            callbackSetAction(callbackSet);
            callbacks.Remove(sequenceNumber);
        }

        private class CallbackSet
        {
            public Action Success { get; private set; }
            public Action Failure { get; private set; }

            public CallbackSet(Action success, Action failure)
            {
                Success = success;
                Failure = failure;
            }
        }
    }
}