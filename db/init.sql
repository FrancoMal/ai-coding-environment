USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AIcoding')
BEGIN
    CREATE DATABASE AIcoding;
END
GO

USE AIcoding;
GO

-- Roles table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE Roles (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(255) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Seed default roles
IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'admin')
BEGIN
    INSERT INTO Roles (Name, Description) VALUES ('admin', 'Administrador con acceso total');
END
GO

IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'usuario')
BEGIN
    INSERT INTO Roles (Name, Description) VALUES ('usuario', 'Usuario con acceso basico');
END
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'usuario',
        RoleId INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1,
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
    );
END
GO

-- Add new columns if table already exists but columns don't
IF EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'FirstName')
BEGIN
    ALTER TABLE Users ADD FirstName NVARCHAR(100) NULL;
    ALTER TABLE Users ADD LastName NVARCHAR(100) NULL;
    ALTER TABLE Users ADD Phone NVARCHAR(50) NULL;
END
GO

-- Add RoleId column if it doesn't exist
IF EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'RoleId')
BEGIN
    ALTER TABLE Users ADD RoleId INT NULL;
    -- Set existing admin users to role 1 (admin)
    UPDATE Users SET RoleId = 1 WHERE Role = 'admin';
    -- Set existing regular users to role 2 (usuario)
    UPDATE Users SET RoleId = 2 WHERE Role != 'admin' OR RoleId IS NULL;
    -- Make it not null with default
    ALTER TABLE Users ALTER COLUMN RoleId INT NOT NULL;
    ALTER TABLE Users ADD CONSTRAINT DF_Users_RoleId DEFAULT 2 FOR RoleId;
    ALTER TABLE Users ADD CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id);
END
GO

-- Integrations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Integrations' AND xtype='U')
BEGIN
    CREATE TABLE Integrations (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Provider NVARCHAR(50) NOT NULL UNIQUE,
        AppId NVARCHAR(255) NULL,
        AppSecret NVARCHAR(255) NULL,
        RedirectUrl NVARCHAR(500) NULL,
    Settings NVARCHAR(MAX) NULL,
        IsActive BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- MeliAccounts table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MeliAccounts' AND xtype='U')
BEGIN
    CREATE TABLE MeliAccounts (
        Id INT PRIMARY KEY IDENTITY(1,1),
        MeliUserId BIGINT NOT NULL UNIQUE,
        Nickname NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NULL,
        AccessToken NVARCHAR(MAX) NOT NULL,
        RefreshToken NVARCHAR(MAX) NULL,
        TokenExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- MeliOrders table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MeliOrders' AND xtype='U')
BEGIN
    CREATE TABLE MeliOrders (
        Id INT PRIMARY KEY IDENTITY(1,1),
        MeliOrderId BIGINT NOT NULL,
        MeliAccountId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        DateCreated DATETIME2 NOT NULL,
        DateClosed DATETIME2 NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        CurrencyId NVARCHAR(10) NOT NULL,
        BuyerId BIGINT NOT NULL,
        BuyerNickname NVARCHAR(255) NOT NULL,
        ItemId NVARCHAR(50) NOT NULL,
        ItemTitle NVARCHAR(500) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        ShippingId BIGINT NULL,
        PackId BIGINT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_MeliOrders_MeliAccounts FOREIGN KEY (MeliAccountId) REFERENCES MeliAccounts(Id)
    );
    CREATE UNIQUE INDEX IX_MeliOrders_MeliOrderId_ItemId ON MeliOrders (MeliOrderId, ItemId);
    CREATE INDEX IX_MeliOrders_MeliAccountId ON MeliOrders (MeliAccountId);
    CREATE INDEX IX_MeliOrders_DateCreated ON MeliOrders (DateCreated);
    CREATE INDEX IX_MeliOrders_PackId ON MeliOrders (PackId);
END
GO

-- Add PackId column if table already exists
IF EXISTS (SELECT * FROM sysobjects WHERE name='MeliOrders' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MeliOrders') AND name = 'PackId')
BEGIN
    ALTER TABLE MeliOrders ADD PackId BIGINT NULL;
    CREATE INDEX IX_MeliOrders_PackId ON MeliOrders (PackId);
END
GO

-- MeliItems table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MeliItems' AND xtype='U')
BEGIN
    CREATE TABLE MeliItems (
        Id INT PRIMARY KEY IDENTITY(1,1),
        MeliItemId NVARCHAR(50) NOT NULL,
        MeliAccountId INT NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        CategoryId NVARCHAR(50) NULL,
        Price DECIMAL(18,2) NOT NULL DEFAULT 0,
        OriginalPrice DECIMAL(18,2) NULL,
        CurrencyId NVARCHAR(10) NOT NULL DEFAULT 'ARS',
        AvailableQuantity INT NOT NULL DEFAULT 0,
        SoldQuantity INT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'active',
        Condition NVARCHAR(20) NULL,
        ListingTypeId NVARCHAR(50) NULL,
        Thumbnail NVARCHAR(500) NULL,
        Permalink NVARCHAR(1000) NULL,
        Sku NVARCHAR(255) NULL,
        UserProductId NVARCHAR(100) NULL,
        FamilyId NVARCHAR(100) NULL,
        FamilyName NVARCHAR(500) NULL,
        DateCreated DATETIME2 NULL,
        LastUpdated DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_MeliItems_MeliAccounts FOREIGN KEY (MeliAccountId) REFERENCES MeliAccounts(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_MeliItems_MeliItemId ON MeliItems (MeliItemId);
    CREATE INDEX IX_MeliItems_MeliAccountId ON MeliItems (MeliAccountId);
    CREATE INDEX IX_MeliItems_Status ON MeliItems (Status);
    CREATE INDEX IX_MeliItems_UserProductId ON MeliItems (UserProductId);
    CREATE INDEX IX_MeliItems_FamilyId ON MeliItems (FamilyId);
END
GO

-- AuditLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT PRIMARY KEY IDENTITY(1,1),
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        Changes NVARCHAR(MAX) NULL,
        UserName NVARCHAR(100) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs (EntityType, EntityId);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs (CreatedAt DESC);
END
GO

-- Seed admin user (password will be set by API on startup)
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role, RoleId)
    VALUES ('admin', 'admin@template.local', 'placeholder', 'Admin', 'Sistema', 'admin', 1);
END
GO

PRINT 'Database initialized successfully';
GO

-- Add CategoryPath column to MeliItems
IF EXISTS (SELECT * FROM sysobjects WHERE name='MeliItems' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MeliItems') AND name = 'CategoryPath')
BEGIN
    ALTER TABLE MeliItems ADD CategoryPath NVARCHAR(500) NULL;
END
GO

-- Add InstallmentTag column to MeliItems
IF EXISTS (SELECT * FROM sysobjects WHERE name='MeliItems' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MeliItems') AND name = 'InstallmentTag')
BEGIN
    ALTER TABLE MeliItems ADD InstallmentTag NVARCHAR(50) NULL;
END
GO

-- Add FreeShipping column to MeliItems
IF EXISTS (SELECT * FROM sysobjects WHERE name='MeliItems' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MeliItems') AND name = 'FreeShipping')
BEGIN
    ALTER TABLE MeliItems ADD FreeShipping BIT NOT NULL DEFAULT 0;
END
GO

-- ScheduledProcesses table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ScheduledProcesses' AND xtype='U')
BEGIN
    CREATE TABLE ScheduledProcesses (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(100) NOT NULL UNIQUE,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(500) NULL,
        TriggerType NVARCHAR(20) NOT NULL DEFAULT 'Interval',
        IntervalMinutes INT NULL,
        DailyAtTime NVARCHAR(5) NULL,
        CronExpression NVARCHAR(100) NULL,
        IsEnabled BIT NOT NULL DEFAULT 0,
        LastRunAt DATETIME2 NULL,
        LastRunStatus NVARCHAR(20) NULL,
        LastRunDurationMs INT NULL,
        NextRunAt DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- ProcessExecutionLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessExecutionLogs' AND xtype='U')
BEGIN
    CREATE TABLE ProcessExecutionLogs (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ProcessCode NVARCHAR(100) NOT NULL,
        StartedAt DATETIME2 NOT NULL,
        FinishedAt DATETIME2 NULL,
        Status NVARCHAR(20) NOT NULL,
        DurationMs INT NULL,
        ResultSummary NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        CONSTRAINT FK_ProcessLogs_Process FOREIGN KEY (ProcessCode) REFERENCES ScheduledProcesses(Code) ON DELETE CASCADE
    );
    CREATE INDEX IX_ProcessLogs_ProcessCode ON ProcessExecutionLogs (ProcessCode);
    CREATE INDEX IX_ProcessLogs_StartedAt ON ProcessExecutionLogs (StartedAt DESC);
END
GO

-- Seed scheduled processes
IF NOT EXISTS (SELECT * FROM ScheduledProcesses WHERE Code = 'SyncMeliOrders')
BEGIN
    INSERT INTO ScheduledProcesses (Code, Name, Description, TriggerType, IntervalMinutes, IsEnabled)
    VALUES ('SyncMeliOrders', 'Sincronizar Ordenes', 'Sincroniza las ordenes de MercadoLibre de los ultimos 7 dias', 'Interval', 360, 0);
END
GO

IF NOT EXISTS (SELECT * FROM ScheduledProcesses WHERE Code = 'SyncMeliItems')
BEGIN
    INSERT INTO ScheduledProcesses (Code, Name, Description, TriggerType, IntervalMinutes, IsEnabled)
    VALUES ('SyncMeliItems', 'Sincronizar Publicaciones', 'Sincroniza las publicaciones activas de MercadoLibre', 'Interval', 360, 0);
END
GO
