namespace EasyNetQ.Rpc
{
    public interface IAdvancedRpcFactory
    {
        IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus);
        IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus);
    }
}