using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.Integration
{
    public class AmqpStringSizeExperiments
    {
        // 320 char input
        private const string longString = 
            "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + 
            "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + 
            "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + 
            "01234567890123456789012345678901234567890123456789012345678901234567890123456789";

        public void ExchangeName()
        {
            // failed: Short string too long; UTF-8 encoded length=320, max=255
	        // RabbitMQ.Client.Exceptions.WireFormattingException: Short string too long; 
            // UTF-8 encoded length=320, max=255

            WithChannel(x => x.ExchangeDeclare(longString, "direct"));
        }

        public void BasicProperties()
        {
            /*
             * failed: Unable to read data from the transport connection: 
             * An existing connection was forcibly closed by the remote host. 
             * System.IO.IOException: Unable to read data from the transport 
             * connection: An existing connection was forcibly closed by the remote host.
             */

            // setting any basic properties to more than 255 chars will cause this
            // unhelpful exception.
            WithChannel(x =>
                {
                    var properties = x.CreateBasicProperties();

//                    properties.Type = longString;
//                    properties.CorrelationId = longString;
//                    properties.AppId = longString;
//                    properties.ClusterId = longString;
//                    properties.ContentEncoding = longString;
//                    properties.ContentType = longString;

                    x.BasicPublish("", "", properties, new byte[32]);
                });
        }

        public void Headers()
        {
            // Headers hashtable will cause publish to throw when its size
            // exceeds 128K
            WithChannel(x =>
                {
                    var properties = x.CreateBasicProperties();

                    properties.Headers = new Dictionary<string, object>();
                    properties.Headers.Add("key", new string('*', 1024 * 127));

                    x.BasicPublish("", "", properties, new byte[32]);
                });
        }

        public void WithChannel(Action<IModel> channelAction)
        {
            var connectionFactory = new ConnectionFactory
                {
                    Uri = "amqp://localhost"
                };

            using (var connection = connectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channelAction(channel);
                }
            }
        }
    }
}