using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace EasyNetQ.Tests;

/// <summary>
///     Ratchets down runtime reflection in the EasyNetQ assembly: scans the compiled metadata for references to
///     banned reflection members and fails when a new one appears. Members listed in <see cref="Allowed" /> are the
///     sanctioned escape hatches (each with its reason); the target is an empty allowlist for the Core assembly by
///     the end of the v9 transport split.
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
        // reason: runtime fallback for Types not pre-registered by the source generator
        ["System.Type.MakeGenericType"] = "RuntimeDescriptorFactory + type name serializers (generator makes unreachable)",
        ["System.Reflection.Assembly.GetType"] = "DefaultTypeNameSerializer wire-name resolution (moves to bundle)",
        ["System.Type.GetType"] = "DefaultTypeNameSerializer wire-name resolution (moves to bundle)",
        ["System.Reflection.Assembly.Load"] = "DefaultTypeNameSerializer wire-name resolution (moves to bundle)",
        ["System.Reflection.MethodInfo.MakeGenericMethod"] = "NonGenericRpcExtensions pair bridge + AutoSubscriber (move to bundle)",
        ["System.Reflection.MemberInfo.GetCustomAttributes"] = "AttributeMetadataReader descriptor fallback + AutoSubscriber",
        ["System.Reflection.CustomAttributeExtensions.GetCustomAttribute"] = "ConnectionConfigurationExtensions platform stamping (setup only)",
        ["System.Type.IsAssignableFrom"] = "HandlerTable/HandlerCollection polymorphic match (memoized) + PullingConsumer type guard",
        ["System.Reflection.Assembly.GetTypes"] = "AutoSubscriberExtensions assembly scanning (moves to bundle)",
    };

    [Fact]
    public void Should_only_reference_allowed_reflection_members()
    {
        using var stream = File.OpenRead(typeof(IBus).Assembly.Location);
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

            found.Add($"{declaringType}.{name}");
        }

        var unexpected = found.Where(f => !Allowed.ContainsKey(f)).ToList();
        unexpected.Should().BeEmpty(
            $"every banned reflection member must be in the ratchet allowlist; currently referenced: {string.Join(", ", found)}");

        // The hard bans: expression-tree compilation and Activator are gone for good
        found.Should().NotContain(f => f.Contains("Expressions"), "expression-tree compilation was removed in Phase 3");
        found.Should().NotContain(f => f.Contains("Activator"), "Activator.CreateInstance is banned");
    }
}
