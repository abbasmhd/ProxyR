CREATE VIEW [ProxyR].[Api_Users_View]
AS
SELECT u.UserId
     , u.Username
     , u.Firstname + ' ' + u.Lastname AS Fullname
     , r.Name AS RoleName
     , u.Email
  FROM dbo.[User] AS u
  JOIN dbo.UserRole AS ur ON ur.UserId = u.UserId
  JOIN dbo.Role AS r      ON r.RoleId = ur.RoleId
 WHERE u.IsDeleted = 0


GO
GRANT SELECT
    ON OBJECT::[ProxyR].[Api_Users_View] TO [ProxyR]
    AS [dbo];
