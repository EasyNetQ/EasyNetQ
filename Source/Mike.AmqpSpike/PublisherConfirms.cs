using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    public class PublisherConfirms
    {
        public void Spike()
        {
            WithChannel.Do(model =>
            {
                var queue = model.QueueDeclare("confirms.test", false, false, false, null);
                model.ConfirmSelect();

                model.BasicAcks += (model1, args) => 
                    Console.Out.WriteLine("Delivered DeliveryTag: '{0}', Multiple: {1}", args.DeliveryTag, args.Multiple);

                model.BasicNacks += (model1, args) =>
                    Console.Out.WriteLine("Failed DeliverTag: '{0}', Multiple: {1}", args.DeliveryTag, args.Multiple);

                Publish(model, queue, "Hello 1");
                Publish(model, queue, "Hello 2");

                Console.Out.WriteLine("Waiting for result");
                Thread.Sleep(2000);

                BasicGetResult result;
                do
                {
                    Thread.Sleep(100);
                    result = model.BasicGet(queue, false);
                } while (result == null);

                Console.Out.WriteLine("result.Body = {0}", Encoding.UTF8.GetString(result.Body));

                model.BasicAck(result.DeliveryTag, false);
            });
        }

        public void Publish(IModel model, string queue, string message)
        {
            var properties = model.CreateBasicProperties();
            properties.SetPersistent(true);

            var nextPublishSeqNo = model.NextPublishSeqNo;
            Console.Out.WriteLine("Published seq no = {0}", nextPublishSeqNo);

            model.BasicPublish("", queue, properties, Encoding.UTF8.GetBytes(message));
        }
    }
}