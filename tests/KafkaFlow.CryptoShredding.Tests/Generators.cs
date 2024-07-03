using Contrib.KafkaFlow.CryptoShredding.TestContract;
using FsCheck;
using FsCheck.Fluent;
using KafkaFlow.CryptoShredding.Avro;
using Avro.LogicalTypes;

namespace KafkaFlow.CryptoShredding.Tests;

public sealed record Domain(string Value);

public sealed record Email(string Value);

public static class Generators
{
    public static Arbitrary<Domain> DomainGen() =>
        (from tld in Gen.Elements(["com", "net", "org", "io", "dev"])
            from domain in ArbMap.Default.GeneratorFor<NonEmptyString>()
            select new Domain($"{domain.Item}.{tld}")).ToArbitrary();

    public static Arbitrary<Email> EmailGen() =>
        (from localPart in ArbMap.Default.GeneratorFor<NonEmptyString>()
            from domain in DomainGen().Generator
            select new Email($"{localPart.Item}@{domain}")).ToArbitrary();


    public static Arbitrary<Secret> Secret() =>
        ArbMap.Default.ArbFor<NonNull<string>>().Convert(x => new Secret { Value = x.Item }, x => NonNull<string>.NewNonNull(x.Value));

    public static Arbitrary<EncryptedSecret> EncryptedSecret() =>
        TypedEncryptedString().Convert(
            x => new EncryptedSecret { Value = x },
            x => x.Value);

    public static Arbitrary<EncryptedString> TypedEncryptedString() =>
        ArbMap.Default.ArbFor<NonNull<string>>().Convert(
            x => EncryptedString.FromPlain(x.Item),
            x =>  NonNull<string>.NewNonNull(((EncryptedString.Decrypred)x).Value));

    public static Arbitrary<InlineEncryptedMessage> UserRegistered()
    {
        var cloneSecret = (Secret secret) => new Secret { Value = secret.Value };

        var gen =
            from id in ArbMap.Default.GeneratorFor<Guid>()
            from secret in ArbMap.Default.GeneratorFor<Secret>()
            from size in Gen.Choose(0, 5)
            select new InlineEncryptedMessage
            {
                id = id,
                email = secret.Value,
                encryptedStrings = Enumerable.Repeat(secret.Value, size).ToList(),
                secret = cloneSecret(secret),
                secrets = Enumerable.Range(0, size).Select(_ => cloneSecret(secret)).ToList(),
                secretOrString = cloneSecret(secret),
                secretsMap = new Dictionary<string, Secret>
                {
                    ["location"] = cloneSecret(secret)
                },
                encryptedMap = new Dictionary<string, string>
                {
                    ["password"] = secret.Value,
                    ["username"] = secret.Value
                },
                innerSecret = new() { Value = secret.Value}
            };



        return gen.ToArbitrary();
    }

    public static Arbitrary<EncryptedMessage> UserUpdated()
    {
        var cloneSecret = (EncryptedSecret secret) => new EncryptedSecret { Value = secret.Value };

        var gen =
            from id in ArbMap.Default.GeneratorFor<Guid>()
            from secret in EncryptedSecret().Generator
            from size in Gen.Choose(0, 5)
            select new EncryptedMessage
            {
                id = id,
                email = secret.Value,
                encryptedStrings = Enumerable.Repeat(secret.Value, size).ToList(),
                secret = cloneSecret(secret),
                secrets = Enumerable.Range(0, size).Select(_ => cloneSecret(secret)).ToList(),
                secretOrString = cloneSecret(secret),
                secretsMap = new Dictionary<string, EncryptedSecret>
                {
                    ["location"] = cloneSecret(secret)
                },
                // encryptedMap = new Dictionary<string, EncryptedString>
                // {
                //     ["password"] = secret.Value,
                //     ["username"] = secret.Value
                // },
                innerSecret = new() { Value = secret.Value}
            };



        return gen.ToArbitrary();
    }
}
