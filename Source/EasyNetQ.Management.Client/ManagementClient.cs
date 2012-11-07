using System.Collections.Generic;
using System.IO;
using System.Net;
using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client
{
    public class ManagementClient : IManagementClient
    {
        private readonly string hostUrl;
        private readonly string username;
        private readonly string password;
        private readonly int portNumber;

        public ManagementClient(
            string hostUrl, 
            string username, 
            string password) : this(hostUrl, username, password, 55672)
        {
            this.hostUrl = hostUrl;
            this.username = username;
            this.password = password;
        }

        public ManagementClient(string hostUrl, string username, string password, int portNumber)
        {
            this.hostUrl = hostUrl;
            this.username = username;
            this.password = password;
            this.portNumber = portNumber;
        }

        public Overview GetOverview()
        {
            return GetRequest<Overview>("overview");
        }

        public IEnumerable<Node> GetNodes()
        {
            return GetRequest<IEnumerable<Node>>("nodes");
        }

        public Definitions GetDefinitions()
        {
            return GetRequest<Definitions>("definitions");
        }

        public IEnumerable<Connection> GetConnections()
        {
            return GetRequest<IEnumerable<Connection>>("connections");
        }

        public IEnumerable<Channel> GetChannels()
        {
            return GetRequest<IEnumerable<Channel>>("channels");
        }

        public IEnumerable<Exchange> GetExchanges()
        {
            return GetRequest<IEnumerable<Exchange>>("exchanges");
        }

        public IEnumerable<Queue> GetQueues()
        {
            return GetRequest<IEnumerable<Queue>>("queues");
        }

        public IEnumerable<Binding> GetBindings()
        {
            return GetRequest<IEnumerable<Binding>>("bindings");
        }

        public IEnumerable<Vhost> GetVHosts()
        {
            return GetRequest<IEnumerable<Vhost>>("vhosts");
        }

        public IEnumerable<User> GetUsers()
        {
            return GetRequest<IEnumerable<User>>("users");
        }

        public IEnumerable<Permission> GetPermissions()
        {
            return GetRequest<IEnumerable<Permission>>("permissions");
        }

        private T GetRequest<T>(string path)
        {
            var request = CreateRequestForPath(path);

            var response = (HttpWebResponse) request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new EasyNetQManagementException("Request failed with status code {0}", response.StatusCode);
            }

            var responseBody = GetBodyFromResponse(response);

            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        private static string GetBodyFromResponse(HttpWebResponse response)
        {
            string responseBody;
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    throw new EasyNetQManagementException("Response stream was null");
                }
                using (var reader = new StreamReader(responseStream))
                {
                    responseBody = reader.ReadToEnd();
                }
            }
            return responseBody;
        }

        private HttpWebRequest CreateRequestForPath(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(BuildEndpointAddress(path));
            request.Credentials = new NetworkCredential(username, password);
            return request;
        }

        private string BuildEndpointAddress(string path)
        {
            return string.Format("{0}:{1}/api/{2}", hostUrl, portNumber, path);
        }
    }
}