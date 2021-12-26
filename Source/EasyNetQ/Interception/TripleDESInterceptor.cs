using System.Buffers;
using System.Security.Cryptography;

namespace EasyNetQ.Interception;

/// <summary>
///     An interceptor which encrypts and decrypts messages
/// </summary>
public class TripleDESInterceptor : IProduceConsumeInterceptor
{
    private readonly byte[] iv;
    private readonly byte[] key;

    /// <summary>
    ///     Creates TripleDESInterceptor
    /// </summary>
    public TripleDESInterceptor(byte[] key, byte[] iv)
    {
        this.iv = iv;
        this.key = key;
    }

    /// <inheritdoc />
    public ProducedMessage OnProduce(in ProducedMessage message)
    {
        var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

        try
        {
            message.Body.CopyTo(body);
            using var tripleDes = TripleDES.Create();
            using var tripleDesEncryptor = tripleDes.CreateEncryptor(key, iv);
            var encryptedBody = tripleDesEncryptor.TransformFinalBlock(body, 0, message.Body.Length);
            return new ProducedMessage(message.Properties, encryptedBody);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(body);
        }
    }

    /// <inheritdoc />
    public ConsumedMessage OnConsume(in ConsumedMessage message)
    {
        using var tripleDes = TripleDES.Create();
        using var tripleDesDecryptor = tripleDes.CreateDecryptor(key, iv);

        var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

        try
        {
            message.Body.CopyTo(body);
            return new ConsumedMessage(
                message.ReceivedInfo,
                message.Properties,
                tripleDesDecryptor.TransformFinalBlock(body, 0, message.Body.Length)
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(body);
        }
    }
}
