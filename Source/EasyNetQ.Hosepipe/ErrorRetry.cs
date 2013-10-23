using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public class ErrorRetry : IErrorRetry
    {
        private readonly ISerializer serializer;

        public ErrorRetry(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public void RetryErrors(IEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters)
        {
            foreach (var rawErrorMessage in rawErrorMessages)
            {
                var error = serializer.BytesToMessage<Error>(Encoding.UTF8.GetBytes(rawErrorMessage.Body));
                RepublishError(error, parameters);
            }
        }


        public void RepublishError(Error error, QueueParameters parameters)
        {
            using (var connection = HosepipeConnection.FromParamters(parameters))
            using (var model = connection.CreateModel())
            {
                try
                {
                    if (error.Exchange != string.Empty)
                    {
                        model.ExchangeDeclarePassive(error.Exchange);
                    }

                    var properties = model.CreateBasicProperties();
                    error.BasicProperties.CopyTo(properties);

                    var body = Encoding.UTF8.GetBytes(error.Message);

                    model.BasicPublish(error.Exchange, error.RoutingKey, properties, body);
                }
                catch (OperationInterruptedException)
                {
                    Console.WriteLine("The exchange, '{0}', described in the error message does not exist on '{1}', '{2}'",
                        error.Exchange, parameters.HostName, parameters.VHost);
                }
            }            
        }
    }
}