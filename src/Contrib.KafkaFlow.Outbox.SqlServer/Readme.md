# Contrib.KafkaFlow.Outbox.SqlServer

SQL Server backend for `Contrib.KafkaFlow.Outbox`.

```csharp
services.AddSqlServerOutboxBackend(connectionString);
```

Creates the table described by `schema/`: `[outbox].[outbox]`.

## Custom schema or table name

Where `[outbox].[outbox]` is taken, or the schema is owned by a separate
migration/dacpac project, point the backend at another table:

```csharp
services.AddSqlServerOutboxBackend(
    connectionString,
    new SqlServerOutboxOptions { SchemaName = "Outbox", TableName = "KafkaMessages" });
```

Only the location is configurable — the column names and types in `schema/0002.Table.sql`
are part of the contract and must be reproduced as-is.

Note SQL Server identifier comparison follows the database collation, which is commonly
case-insensitive; `[outbox].[outbox]` and `[Outbox].[Outbox]` are then the same object.
