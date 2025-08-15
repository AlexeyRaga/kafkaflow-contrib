using System.Security.Cryptography;
using System.Text;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public static class GuidUtils
{
    /// <summary>
    /// Generates a UUID v5 based on a namespace and name.
    /// </summary>
    /// <param name="namespace">The namespace to use for UUID generation</param>
    /// <param name="name">The name to use for UUID generation</param>
    /// <returns>A deterministic UUID v5</returns>
    public static Guid GenerateV5(string @namespace, Guid name)
    {
        if (@namespace is null) throw new ArgumentNullException(nameof(@namespace));

        // Convert namespace string to bytes (full length, not truncated!)
        byte[] namespaceBytes = Encoding.UTF8.GetBytes(@namespace);

        // Convert GUID name to network byte order
        Span<byte> nameBytes = stackalloc byte[16];
        name.TryWriteBytes(nameBytes);
        SwapGuid(nameBytes);

        // Concatenate namespace + name
        byte[] buffer = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(buffer, 0);
        nameBytes.CopyTo(buffer.AsSpan(namespaceBytes.Length));

        // Hash with SHA-1
        Span<byte> sha1 = stackalloc byte[20];
        SHA1.HashData(buffer, sha1);

        // Take first 16 bytes and set version/variant bits
        Span<byte> uuid = sha1[..16];
        uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50); // version 5
        uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80); // RFC 4122 variant

        // Convert back from network byte order
        SwapGuid(uuid);
        return new Guid(uuid);
    }

    private static void SwapGuid(Span<byte> g)
    {
        (g[0], g[3]) = (g[3], g[0]);
        (g[1], g[2]) = (g[2], g[1]);
        (g[4], g[5]) = (g[5], g[4]);
        (g[6], g[7]) = (g[7], g[6]);
    }

}
