using EasyNetQ.MessageVersioning;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    public class MyMessageV2 : MyMessage, ISupersede<MyMessage>
    {
        public int Number { get; set; }
    }
}