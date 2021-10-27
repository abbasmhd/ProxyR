CREATE TABLE [dbo].[User]
(
    [UserId]                    UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [Username]                  VARCHAR(230)        NOT NULL UNIQUE,
    [Firstname]                 NVARCHAR(100)       NOT NULL,
    [Lastname]                  NVARCHAR(100)       NOT NULL,
    [Email]                     VARCHAR(250)        NOT NULL,
    [IsEnabled]                 BIT                 NOT NULL DEFAULT 1,
    [CreatedDate]               DATETIME            NOT NULL DEFAULT GETDATE(),
    [CreatedBy]                 VARCHAR(230)        NOT NULL,
    [UpdatedDate]               DATETIME            NOT NULL DEFAULT GETDATE(),
    [UpdatedBy]                 VARCHAR(230)        NOT NULL,
    [IsDeleted]                 BIT                 NOT NULL DEFAULT 0,
    [Timestamp]                 TIMESTAMP           NOT NULL 
)
