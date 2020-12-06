using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace EasyNetQ.IntegrationTests
{
    public class DockerProxy : IDisposable
    {
        private readonly DockerClient client;
        private readonly DockerClientConfiguration dockerConfiguration;

        public DockerProxy(Uri uri)
        {
            dockerConfiguration = new DockerClientConfiguration(uri);
            client = dockerConfiguration.CreateClient();
        }

        public void Dispose()
        {
            dockerConfiguration.Dispose();
            client.Dispose();
        }

        public async Task<OSPlatform> GetDockerEngineOsAsync(CancellationToken token = default)
        {
            var response = await client.System.GetSystemInfoAsync(token);
            return OSPlatform.Create(response.OSType.ToUpper());
        }

        public async Task CreateNetworkAsync(string name, CancellationToken token = default)
        {
            var networksCreateParameters = new NetworksCreateParameters
            {
                Name = name
            };
            await client.Networks.CreateNetworkAsync(networksCreateParameters, token);
        }

        public async Task PullImageAsync(string image, string tag, CancellationToken token = default)
        {
            var createParameters = new ImagesCreateParameters
            {
                FromImage = image,
                Tag = tag
            };
            var progress = new Progress<JSONMessage>(jsonMessage => { });
            await client.Images.CreateImageAsync(createParameters, null, progress, token);
        }

        public async Task<string> CreateContainerAsync(string image, string name,
            IDictionary<string, ISet<string>> portMappings, string networkName = null, IList<string> envVars = null,
            CancellationToken token = default)
        {
            var createParameters = new CreateContainerParameters
            {
                Image = image,
                Env = envVars ?? Enumerable.Empty<string>().ToList(),
                Name = name,
                Hostname = name,
                HostConfig = new HostConfig
                {
                    PortBindings = PortBindings(portMappings),
                    NetworkMode = networkName
                },
                ExposedPorts = portMappings.ToDictionary(x => x.Key, x => new EmptyStruct())
            };
            var response = await client.Containers.CreateContainerAsync(createParameters, token);
            return response.ID;
        }

        public async Task StartContainerAsync(string id, CancellationToken token = default)
        {
            await client.Containers.StartContainerAsync(id, new ContainerStartParameters(), token)
                ;
        }

        public async Task<string> GetContainerIpAsync(string id, CancellationToken token = default)
        {
            var response = await client.Containers.InspectContainerAsync(id, token);
            var networks = response.NetworkSettings.Networks;
            return networks.Select(x => x.Value.IPAddress).First(x => !string.IsNullOrEmpty(x));
        }

        public async Task StopContainerAsync(string name, CancellationToken token = default)
        {
            var ids = await FindContainerIdsAsync(name);
            var stopTasks = ids.Select(x =>
                client.Containers.StopContainerAsync(x, new ContainerStopParameters(), token));
            await Task.WhenAll(stopTasks);
        }

        public Task StopContainerByIdAsync(string id, CancellationToken token = default)
        {
            return client.Containers.StopContainerAsync(id, new ContainerStopParameters(), token);
        }

        public async Task RemoveContainerAsync(string name, CancellationToken token = default)
        {
            var ids = await FindContainerIdsAsync(name);
            var containerRemoveParameters = new ContainerRemoveParameters {Force = true, RemoveVolumes = true};
            var removeTasks =
                ids.Select(x => client.Containers.RemoveContainerAsync(x, containerRemoveParameters, token));
            await Task.WhenAll(removeTasks);
        }

        public async Task DeleteNetworkAsync(string name, CancellationToken token = default)
        {
            var ids = await FindNetworkIdsAsync(name);
            var deleteTasks = ids.Select(x => client.Networks.DeleteNetworkAsync(x, token));
            await Task.WhenAll(deleteTasks);
        }

        private static IDictionary<string, IList<PortBinding>> PortBindings(
            IDictionary<string, ISet<string>> portMappings)
        {
            return portMappings
                .Select(x => new {ContainerPort = x.Key, HostPorts = HostPorts(x.Value)})
                .ToDictionary(x => x.ContainerPort, x => (IList<PortBinding>) x.HostPorts);
        }

        private static List<PortBinding> HostPorts(IEnumerable<string> hostPorts)
        {
            return hostPorts.Select(x => new PortBinding {HostPort = x}).ToList();
        }

        public async Task<IEnumerable<string>> FindContainerIdsAsync(string name)
        {
            var containers = await client.Containers
                .ListContainersAsync(new ContainersListParameters {All = true, Filters = ListFilters(name)})
                ;
            return containers.Select(x => x.ID);
        }

        private async Task<IEnumerable<string>> FindNetworkIdsAsync(string name)
        {
            var networks = await client.Networks
                .ListNetworksAsync(new NetworksListParameters {Filters = ListFilters(name)});
            return networks.Select(x => x.ID);
        }

        private static Dictionary<string, IDictionary<string, bool>> ListFilters(string name)
        {
            return new Dictionary<string, IDictionary<string, bool>>
            {
                {"name", new Dictionary<string, bool> {{name, true}}}
            };
        }
    }
}
