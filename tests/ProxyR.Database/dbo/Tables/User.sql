CREATE TABLE [dbo].[User] (
    [UserId]      UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [Username]    VARCHAR (230)    NOT NULL,
    [Firstname]   VARCHAR (100)    NOT NULL,
    [Lastname]    VARCHAR (100)    NOT NULL,
    [Email]       VARCHAR (250)    NOT NULL,
    [IsEnabled]   BIT              DEFAULT ((1)) NOT NULL,
    [CreatedDate] DATETIME         DEFAULT (getdate()) NOT NULL,
    [CreatedBy]   VARCHAR (230)    NOT NULL,
    [UpdatedDate] DATETIME         DEFAULT (getdate()) NOT NULL,
    [UpdatedBy]   VARCHAR (230)    NOT NULL,
    [IsDeleted]   BIT              DEFAULT ((0)) NOT NULL,
    [Timestamp]   ROWVERSION       NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    UNIQUE NONCLUSTERED ([Username] ASC)
);

