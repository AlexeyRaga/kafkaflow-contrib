/* Table */
IF OBJECT_ID(N'[outbox].[outbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [outbox].[outbox](
	    [sequence_id] [bigint] IDENTITY(1,1) NOT NULL,
	    [topic_name] [nvarchar](255) NOT NULL,
	    [partition] [int] NULL,
	    [message_key] [varbinary](max) NULL,
	    [message_headers] [nvarchar](max) NULL,
	    [message_body] [varbinary](max) NULL,
	    [date_added_utc] [datetime2] NOT NULL DEFAULT(SYSUTCDATETIME()),
        [rowversion] [int] NOT NULL DEFAULT(1),
	    CONSTRAINT [PK_outbox] PRIMARY KEY CLUSTERED ([sequence_id] ASC),
	    CONSTRAINT [CK_headers_not_blank_or_empty] CHECK ((TRIM([message_headers])<>N'')),
	    CONSTRAINT [CK_topic_name_not_blank_or_empty] CHECK ((TRIM([topic_name])<>N''))
    )
END
GO
