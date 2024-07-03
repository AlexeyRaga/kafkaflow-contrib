using Avro.Specific;

namespace KafkaFlow.CryptoShredding.Avro;

public sealed class AvroEncryptionConsumerMiddleware(IEncryptionKeyProvider keyProvider) : IMessageMiddleware
{
    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        if (context.Message.Value is ISpecificRecord message
            && context.Headers.GetString(CryptoShreddingHeaders.CryptoShreddingKey) is { } keyId)
        {
            var key = await keyProvider.GetKey(keyId);
            using var keyEncryptor = new AesKeyedEncryptor(key);
            AvroCryptoShredder.Decrypt(keyEncryptor, message);
        }

        await next(context).ConfigureAwait(false);
    }

}
