using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Handles publisher confirms.
    /// http://www.rabbitmq.com/blog/2011/02/10/introducing-publisher-confirms/
    /// http://www.rabbitmq.com/confirms.html
    /// </summary>
    public class PublisherConfirms : IPublisherConfirms
    {
        private readonly IConnectionConfiguration configuration;
        private readonly IEasyNetQLogger logger;
        private readonly IDictionary<ulong, ConfirmActions> dictionary = new Dictionary<ulong, ConfirmActions>();

        private IModel cachedModel;

        public PublisherConfirms(IConnectionConfiguration configuration, IEasyNetQLogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        private void SetModel(IModel model)
        {
            // we only need to set up the channel once, but the persistent channel can change
            // the IModel instance underneath us, so check on each publish.
            if (cachedModel == model) return;

            // the old model has been closed and we're now using a new model, so remove
            // any existing callback entries in the dictionary
            dictionary.Clear();

            if (cachedModel != null)
            {
                cachedModel.BasicAcks -= ModelOnBasicAcks;
                cachedModel.BasicNacks -= ModelOnBasicNacks;
            }

            cachedModel = model;

            // switch channel to confirms mode.
            model.ConfirmSelect();

            model.BasicAcks += ModelOnBasicAcks;
            model.BasicNacks += ModelOnBasicNacks;
        }

        private void ModelOnBasicNacks(IModel model, BasicNackEventArgs args)
        {
            HandleConfirm(args.DeliveryTag, x => x.OnNack());
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            HandleConfirm(args.DeliveryTag, x => x.OnAck());
        }

        private void HandleConfirm(ulong sequenceNumber, Action<ConfirmActions> confirmAction)
        {
            if (!dictionary.ContainsKey(sequenceNumber))
            {
                // timed out and removed so just return
                return;
            }

            confirmAction(dictionary[sequenceNumber]);
            dictionary.Remove(sequenceNumber);
        }

        public Task PublishWithConfirm(IModel model, Action<IModel> publishAction)
        {
            if (!configuration.PublisherConfirms)
            {
                return ExecutePublishActionDirectly(model, publishAction);
            }

            SetModel(model);

            var tcs = new TaskCompletionSource<NullStruct>();
            var sequenceNumber = model.NextPublishSeqNo;

            // make sure we don't get a race condition when the ack/nack and timeout
            // occur at the same time.
            publishAction(model);

            var responseLock = new object();

            // there are three possible outcomes from a publish with confirms:

            // 1. We time out without an ack or a nack being received. Throw a timeout exception.
            var timer = new Timer(state =>
                {
                    lock (responseLock)
                    {
                        if (tcs.Task.IsCompleted) return;
                        logger.ErrorWrite("Publish timed out. Sequence number: {0}", sequenceNumber);
                        dictionary.Remove(sequenceNumber);
                        tcs.SetException(new TimeoutException());
                    }
                }, null, configuration.Timeout * 1000, Timeout.Infinite);

            dictionary.Add(sequenceNumber, new ConfirmActions
                {
                    // 2. An ack is received, so complete normally.
                    OnAck = () =>
                        {
                            lock (responseLock)
                            {
                                if (tcs.Task.IsCompleted) return;
                                timer.Dispose();
                                tcs.SetResult(new NullStruct());
                            }
                        },

                    // 3. A Nack is received, so throw an exception.
                    OnNack = () =>
                        {
                            lock (responseLock)
                            {
                                if (tcs.Task.IsCompleted) return;
                                timer.Dispose();
                                logger.ErrorWrite("Publish was nacked by broker. Sequence number: {0}", sequenceNumber);
                                tcs.SetException(new PublishNackedException(string.Format(
                                    "Broker has signalled that publish {0} was unsuccessful", sequenceNumber)));
                            }
                        }
                }); 
            
            return tcs.Task;
        }

        private Task ExecutePublishActionDirectly(IModel model, Action<IModel> publishAction)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            publishAction(model);
            tcs.SetResult(new NullStruct());
            return tcs.Task;
        }

        private struct NullStruct { }

        private class ConfirmActions
        {
            public Action OnAck { get; set; }
            public Action OnNack { get; set; }
        }
    }
}