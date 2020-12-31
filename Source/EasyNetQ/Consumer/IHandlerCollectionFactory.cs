using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public interface IHandlerCollectionFactory
    {
        IHandlerCollection CreateHandlerCollection(Queue queue);
    }
}
