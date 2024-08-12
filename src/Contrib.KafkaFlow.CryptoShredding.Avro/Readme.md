# Contrib.KafkaFlow.CryptoShredding.Avro

`Contrib.KafkaFlow.CryptoShredding.Avro` is a .NET package designed for secure handling of sensitive data in Kafka messages
using Avro serialization. The package introduces a string-based logical type called "encrypted-string"
that ensures sensitive information is encrypted and securely managed throughout the message lifecycle.

## Overview

In many systems, particularly multi-tenant architectures, messages may contain sensitive data that must be protected. This package enables
consumers to use tenant-specific encryption keys, ensuring that once a key is dropped, the secrets for a given tenant cannot be decrypted,
effectively rendering the data as deleted.

### Key Features

- **Sensitive Data Protection**: Supports the use of an "encrypted-string" logical type for handling sensitive data.
- **Tenant-Specific Encryption**: Allows each message to have its own encryption key, ideal for multi-tenant systems.
- **Safe Serialization**: Ensures that plain text strings cannot be accidentally serialized by throwing exceptions if attempted.

## EncryptedString Type

When C# code is generated for messages that include sensitive data, these strings are represented by the EncryptedString type. The
EncryptedString has two cases:

- `EncryptedString.Encrypted`: Represents the encrypted state of the data.
- `EncryptedString.Plain`: Represents the plain text version of the data.

To set a value in plain text, use:

```csharp
sensitiveData = EncryptedString.FromPlain("secret-value");
```

To prevent accidental leakage of secrets, `EncryptedString.Plain` cannot be serialized to Avro. An attempt to do so will result in an
exception.

## Encryption Process

For serialization to occur, the secrets must be encrypted first, converting `EncryptedString.Plain` into `EncryptedString.Encrypted`. The
encryption process requires an encryption key, which is retrieved using an `IEncryptionKeyProvider` service:

```csharp
public interface IEncryptionKeyProvider
{
    Task<string> GetKey(string keyId);
}
```

Application developers are responsible for securely storing and retrieving encryption keys. The key ID for encryption should be specified in
the `Crypto-Shredding-Key-Id` header when publishing the message to the KafkaFlow producer.

### Enabling Encryption

Encryption can be enabled transparently by adding the `AvroEncryptionProducerMiddleware` to the producer pipeline:

```csharp
services
    .AddSingleton<IEncryptionKeyProvider, BigOrgSecureEncryptionKeyProvider>()
    .AddKafkaFlowHostedService(kafka => kafka
        .AddCluster(cluster => cluster
            .AddProducer<IMyMessagesProducer>(producer => producer
                .AddMiddlewares(middlewares => middlewares
                    // Encrypt sensitive data before serialization
                    .Add<AvroEncryptionProducerMiddleware>()
                    .AddSchemaRegistryAvroSerializer()
```

The middleware will:

- Check the `Crypto-Shredding-Key-Id` header
- Retrieve the encryption key from the registered `IEncryptionKeyProvider`
- Encrypt the sensitive parts of the message before serialization to Avro

## Deserialization Process

Deserialization is handled by the `AvroEncryptionConsumerMiddleware`, which can be registered as follows:

```csharp
.AddConsumer(consumer => consumer
    .AddMiddlewares(middlewares => middlewares
        .AddSchemaRegistryAvroDeserializer()
        // Decrypt sensitive data before consumption
        .Add<AvroEncryptionConsumerMiddleware>()
```

The middleware will:

- Check the `Crypto-Shredding-Key-Id` header
- Retrieve the encryption key from the registered `IEncryptionKeyProvider`
- Decrypt the sensitive parts of the message

### Optional Decryption

The decryption step is optional.
If a consumer does not need access to the encrypted parts of the message, the decryption process can be omitted. The message will still be
deserialized and processed, but the secrets will remain encrypted.
