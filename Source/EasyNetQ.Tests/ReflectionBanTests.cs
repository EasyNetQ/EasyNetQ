using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace EasyNetQ.Tests;

/// <summary>
///     Ratchets down runtime reflection in the EasyNetQ assemblies: scans the compiled metadata of each package
///     assembly for references to banned reflection members and fails when a new one appears. Members listed in
///     <see cref="Allowed" /> (keyed by assembly) are the sanctioned escape hatches, each with its reason; the
///     target is an empty allowlist for EasyNetQ.Core, with the JIT-only escape hatches confined to the bundle.
/// </summary>
public class ReflectionBanTests
{
    private static readonly string[] Banned =
    [
        "Load",              // Assembly.Load*
        "LoadFrom",
        "GetType",           // Type.GetType(string) / Assembly.GetType(string): resolved below to the declaring type
        "MakeGenericType",
        "MakeGenericMethod",
        "Compile",           // Expression<T>.Compile
        "CreateInstance",    // Activator.CreateInstance
        "GetCustomAttribute",
        "GetCustomAttributes",
        "IsAssignableFrom",
    ];

    private static readonly Dictionary<string, string> Allowed = new()
    {
        // EasyNetQ.Core - target is an empty list; every entry here is scheduled to shrink
        ["EasyNetQ.Core:System.Type.MakeGenericType"] = "RuntimeDescriptorFactory fallback (generator makes unreachable)",
        ["EasyNetQ.Core:System.Reflection.Assembly.GetType"] = "DefaultTypeNameSerializer.Deserialize legacy wire-name resolution (unused by the descriptor pipeline)",
        ["EasyNetQ.Core:System.Type.GetType"] = "DefaultTypeNameSerializer.Deserialize legacy wire-name resolution (unused by the descriptor pipeline)",
        ["EasyNetQ.Core:System.Reflection.Assembly.Load"] = "DefaultTypeNameSerializer.Deserialize legacy wire-name resolution (unused by the descriptor pipeline)",
        ["EasyNetQ.Core:System.Reflection.MemberInfo.GetCustomAttributes"] = "AttributeMetadataReader descriptor fallback (once per type; generator supersedes)",
        ["EasyNetQ.Core:System.Type.IsAssignableFrom"] = "HandlerTable/HandlerCollection polymorphic match (memoized)",

        // EasyNetQ.RabbitMQ - setup-time only
        ["EasyNetQ.RabbitMQ:System.Reflection.CustomAttributeExtensions.GetCustomAttribute"] = "ConnectionConfigurationExtensions platform stamping (setup only)",
        ["EasyNetQ.RabbitMQ:System.Type.IsAssignableFrom"] = "PullingConsumer type guard",

        // EasyNetQ (bundle) - JIT-only compat surface, annotated RequiresDynamicCode where dynamic
        ["EasyNetQ:System.Reflection.MemberInfo.GetCustomAttributes"] = "AutoSubscriber subscription attributes (bundle-only)",
        ["EasyNetQ:System.Type.GetType"] = "LegacyTypeNameSerializer wire-name resolution (bundle-only)",
        ["EasyNetQ:System.Reflection.MethodInfo.MakeGenericMethod"] = "NonGenericRpcExtensions pair bridge + AutoSubscriber (bundle-only)",
    };

    public static TheoryData<string> PackageAssemblies => new("EasyNetQ.Core", "EasyNetQ.RabbitMQ", "EasyNetQ");

    private static string AssemblyPath(string simpleName) => simpleName switch
    {
        "EasyNetQ.Core" => typeof(global::EasyNetQ.Pipeline.PropertyBag).Assembly.Location,
        "EasyNetQ.RabbitMQ" => typeof(RabbitBus).Assembly.Location,
        "EasyNetQ" => typeof(global::EasyNetQ.AutoSubscribe.AutoSubscriber).Assembly.Location,
        _ => throw new ArgumentOutOfRangeException(nameof(simpleName))
    };

    [Theory]
    [MemberData(nameof(PackageAssemblies))]
    public void Should_only_reference_allowed_reflection_members(string assemblyName)
    {
        using var stream = File.OpenRead(AssemblyPath(assemblyName));
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var found = new HashSet<string>();
        foreach (var handle in metadata.MemberReferences)
        {
            var memberReference = metadata.GetMemberReference(handle);
            var name = metadata.GetString(memberReference.Name);
            if (!Banned.Contains(name)) continue;

            if (memberReference.Parent.Kind != HandleKind.TypeReference) continue;
            var typeReference = metadata.GetTypeReference((TypeReferenceHandle)memberReference.Parent);
            var declaringType = $"{metadata.GetString(typeReference.Namespace)}.{metadata.GetString(typeReference.Name)}";

            // Only reflection namespaces count (e.g. JsonElement.GetProperty is not Type.GetProperty)
            if (!declaringType.StartsWith("System.Type") &&
                !declaringType.StartsWith("System.Reflection") &&
                !declaringType.StartsWith("System.Activator") &&
                !declaringType.StartsWith("System.Linq.Expressions")) continue;

            found.Add($"{assemblyName}:{declaringType}.{name}");
        }

        var unexpected = found.Where(f => !Allowed.ContainsKey(f)).ToList();
        unexpected.Should().BeEmpty(
            $"every banned reflection member must be in the ratchet allowlist; currently referenced: {string.Join(", ", found)}");

        // The hard bans: expression-tree compilation and Activator are gone for good
        found.Should().NotContain(f => f.Contains("Expressions"), "expression-tree compilation was removed in Phase 3");
        found.Should().NotContain(f => f.Contains("Activator"), "Activator.CreateInstance is banned");
    }

    [Fact]
    public void Core_should_not_reference_the_rabbitmq_client()
    {
        using var stream = File.OpenRead(AssemblyPath("EasyNetQ.Core"));
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var references = metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .ToList();

        references.Should().NotContain("RabbitMQ.Client", "EasyNetQ.Core must stay transport-agnostic");
    }
}
