CREATE FUNCTION [ProxyR].[Api_Users_Grid]()
RETURNS TABLE AS RETURN
(
    SELECT u.[UserId]
         , u.[Username]
         , u.[Firstname]
         , u.[Lastname]
         , u.[Firstname] + ' ' + [Lastname] AS  [Fullname]
         , u.[Email]
         , u.[IsEnabled]
         , u.[Timestamp]
         , r.[Name]     AS [RoleName]
      FROM [dbo].[User] u
      JOIN [dbo].[UserRole] ur on ur.[UserId] = u.[UserId]
      JOIN [dbo].[Role] r ON r.[RoleId] = ur.[RoleId]
     WHERE u.[IsDeleted] = 0
)

GO
GRANT SELECT
    ON OBJECT::[ProxyR].[Api_Users_Grid] TO [ProxyR]
    AS [dbo];
