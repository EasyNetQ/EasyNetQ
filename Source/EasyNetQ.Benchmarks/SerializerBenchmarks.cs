using BenchmarkDotNet.Attributes;
using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;

namespace EasyNetQ.Benchmarks;

[MemoryDiagnoser]
public class SerializerBenchmarks
{
    private ISerializer systemTextJson = null!;
    private ISerializer systemTextJsonV2 = null!;
    private ISerializer newtonsoft = null!;

    private object message = null!;
    private Type messageType = null!;

    private ReadOnlyMemory<byte> systemTextJsonBytes;
    private ReadOnlyMemory<byte> systemTextJsonV2Bytes;
    private ReadOnlyMemory<byte> newtonsoftBytes;

    [Params("Small", "Medium", "Large")]
    public string Size { get; set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        systemTextJson = new SystemTextJsonSerializer();
        systemTextJsonV2 = new SystemTextJsonSerializerV2();
        newtonsoft = new NewtonsoftJsonSerializer();

        message = SampleMessages.Create(Size);
        messageType = SampleMessages.GetType(Size);

        using var stj = systemTextJson.MessageToBytes(messageType, message);
        systemTextJsonBytes = stj.Memory.ToArray();

        using var stj2 = systemTextJsonV2.MessageToBytes(messageType, message);
        systemTextJsonV2Bytes = stj2.Memory.ToArray();

        using var nj = newtonsoft.MessageToBytes(messageType, message);
        newtonsoftBytes = nj.Memory.ToArray();
    }

    [Benchmark]
    public void SystemTextJson_Serialize()
    {
        using var result = systemTextJson.MessageToBytes(messageType, message);
    }

    [Benchmark]
    public void SystemTextJsonV2_Serialize()
    {
        using var result = systemTextJsonV2.MessageToBytes(messageType, message);
    }

    [Benchmark]
    public void Newtonsoft_Serialize()
    {
        using var result = newtonsoft.MessageToBytes(messageType, message);
    }

    [Benchmark]
    public object SystemTextJson_Deserialize()
    {
        return systemTextJson.BytesToMessage(messageType, systemTextJsonBytes);
    }

    [Benchmark]
    public object SystemTextJsonV2_Deserialize()
    {
        return systemTextJsonV2.BytesToMessage(messageType, systemTextJsonV2Bytes);
    }

    [Benchmark]
    public object Newtonsoft_Deserialize()
    {
        return newtonsoft.BytesToMessage(messageType, newtonsoftBytes);
    }
}
