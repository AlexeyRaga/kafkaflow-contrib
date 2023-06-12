using FluentAssertions;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class A
{
    public A(string value)
    {
        Value = value;
    }
    public string Value { get; set; }
}

public sealed class C : IEquatable<C>
{
    public C(string value)
    {
        Value = value;
    }
    public string Value { get; set; }

    public bool Equals(C? other) => Value == other?.Value;
}

public sealed record B(string Value);

public sealed class Playground
{
    [Fact]
    public void ShouldEqual()
    {
        EqualityComparer<A>.Default.Equals(new A("a"), new A("a")).Should().BeFalse();
        EqualityComparer<C>.Default.Equals(new C("a"), new C("a")).Should().BeTrue();

        EqualityComparer<B>.Default.Equals(new B("a"), new B("a")).Should().BeTrue();
    }
}
