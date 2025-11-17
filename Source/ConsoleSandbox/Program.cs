using EasyNetQ;
public class Program
{
    public static async Task Main(string[] args)
    {
        string connectinString = "host=localhost;publisherConfirms=true;timeout=10";
        var bus = RabbitHutch.CreateBus(connectinString);
        await SimplePubSubAsync(bus);
        await SimpleSendReceiveAsync(bus);
        await SimpleRequestResponseAsync(bus);
        await SimpleRequestResponseAsync2(bus);
        await SimpleRequestResponseAsync3(bus);
        Console.ReadLine();
    }
    private static async Task SimplePubSubAsync(IBus bus)
    {
        var res = bus.PubSub;
        await res.SubscribeAsync<TestMessage>("Test", async msg =>
        {
            Console.WriteLine($"Received message: {msg.Text}");
            await Task.CompletedTask;
        });
        for (int i = 0; i < 100; i++)
        {
            await res.PublishAsync(new TestMessage { Text = $"Hello, {i}!" });
        }
    }
    private static async Task SimpleSendReceiveAsync(IBus bus)
    {
        var res = bus.SendReceive;
        await res.ReceiveAsync<TestMessage>("TestQueue", async msg =>
        {
            Console.WriteLine($"Received Send message: {msg.Text}");
            await Task.CompletedTask;
        });
        for (int i = 0; i < 100; i++)
        {
            await res.SendAsync("TestQueue", new TestMessage { Text = $"Hello, {i}!" });
        }
    }
    private static async Task SimpleRequestResponseAsync(IBus bus)
    {
        var res = bus.Rpc;
        await res.RespondAsync<TestMessage,string>(async (msg,ct) =>
        {
            var str = $"RespondAsync message: {msg.Text}";
            Console.WriteLine(str);
            return str;
        }, rc => {  rc.WithQueueName("JustForTest"); });
        for (int i = 0; i < 100; i++)
        {
            await res.RequestAsync<TestMessage,string>(new TestMessage { Text = $"Hello, {i}!" }, rc=>
            {
                rc.WithQueueName("JustForTest");
            });
        }
    }
    private static async Task SimpleRequestResponseAsync2(IBus bus)
    {
        var res = bus.Rpc;
        await res.RespondAsync<TestMessage, string>(async (msg, ct) =>
        {
            var str = $"RespondAsync message2: {msg.Text}";
            Console.WriteLine(str);
            return str;
        }, rc => {  });
        for (int i = 0; i < 100; i++)
        {
            await res.RequestAsync<TestMessage, string>(new TestMessage { Text = $"Hello, {i}!" }, rc =>
            {

            });
        }
    }
    private static async Task SimpleRequestResponseAsync3(IBus bus)
    {
        var res = bus.Rpc;
        await res.RespondAsync<TestMessageNoAttrib, string>(async (msg, ct) =>
        {
            var str = $"RespondAsync message3: {msg.Text}";
            Console.WriteLine(str);
            return str;
        }, rc => { });
        for (int i = 0; i < 100; i++)
        {
            await res.RequestAsync<TestMessageNoAttrib, string>(new TestMessageNoAttrib { Text = $"Hello, {i}!" }, rc =>
            {

            });
        }
    }
}


[EasyNetQ.Queue("MessageQueueForTest", QueueType = QueueType.Quorum)]
public class TestMessage
{
    public string Text { get; set; }
}

public class TestMessageNoAttrib
{
    public string Text { get; set; }
}
