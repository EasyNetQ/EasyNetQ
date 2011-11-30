namespace EasyNetQ.Tests.Sagas
{
    public class SubscribePublishSaga : ISaga
    {
        public void Initialize(IBus bus)
        {
            bus.Subscribe<TestRequestMessage>("subscriptionId", requestMessage =>
            {
                var responseMessage = new TestResponseMessage
                {
                    Text = requestMessage.Text
                };
                bus.Publish(responseMessage);
            });
        }
    }
}