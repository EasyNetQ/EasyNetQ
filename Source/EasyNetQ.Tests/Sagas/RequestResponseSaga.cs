namespace EasyNetQ.Tests.Sagas
{
    public class RequestResponseSaga : ISaga
    {
        public void Initialize(IBus bus)
        {
            bus.Subscribe<StartMessage>("id", startMessage =>
            {
                var request = new TestRequestMessage {Text = startMessage.Text};
                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    {
                        var endMessage = new EndMessage {Text = response.Text};

                        using (var publishChannel2 = bus.OpenPublishChannel())
                        {
                            publishChannel2.Publish(endMessage);
                        }
                    });
                }
            });
        }
    }
}