namespace EasyNetQ.Rpc.ReuseQueue
{
    class ReuseQueueAdvancedRpcFactory : IAdvancedRpcFactory
    {
        public IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus)
        {
            throw new System.NotImplementedException();
        }

        public IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus)
        {
            throw new System.NotImplementedException();
        }
    }
}