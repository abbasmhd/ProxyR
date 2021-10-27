CREATE FUNCTION [dbo].[Query_Users_Grid] ()

RETURNS TABLE AS RETURN
(
    SELECT u.[UserId]
          ,u.[Username]
          ,u.[Firstname]
          ,u.[Lastname]
          ,u.[Firstname] + ' ' + [Lastname] AS  [Fullname]
          ,u.[Email]
          ,u.[IsEnabled]
          ,u.[Timestamp]
  FROM [User] u
  JOIN [UserRole] ur on ur.[UserId] = u.[UserId]
  JOIN [Role] r ON r.[RoleId] = ur.[RoleId]
  WHERE u.[IsDeleted] = 0
)
