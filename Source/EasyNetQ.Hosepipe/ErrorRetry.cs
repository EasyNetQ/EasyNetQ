using System;
using System.Collections.Generic;
using System.Text;

using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public class ErrorRetry : IErrorRetry
    {
        private readonly ISerializer serializer;

        private readonly IErrorMessageSerializer errorMessageSerializer;

        public ErrorRetry(ISerializer serializer, IErrorMessageSerializer errorMessageSerializer)
        {
            this.serializer = serializer;
            this.errorMessageSerializer = errorMessageSerializer;
        }

        public void RetryErrors(IEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters)
        {
            foreach (var rawErrorMessage in rawErrorMessages)
            {
                var error = serializer.BytesToMessage<Error>(errorMessageSerializer.Deserialize(rawErrorMessage.Body));
                RepublishError(error, parameters);
            }
        }


        public void RepublishError(Error error, QueueParameters parameters)
        {
            using (var connection = HosepipeConnection.FromParameters(parameters))
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

                    var body = errorMessageSerializer.Deserialize(error.Message);

                    model.BasicPublish(error.Exchange, error.RoutingKey, true, properties, body);
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