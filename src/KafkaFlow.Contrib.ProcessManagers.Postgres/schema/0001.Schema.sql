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
                WHERE schema_name = 'process_managers'
            ) THEN
            EXECUTE ('CREATE SCHEMA "process_managers"');
        END IF;
    END
$do$;
