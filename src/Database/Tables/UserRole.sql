CREATE TABLE [dbo].[UserRole]
(
    [UserId] UNIQUEIDENTIFIER NOT NULL , 
    [RoleId] UNIQUEIDENTIFIER NOT NULL, 
    PRIMARY KEY ([UserId], [RoleId]), 
    CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [Role]([RoleId]), 
    CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [User]([UserId])
)
