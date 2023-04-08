CREATE FUNCTION [ProxyR].[Api_Roles_Grid]()
RETURNS TABLE AS RETURN 
(
    SELECT * from [Role]
)
