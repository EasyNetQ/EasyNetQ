using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Handles publisher confirms.
    /// http://www.rabbitmq.com/blog/2011/02/10/introducing-publisher-confirms/
    /// http://www.rabbitmq.com/confirms.html
    /// 
    /// Note, this class is designed to be called sequentially from a single thread. It is NOT
    /// thread safe.
    /// </summary>
    public class PublisherConfirms : PublisherBase
    {
        private readonly IEasyNetQLogger logger;
        private readonly IDictionary<ulong, ConfirmActions> dictionary = new ConcurrentDictionary<ulong, ConfirmActions>();

        private readonly int timeoutSeconds;

        public PublisherConfirms(ConnectionConfiguration configuration, IEasyNetQLogger logger, IEventBus eventBus) : base (eventBus)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            timeoutSeconds = configuration.Timeout;
            this.logger = logger;

            eventBus.Subscribe<PublishChannelCreatedEvent>(OnPublishChannelCreated);
        }

        private void OnPublishChannelCreated(PublishChannelCreatedEvent publishChannelCreatedEvent)
        {
            Preconditions.CheckNotNull(publishChannelCreatedEvent.Channel, "oldModel");

            var outstandingConfirms = new List<ConfirmActions>(dictionary.Values);

            foreach (var outstandingConfirm in outstandingConfirms)
            {
                outstandingConfirm.Cancel();
                ExecutePublishWithConfirmation(
                    publishChannelCreatedEvent.Channel,
                    outstandingConfirm.PublishAction,
                    outstandingConfirm.TaskCompletionSource);
            }
            
        }

        protected override void OnChannelOpened(IModel newModel)
        {
            // switch channel to confirms mode.
            newModel.ConfirmSelect();

            newModel.BasicAcks += ModelOnBasicAcks;
            newModel.BasicNacks += ModelOnBasicNacks;
            base.OnChannelOpened(newModel);
        }

        protected override void OnChannelClosed(IModel oldModel)
        {
            // the old model has been closed and we're now using a new model, so remove
            // any existing callback entries in the dictionary
            dictionary.Clear();

            oldModel.BasicAcks -= ModelOnBasicAcks;
            oldModel.BasicNacks -= ModelOnBasicNacks;
            base.OnChannelClosed(oldModel);
        }

        private void ModelOnBasicNacks(IModel model, BasicNackEventArgs args)
        {
            HandleConfirm(args.DeliveryTag, args.Multiple, x => x.OnNack());
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            HandleConfirm(args.DeliveryTag, args.Multiple, x => x.OnAck());
        }

        private void HandleConfirm(ulong sequenceNumber, bool multiple, Action<ConfirmActions> confirmAction)
        {
            if(multiple)
            {
                //Console.Out.WriteLine(">>>>>>>>>>>>> multiple {0}", sequenceNumber);
                foreach (var match in dictionary.Keys.Where(key => key <= sequenceNumber))
                {
                    confirmAction(dictionary[match]);
                    dictionary.Remove(match);
                }
            }
            else
            {
                if(dictionary.ContainsKey(sequenceNumber))
                {
                    confirmAction(dictionary[sequenceNumber]);
                    dictionary.Remove(sequenceNumber);
                }
            }

        }

        public override Task Publish(IModel model, Action<IModel> publishAction)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            return ExecutePublishWithConfirmation(model, publishAction, tcs);
        }

        private Task ExecutePublishWithConfirmation(IModel model, Action<IModel> publishAction, TaskCompletionSource<NullStruct> tcs)
        {
            SetModel(model);

            var sequenceNumber = model.NextPublishSeqNo;

            // there are three possible outcomes from a publish with confirms:

            // 1. We time out without an ack or a nack being received. Throw a timeout exception.
            Timer timer = null;
            timer = new Timer(state =>
                {
                    var set = tcs.TrySetException(new TimeoutException(string.Format(
                        "Publisher confirms timed out after {0} seconds " +
                        "waiting for ACK or NACK from sequence number {1}",
                        timeoutSeconds, sequenceNumber)));

                    if (!set) return;
                    this.logger.ErrorWrite("Publish timed out. Sequence number: {0}", sequenceNumber);
                    this.dictionary.Remove(sequenceNumber);
                    timer.Dispose();
                }, null, timeoutSeconds*1000, Timeout.Infinite);

            dictionary.Add(sequenceNumber, new ConfirmActions
                {
                    // 2. An ack is received, so complete normally.
                    OnAck = () =>
                        {
                            timer.Dispose();
                            Task.Factory.StartNew(() => tcs.TrySetResult(new NullStruct()));
                        },

                    // 3. A Nack is received, so throw an exception.
                    OnNack = () =>
                        {
                            timer.Dispose();
                            logger.ErrorWrite("Publish was nacked by broker. Sequence number: {0}", sequenceNumber);
                            Task.Factory.StartNew(
                                () =>
                                tcs.TrySetException(
                                    new PublishNackedException(
                                        string.Format("Broker has signalled that publish {0} was unsuccessful", sequenceNumber))));
                        },
                    Cancel = () => timer.Dispose(),
                    PublishAction = publishAction,
                    TaskCompletionSource = tcs
                });

            try
            {
                //execute publish
                publishAction(model);
            }
            catch(Exception ex)
            {
                //return fault when publish action can't be executed
                logger.ErrorWrite("Publish Failed. Sequence number: {0}. Exception: {1}", sequenceNumber, ex);
                timer.Dispose();
                dictionary.Remove(sequenceNumber);
                tcs.TrySetException(ex);
            }
            

            return tcs.Task;
        }
        
        private class ConfirmActions
        {
            public Action OnAck { get; set; }
            public Action OnNack { get; set; }
            public Action Cancel { get; set; }
            public Action<IModel> PublishAction { get; set; }
            public TaskCompletionSource<NullStruct> TaskCompletionSource { get; set; } 
        }
    }
}