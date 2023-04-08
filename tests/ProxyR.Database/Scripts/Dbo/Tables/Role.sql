CREATE TABLE [dbo].[Role] (
    [RoleId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) ,
    [Name]        VARCHAR (50)     NOT NULL,
    [IsEnabled]   BIT              NOT NULL DEFAULT ((1)) ,
    [CreatedDate] DATETIME         NOT NULL DEFAULT (GETDATE()) ,
    [CreatedBy]   VARCHAR (230)    NOT NULL,
    [UpdatedDate] DATETIME         NOT NULL DEFAULT (GETDATE()) ,
    [UpdatedBy]   VARCHAR (230)    NOT NULL,
    [IsDeleted]   BIT              NOT NULL DEFAULT ((0)) ,
    PRIMARY KEY CLUSTERED ([RoleId] ASC)
);

