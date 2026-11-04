-- PROPOSAL — not yet approved. Fresh review database only; not an in-place migration.
USE [master];
GO
IF DB_ID(N'SWD392_BookSales_3NF_REVIEW_20261027') IS NOT NULL
    THROW 51000, 'Review database already exists. No overwrite or drop is performed.', 1;
GO
CREATE DATABASE [SWD392_BookSales_3NF_REVIEW_20261027];
GO
