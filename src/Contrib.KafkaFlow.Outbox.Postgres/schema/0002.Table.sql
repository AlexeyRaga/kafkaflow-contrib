/* Table */
CREATE TABLE "outbox"."outbox"
(
    sequence_id     SERIAL       NOT NULL,
    topic_name      VARCHAR(255) NOT NULL,
    partition       INT NULL,
    message_key     BYTEA NULL,
    message_headers TEXT NULL,
    message_body    BYTEA NULL,
    date_added_utc  TIMESTAMP WITHOUT TIME ZONE NOT NULL CONSTRAINT df_Outbox_Outbox_date_added_utc DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT pk_Outbox_Outbox PRIMARY KEY (sequence_id),
    CONSTRAINT ck_Outbox_Outbox_Headers_Not_Blank_Or_Empty CHECK (TRIM(message_headers) <> ''),
    CONSTRAINT ck_Outbox_Outbox_topic_name_not_blank_or_empty CHECK (TRIM(topic_name) <> '')
);
