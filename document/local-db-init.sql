IF DB_ID(N'PRN232_GradingTool') IS NULL
BEGIN
    CREATE DATABASE [PRN232_GradingTool];
END;
GO

USE [PRN232_GradingTool];
GO

IF OBJECT_ID(N'dbo.Exams', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Exams]
    (
        [ExamId] INT IDENTITY(1,1) NOT NULL,
        [ExamName] NVARCHAR(200) NOT NULL,
        [SqlScriptPath] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Exams] PRIMARY KEY ([ExamId])
    );
END;
GO

IF OBJECT_ID(N'dbo.Submissions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Submissions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ExamId] INT NOT NULL,
        [StudentName] NVARCHAR(200) NOT NULL,
        [StudentCode] NVARCHAR(50) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [Status] INT NOT NULL CONSTRAINT [DF_Submissions_Status] DEFAULT (0),
        CONSTRAINT [PK_Submissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Submissions_Exams] FOREIGN KEY ([ExamId]) REFERENCES [dbo].[Exams]([ExamId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Submissions_ExamId] ON [dbo].[Submissions]([ExamId]);
END;
GO

IF OBJECT_ID(N'dbo.TestCases', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TestCases]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ExamId] INT NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        CONSTRAINT [PK_TestCases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestCases_Exams] FOREIGN KEY ([ExamId]) REFERENCES [dbo].[Exams]([ExamId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_TestCases_ExamId] ON [dbo].[TestCases]([ExamId]);
END;
GO

IF OBJECT_ID(N'dbo.TestResults', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TestResults]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [SubmissionId] INT NOT NULL,
        [Score] DECIMAL(8, 2) NOT NULL,
        [ReportPath] NVARCHAR(500) NULL,
        CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestResults_Submissions] FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_TestResults_SubmissionId] ON [dbo].[TestResults]([SubmissionId]);
END;
GO
