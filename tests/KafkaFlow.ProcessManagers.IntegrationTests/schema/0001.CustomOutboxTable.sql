/* A second outbox table, to prove SqlServerOutboxOptions locates a non-default one. */
IF NOT EXISTS(SELECT * FROM sys.schemas WHERE name = 'Custom')
BEGIN
	EXEC ('CREATE SCHEMA [Custom] AUTHORIZATION [dbo]')
END
GO

IF OBJECT_ID(N'[Custom].[kafka_messages]', N'U') IS NULL
BEGIN
    CREATE TABLE [Custom].[kafka_messages](
	    [sequence_id] [bigint] IDENTITY(1,1) NOT NULL,
	    [topic_name] [nvarchar](255) NOT NULL,
	    [partition] [int] NULL,
	    [message_key] [varbinary](max) NULL,
	    [message_headers] [nvarchar](max) NULL,
	    [message_body] [varbinary](max) NULL,
	    [date_added_utc] [datetime2] NOT NULL DEFAULT(SYSUTCDATETIME()),
        [rowversion] [int] NOT NULL DEFAULT(1),
	    CONSTRAINT [PK_Custom_kafka_messages] PRIMARY KEY CLUSTERED ([sequence_id] ASC),
	    CONSTRAINT [CK_Custom_kafka_messages_headers_not_blank_or_empty] CHECK ((TRIM([message_headers])<>N'')),
	    CONSTRAINT [CK_Custom_kafka_messages_topic_name_not_blank_or_empty] CHECK ((TRIM([topic_name])<>N''))
    )
END
GO
