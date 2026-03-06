USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AIcoding')
BEGIN
    CREATE DATABASE AIcoding;
END
GO

USE AIcoding;
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'user',
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1
    );
END
GO

-- Seed admin user (password will be set by API on startup)
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, Role)
    VALUES ('admin', 'admin@template.local', 'placeholder', 'admin');
END
GO

PRINT 'Database initialized successfully';
GO
