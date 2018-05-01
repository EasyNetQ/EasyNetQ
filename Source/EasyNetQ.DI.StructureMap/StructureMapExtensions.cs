using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public static class StructureMapExtensions
    {
        public static Container RegisterEasyNetQ(this Container container)
        {
            new StructureMapAdapter(container).RegisterDefaultServices();
            return container;
        }
    }
}