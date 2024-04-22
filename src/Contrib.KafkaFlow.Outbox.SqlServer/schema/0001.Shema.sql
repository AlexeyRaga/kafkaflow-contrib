IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'kafkaflowtest')
BEGIN
	CREATE DATABASE [kafkaflowtest]
END
GO
