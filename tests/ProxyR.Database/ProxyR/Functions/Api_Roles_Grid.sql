CREATE FUNCTION [ProxyR].[Api_Roles_Grid]()
RETURNS TABLE AS RETURN 
(
    SELECT * from [Role]
)

GO
GRANT SELECT
    ON OBJECT::[ProxyR].[Api_Roles_Grid] TO [ProxyR]
    AS [dbo];
