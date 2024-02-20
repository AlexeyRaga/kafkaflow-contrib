/* Table */
CREATE TABLE "process_managers"."processes"
(
    sequence_id    SERIAL                      NOT NULL,
    process_type   VARCHAR(255)                NOT NULL,
    process_id     UUID                        NOT NULL,
    process_state  TEXT                        NOT NULL,
    date_added_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL
        CONSTRAINT df_process_managers_date_added_utc DEFAULT (now() AT TIME ZONE 'utc'),
    date_updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL
        CONSTRAINT df_process_managers_date_updated_utc DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT pk_process_managers_processes PRIMARY KEY (sequence_id),
    CONSTRAINT ck_process_managers_process_state_not_blank_or_empty CHECK (TRIM(process_state) <> '')
);

CREATE UNIQUE INDEX ix_process_managers_type_and_id ON process_managers.processes (process_type, process_id) WITH (FILLFACTOR = 90);
