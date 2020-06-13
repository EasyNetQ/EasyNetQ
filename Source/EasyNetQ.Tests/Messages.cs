namespace EasyNetQ.Tests
{
    public class TestRequestMessage
    {
    }

    public class TestResponseMessage
    {
        public TestResponseMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public interface IAnimal
    {
    }

    public class Dog : IAnimal
    {
    }
}
