namespace EasyNetQ.Consumer
{
    public interface IHandlerCollectionFactory
    {
        IHandlerCollection CreateHandlerCollection();
    }
}