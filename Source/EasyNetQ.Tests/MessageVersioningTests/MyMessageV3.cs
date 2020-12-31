using EasyNetQ.MessageVersioning;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    public class MyMessageV3 : MyMessageV2, ISupersede<MyMessageV2>
    {
        public int NumberInV3 { get; set; }
    }
}
