CREATE TABLE [dbo].[Role] (
    [RoleId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [Name]   VARCHAR (50)     NOT NULL,
    PRIMARY KEY CLUSTERED ([RoleId] ASC)
);

