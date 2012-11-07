using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;

namespace EasyNetQ.Management.Client
{
    public interface IManagementClient
    {
        /// <summary>
        /// Various random bits of information that describe the whole system.
        /// </summary>
        /// <returns></returns>
        Overview GetOverview();

        /// <summary>
        /// A list of nodes in the RabbitMQ cluster.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Node> GetNodes();

        /// <summary>
        /// The server definitions - exchanges, queues, bindings, users, virtual hosts, permissions. 
        /// Everything apart from messages.
        /// </summary>
        /// <returns></returns>
        Definitions GetDefinitions();

        /// <summary>
        /// A list of all open connections.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Connection> GetConnections();

        /// <summary>
        /// A list of all open channels.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Channel> GetChannels();

        /// <summary>
        /// A list of all exchanges.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Exchange> GetExchanges();

        /// <summary>
        /// A list of all queues.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Queue> GetQueues();

        /// <summary>
        /// A list of all bindings.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Binding> GetBindings();

        /// <summary>
        /// A list of all vhosts.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Vhost> GetVHosts();

        /// <summary>
        /// A list of all users.
        /// </summary>
        /// <returns></returns>
        IEnumerable<User> GetUsers();

        /// <summary>
        /// A list of all permissions for all users.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Permission> GetPermissions();
    }
}