using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace EasyNetQ.IntegrationTests;

public class DockerProxy : IDisposable
{
    private readonly DockerClient client;
    private readonly DockerClientConfiguration dockerConfiguration;

    public DockerProxy()
    {
        dockerConfiguration = new DockerClientConfiguration();
        client = dockerConfiguration.CreateClient();
    }

    public virtual void Dispose()
    {
        client.Dispose();
        dockerConfiguration.Dispose();
    }

    public async Task<OSPlatform> GetDockerEngineOsAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.System.GetSystemInfoAsync(cancellationToken);
        return OSPlatform.Create(response.OSType.ToUpper());
    }

    public async Task CreateNetworkAsync(string name, CancellationToken cancellationToken = default)
    {
        var networksCreateParameters = new NetworksCreateParameters
        {
            Name = name
        };
        await client.Networks.CreateNetworkAsync(networksCreateParameters, cancellationToken);
    }

    public async Task PullImageAsync(string image, string tag, CancellationToken cancellationToken = default)
    {
        var createParameters = new ImagesCreateParameters
        {
            FromImage = image,
            Tag = tag
        };
        var progress = new Progress<JSONMessage>(_ => { });
        await client.Images.CreateImageAsync(createParameters, null, progress, cancellationToken);
    }

    public async Task<string> CreateContainerAsync(string image, string name,
        IDictionary<string, ISet<string>> portMappings, string networkName = null, IList<string> envVars = null,
        CancellationToken cancellationToken = default)
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
            ExposedPorts = portMappings.ToDictionary(x => x.Key, _ => new EmptyStruct())
        };
        var response = await client.Containers.CreateContainerAsync(createParameters, cancellationToken);
        return response.ID;
    }

    public async Task StartContainerAsync(string id, CancellationToken cancellationToken = default)
    {
        await client.Containers.StartContainerAsync(id, new ContainerStartParameters(), cancellationToken)
            ;
    }

    public async Task<string> GetContainerIpAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await client.Containers.InspectContainerAsync(id, cancellationToken);
        var networks = response.NetworkSettings.Networks;
        return networks.Select(x => x.Value.IPAddress).First(x => !string.IsNullOrEmpty(x));
    }

    public async Task StopContainerAsync(string name, CancellationToken cancellationToken = default)
    {
        var ids = await FindContainerIdsAsync(name);
        var stopTasks = ids.Select(x =>
            client.Containers.StopContainerAsync(x, new ContainerStopParameters(), cancellationToken)
        );
        await Task.WhenAll(stopTasks);
    }

    public Task StopContainerByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return client.Containers.StopContainerAsync(id, new ContainerStopParameters(), cancellationToken);
    }

    public async Task RemoveContainerAsync(string name, CancellationToken cancellationToken = default)
    {
        var ids = await FindContainerIdsAsync(name);
        var containerRemoveParameters = new ContainerRemoveParameters { Force = true, RemoveVolumes = true };
        var removeTasks =
            ids.Select(x => client.Containers.RemoveContainerAsync(x, containerRemoveParameters, cancellationToken));
        await Task.WhenAll(removeTasks);
    }

    public async Task DeleteNetworkAsync(string name, CancellationToken cancellationToken = default)
    {
        var ids = await FindNetworkIdsAsync(name);
        var deleteTasks = ids.Select(x => client.Networks.DeleteNetworkAsync(x, cancellationToken));
        await Task.WhenAll(deleteTasks);
    }

    private static IDictionary<string, IList<PortBinding>> PortBindings(
        IDictionary<string, ISet<string>> portMappings)
    {
        return portMappings
            .Select(x => new { ContainerPort = x.Key, HostPorts = HostPorts(x.Value) })
            .ToDictionary(x => x.ContainerPort, x => (IList<PortBinding>)x.HostPorts);
    }

    private static List<PortBinding> HostPorts(IEnumerable<string> hostPorts)
    {
        return hostPorts.Select(x => new PortBinding { HostPort = x }).ToList();
    }

    public async Task<IEnumerable<string>> FindContainerIdsAsync(string name)
    {
        var containers = await client.Containers
            .ListContainersAsync(new ContainersListParameters { All = true, Filters = ListFilters(name) });
        return containers.Select(x => x.ID);
    }

    private async Task<IEnumerable<string>> FindNetworkIdsAsync(string name)
    {
        var networks = await client.Networks
            .ListNetworksAsync(new NetworksListParameters { Filters = ListFilters(name) });
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
