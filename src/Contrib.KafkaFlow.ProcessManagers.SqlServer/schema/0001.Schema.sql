IF NOT EXISTS(SELECT * FROM sys.schemas WHERE name = 'process_managers')
BEGIN
	EXEC ('CREATE SCHEMA [process_managers] AUTHORIZATION [dbo]')
END
GO
