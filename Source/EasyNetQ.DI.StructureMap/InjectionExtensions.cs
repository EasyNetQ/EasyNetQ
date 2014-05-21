namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this StructureMap.IContainer container)
        {
            RabbitHutch.SetContainerFactory(() => new StructureMapAdapter(container));
        }
    }
}