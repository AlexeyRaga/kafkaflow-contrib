CREATE
EXTENSION IF NOT EXISTS "uuid-ossp";

/* Schema */
DO
$do$
BEGIN
    IF
NOT EXISTS (
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name = 'outbox'
        ) THEN
            EXECUTE ('CREATE SCHEMA "outbox"');
END IF;
END
$do$;
