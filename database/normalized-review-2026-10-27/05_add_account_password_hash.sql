-- Additive credential-storage correction. No default password and no existing data reset.
-- Apply to the existing review DB; fresh installations obtain the column from 02_create_schema.sql.
USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Account',N'U') IS NULL
    THROW 51991, 'Account table is missing; refusing to create an unrelated schema.', 1;

SELECT account_id,employee_id,username,status INTO #AccountBeforePasswordStorage FROM dbo.Account;

IF COL_LENGTH(N'dbo.Account',N'password_hash') IS NULL
    EXEC(N'ALTER TABLE dbo.Account ADD password_hash nvarchar(512) NULL;');

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id=OBJECT_ID(N'dbo.Account') AND name=N'password_hash'
      AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=1024 AND is_nullable=1
)
    THROW 51992, 'Existing password_hash differs from nvarchar(512) NULL; manual review required.', 1;

IF EXISTS (
    SELECT account_id,employee_id,username,status FROM #AccountBeforePasswordStorage
    EXCEPT SELECT account_id,employee_id,username,status FROM dbo.Account
) OR EXISTS (
    SELECT account_id,employee_id,username,status FROM dbo.Account
    EXCEPT SELECT account_id,employee_id,username,status FROM #AccountBeforePasswordStorage
)
    THROW 51993, 'Account fields changed unexpectedly.', 1;

DROP TABLE #AccountBeforePasswordStorage;
COMMIT TRANSACTION;
PRINT 'PASS: password_hash storage available; existing account fields preserved. NULL hashes cannot authenticate.';
GO
