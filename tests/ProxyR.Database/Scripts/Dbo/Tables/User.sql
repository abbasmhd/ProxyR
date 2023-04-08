CREATE TABLE [dbo].[User] (
    [UserId]      UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) ,
    [Username]    VARCHAR (230)    NOT NULL,
    [Firstname]   VARCHAR (100)    NOT NULL,
    [Lastname]    VARCHAR (100)    NOT NULL,
    [Email]       VARCHAR (250)    NOT NULL,
    [IsEnabled]   BIT              NOT NULL DEFAULT ((1)) ,
    [CreatedDate] DATETIME         NOT NULL DEFAULT (GETDATE()) ,
    [CreatedBy]   VARCHAR (230)    NOT NULL,
    [UpdatedDate] DATETIME         NOT NULL DEFAULT (GETDATE()) ,
    [UpdatedBy]   VARCHAR (230)    NOT NULL,
    [IsDeleted]   BIT              NOT NULL DEFAULT ((0)) ,
    [Timestamp]   ROWVERSION       NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    UNIQUE NONCLUSTERED ([Username] ASC)
);

