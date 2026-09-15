using AwesomeAssertions;
using KafkaFlow.Outbox;
using KafkaFlow.Outbox.SqlServer;
using Microsoft.Extensions.Configuration;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class SqlServerOutboxTableLocationTests
{
    private static string ConnectionString() =>
        new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("SqlServerBackend")!;

    [Fact]
    public async Task Should_round_trip_through_a_configured_table()
    {
        var repository = new SqlServerOutboxRepository(
            ConnectionString(),
            new SqlServerOutboxOptions { SchemaName = "Custom", TableName = "kafka_messages" });

        var row = new OutboxTableRow("a-topic", 3, [1, 2, 3], """{"header":"value"}""", [4, 5, 6]);

        await repository.Store(row);
        var read = await repository.Read(10);

        read.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(row, options => options.Excluding(x => x.SequenceId));
    }

    [Fact]
    public void Should_reject_an_empty_identifier_on_assignment() =>
        FluentActions.Invoking(() => new SqlServerOutboxOptions { TableName = " " })
            .Should().Throw<ArgumentException>().WithParameterName("TableName");

    [Fact]
    public void Should_print_without_throwing() =>
        new SqlServerOutboxOptions { SchemaName = "Custom", TableName = "kafka_messages" }
            .ToString().Should().Contain("kafka_messages");

    [Fact]
    public void Should_escape_a_closing_bracket_in_an_identifier() =>
        new SqlServerOutboxOptions { SchemaName = "we[i]rd", TableName = "t]bl" }
            .QualifiedTableName.Should().Be("[we[i]]rd].[t]]bl]");

    [Fact]
    public void Should_default_to_the_shipped_schema() =>
        new SqlServerOutboxOptions().QualifiedTableName.Should().Be("[outbox].[outbox]");
}
