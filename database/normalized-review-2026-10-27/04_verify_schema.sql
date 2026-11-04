-- PROPOSAL — not yet approved. Fresh review database only; not an in-place migration.

USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0 AND name<>N'sysdiagrams') <> 38 THROW 51002, 'Unexpected tables count.', 1;
IF (SELECT COUNT(*) FROM sys.key_constraints WHERE type='PK' AND parent_object_id NOT IN (SELECT object_id FROM sys.tables WHERE name=N'sysdiagrams')) <> 38 THROW 51002, 'Unexpected primary_keys count.', 1;
IF (SELECT COUNT(*) FROM sys.key_constraints WHERE type='UQ' AND parent_object_id NOT IN (SELECT object_id FROM sys.tables WHERE name=N'sysdiagrams')) <> 6 THROW 51002, 'Unexpected unique_constraints count.', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id NOT IN (SELECT object_id FROM sys.tables WHERE name=N'sysdiagrams')) <> 40 THROW 51002, 'Unexpected foreign_keys count.', 1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id NOT IN (SELECT object_id FROM sys.tables WHERE name=N'sysdiagrams')) <> 8 THROW 51002, 'Unexpected checks count.', 1;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51003, 'Untrusted FK.', 1;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THROW 51004, 'Untrusted CHECK.', 1;
SELECT SCHEMA_NAME(schema_id) AS schema_name,name FROM sys.tables WHERE is_ms_shipped=0 AND name<>N'sysdiagrams' ORDER BY name;
SELECT name,is_disabled,is_not_trusted FROM sys.foreign_keys ORDER BY name;
