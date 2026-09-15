# Contrib.KafkaFlow.Outbox.SqlServer

SQL Server backend for `Contrib.KafkaFlow.Outbox`.

```csharp
services.AddSqlServerOutboxBackend(connectionString);
```

Reads and writes `[outbox].[outbox]`. The backend does not provision it — run the
`schema/` scripts, which create exactly that schema and table, as part of your migrations.

## Custom schema or table name

Where `[outbox].[outbox]` is taken, or a separate migration/dacpac project decides where
the table lives, point the backend at another table:

```csharp
services.AddSqlServerOutboxBackend(
    connectionString,
    new SqlServerOutboxOptions { SchemaName = "Outbox", TableName = "KafkaMessages" });
```

Only the location is configurable — the column names and types in `schema/0002.Table.sql`
are part of the contract and must be reproduced as-is. The shipped scripts create only the
default table, so a custom one is yours to create.

Note SQL Server identifier comparison follows the database collation, which is commonly
case-insensitive; `[outbox].[outbox]` and `[Outbox].[Outbox]` are then the same object.
