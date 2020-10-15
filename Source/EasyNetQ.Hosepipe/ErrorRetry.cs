using System.Collections.Generic;
using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;

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
            using var connection = HosepipeConnection.FromParameters(parameters);
            using var model = connection.CreateModel();

            model.ConfirmSelect();

            foreach (var rawErrorMessage in rawErrorMessages)
            {
                var error = (Error) serializer.BytesToMessage(typeof(Error), errorMessageSerializer.Deserialize(rawErrorMessage.Body));
                var properties = model.CreateBasicProperties();
                error.BasicProperties.CopyTo(properties);
                var body = errorMessageSerializer.Deserialize(error.Message);
                model.BasicPublish("", error.Queue, true, properties, body);
                model.WaitForConfirmsOrDie();
            }
        }
    }
}
