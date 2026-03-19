IF DB_ID(N'FA25BearDB') IS NULL
BEGIN
    CREATE DATABASE FA25BearDB;
END
GO

USE FA25BearDB;
GO

IF OBJECT_ID(N'dbo.BearProfile', N'U') IS NOT NULL DROP TABLE dbo.BearProfile;
IF OBJECT_ID(N'dbo.BearAccount', N'U') IS NOT NULL DROP TABLE dbo.BearAccount;
IF OBJECT_ID(N'dbo.BearType', N'U') IS NOT NULL DROP TABLE dbo.BearType;
GO

CREATE TABLE dbo.BearType (
    BearTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BearTypeName NVARCHAR(250) NULL,
    Origin NVARCHAR(250) NULL,
    Description NVARCHAR(1000) NULL
);

CREATE TABLE dbo.BearAccount (
    AccountID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    [Password] NVARCHAR(100) NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    RoleId INT NOT NULL
);

CREATE TABLE dbo.BearProfile (
    BearProfileId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BearTypeId INT NOT NULL,
    BearName NVARCHAR(150) NOT NULL,
    [Weight] FLOAT NOT NULL,
    Characteristics NVARCHAR(2000) NOT NULL,
    CareNeeds NVARCHAR(1500) NOT NULL,
    ModifiedDate DATETIME NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT FK_BearProfile_BearType FOREIGN KEY (BearTypeId)
        REFERENCES dbo.BearType (BearTypeId)
);
GO

INSERT INTO dbo.BearType (BearTypeName, Origin, Description)
VALUES
(N'Grizzly', N'North America', N'Large brown bear'),
(N'Polar', N'Arctic', N'White bear adapted to cold'),
(N'Panda', N'China', N'Black and white bear');

INSERT INTO dbo.BearAccount (UserName, [Password], FullName, Email, Phone, RoleId)
VALUES
(N'manager', N'123', N'Manager One', N'manager@hb.com', N'0900000001', 2),
(N'staff', N'123', N'Staff One', N'staff@hb.com', N'0900000002', 3),
(N'member', N'123', N'Member One', N'member@hb.com', N'0900000003', 4);

INSERT INTO dbo.BearProfile (BearTypeId, BearName, [Weight], Characteristics, CareNeeds, ModifiedDate)
VALUES
(1, N'Grizzly#1', 350.5, N'Strong and large', N'Needs large habitat', GETDATE()),
(2, N'Polar#1', 400.2, N'Excellent swimmer', N'Needs cold environment', GETDATE());
