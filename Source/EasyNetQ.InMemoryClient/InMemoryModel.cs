using System;
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;

#pragma warning disable 67

namespace EasyNetQ.InMemoryClient
{
    public class InMemoryModel : IModel
    {
        private InMemoryConnection connection;

        public InMemoryModel(InMemoryConnection connection)
        {
            this.connection = connection;
        }

        public void Dispose()
        {
            // nothing to do.
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new BasicProperties();
        }

        public IFileProperties CreateFileProperties()
        {
            throw new NotImplementedException();
        }

        public IStreamProperties CreateStreamProperties()
        {
            throw new NotImplementedException();
        }

        public void ChannelFlow(bool active)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            ExchangeDeclare(exchange, type, durable);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            if (connection.Exchanges.ContainsKey(exchange)) return;
            
            connection.Exchanges.Add(exchange, new ExchangeInfo(exchange, type, durable));
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(string exchange)
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            throw new NotImplementedException();
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare()
        {
            var serverGeneratedQueueName = Guid.NewGuid().ToString();
            return QueueDeclare(serverGeneratedQueueName, false, true, true, null);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            if (!connection.Queues.ContainsKey(queue))
            {
                var queueInfo = new QueueInfo(queue, durable, exclusive, autoDelete, arguments);
                connection.Queues.Add(queue, queueInfo);

                // do the default bind to the default exchange ...
                connection.Exchanges[""].BindTo(queueInfo, queue);
            }
            return new QueueDeclareOk(queue, 0, 0);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            if (!connection.Exchanges.ContainsKey(exchange))
            {
                throw new InMemoryClientException(string.Format("Exchange '{0}' does not exist", exchange));
            }
            if (!connection.Queues.ContainsKey(queue))
            {
                throw new InMemoryClientException(string.Format("Queue '{0}' does not exist", queue));
            }

            var exchangeInfo = connection.Exchanges[exchange];
            var queueInfo = connection.Queues[queue];

            exchangeInfo.BindTo(queueInfo, routingKey);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public uint QueuePurge(string queue)
        {
            throw new NotImplementedException();
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            throw new NotImplementedException();
        }

        public uint QueueDelete(string queue)
        {
            throw new NotImplementedException();
        }

        public void ConfirmSelect()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie()
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            if (!connection.Queues.ContainsKey(queue))
            {
                throw new InMemoryClientException(string.Format("Queue '{0}' does not exist", queue));
            }

            consumer.HandleBasicConsumeOk(consumerTag);
            connection.Queues[queue].AddConsumer(noAck, consumerTag, consumer);

            return "";
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public void BasicCancel(string consumerTag)
        {
            throw new NotImplementedException();
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            // do nothing
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            if (!connection.Exchanges.ContainsKey(exchange))
            {
                throw new InMemoryClientException(string.Format("Exchange '{0}' does not exist", exchange));
            }
            connection.Exchanges[exchange].Publish(routingKey, basicProperties, body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            // do nothing
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecover(bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecoverAsync(bool requeue)
        {
            throw new NotImplementedException();
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            throw new NotImplementedException();
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            // do nothing
        }

        public void Close(ushort replyCode, string replyText)
        {
            // do nothing
        }

        public void Abort()
        {
            // do nothing
        }

        public void Abort(ushort replyCode, string replyText)
        {
            // do nothing
        }

        public IBasicConsumer DefaultConsumer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
        }

        public ulong NextPublishSeqNo
        {
            get { throw new NotImplementedException(); }
        }

        public event ModelShutdownEventHandler ModelShutdown;
        public event BasicReturnEventHandler BasicReturn;
        public event BasicAckEventHandler BasicAcks;
        public event BasicNackEventHandler BasicNacks;
        public event CallbackExceptionEventHandler CallbackException;
        public event FlowControlEventHandler FlowControl;
        public event BasicRecoverOkEventHandler BasicRecoverOk;
    }
}

#pragma warning restore 67