---
layout: default
title: Troubleshooting
---

# Troubleshooting Guide

This guide helps you diagnose and resolve common issues when working with ProxyR.

## Common Issues

### 1. Endpoint Not Found

#### Symptoms
- 404 Not Found response
- Endpoint doesn't appear in Swagger

#### Possible Causes
1. **Incorrect Naming Convention**
   ```sql
   -- ❌ Wrong
   CREATE FUNCTION dbo.GetUsers -- Missing schema and prefix

   -- ✅ Correct
   CREATE FUNCTION ProxyR.Api_Users_Grid
   ```

2. **Wrong Schema**
   ```json
   {
     "ProxyR": {
       "DefaultSchema": "dbo",  // ❌ Wrong
       "DefaultSchema": "ProxyR"  // ✅ Correct
     }
   }
   ```

3. **Permission Issues**
   ```sql
   -- Check permissions
   SELECT
       dp.state_desc AS PermissionType,
       dp.permission_name AS Permission,
       OBJECT_SCHEMA_NAME(major_id) AS SchemaName,
       o.name AS ObjectName
   FROM sys.database_permissions dp
   JOIN sys.objects o ON dp.major_id = o.object_id
   WHERE dp.grantee_principal_id = USER_ID('ProxyRUser');

   -- Fix permissions
   GRANT EXECUTE ON SCHEMA::[ProxyR] TO [ProxyRUser];
   ```

### 2. Parameter Mapping Issues

#### Symptoms
- Parameters not being passed correctly
- Null values when values are provided

#### Solutions

1. **Case Sensitivity**
   ```sql
   -- ❌ Wrong
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @searchTerm NVARCHAR(50),  -- Camel case
       @Status BIT                -- Pascal case
   )

   -- ✅ Correct
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @SearchTerm NVARCHAR(50),  -- Consistent Pascal case
       @Status BIT
   )
   ```

2. **Parameter Type Mismatch**
   ```sql
   -- ❌ Wrong: No type conversion
   WHERE Price = @Price  -- @Price comes as string

   -- ✅ Correct: Handle type conversion
   WHERE Price = TRY_CAST(@Price AS DECIMAL(18,2))
   ```

3. **Null Handling**
   ```sql
   -- ❌ Wrong: No null handling
   WHERE Status = @Status

   -- ✅ Correct: Handle nulls
   WHERE (@Status IS NULL OR Status = @Status)
   ```

### 3. Performance Issues

#### Symptoms
- Slow response times
- Timeouts
- High CPU usage

#### Solutions

1. **Index Missing**
   ```sql
   -- Check missing indexes
   SELECT
       OBJECT_SCHEMA_NAME(mid.object_id) AS SchemaName,
       OBJECT_NAME(mid.object_id) AS TableName,
       migs.avg_user_impact,
       mid.equality_columns,
       mid.inequality_columns,
       mid.included_columns
   FROM sys.dm_db_missing_index_details mid
   JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
   JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
   ORDER BY migs.avg_user_impact DESC;

   -- Create needed indexes
   CREATE INDEX IX_User_Username ON dbo.User(Username)
   INCLUDE (Email, CreatedDate);
   ```

2. **Parameter Sniffing**
   ```sql
   -- ❌ Wrong: Susceptible to parameter sniffing
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @SearchTerm NVARCHAR(50)
   )

   -- ✅ Correct: Use OPTION (RECOMPILE) or local variables
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @SearchTerm NVARCHAR(50)
   )
   RETURNS TABLE
   AS
   RETURN
   (
       DECLARE @LocalSearch NVARCHAR(50) = @SearchTerm;

       SELECT *
       FROM dbo.User
       WHERE @LocalSearch IS NULL
          OR Username LIKE '%' + @LocalSearch + '%'
       OPTION (RECOMPILE)
   );
   ```

3. **Large Result Sets**
   ```sql
   -- ❌ Wrong: No pagination
   SELECT * FROM dbo.User;

   -- ✅ Correct: Use pagination
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @PageNumber INT = 1,
       @PageSize INT = 50
   )
   RETURNS TABLE
   AS
   RETURN
   (
       SELECT *
       FROM dbo.User
       ORDER BY Id
       OFFSET (@PageNumber - 1) * @PageSize ROWS
       FETCH NEXT @PageSize ROWS ONLY
   );
   ```

### 4. Security Issues

#### Symptoms
- Unauthorized access
- Exposed sensitive data
- SQL injection vulnerabilities

#### Solutions

1. **SQL Injection Prevention**
   ```sql
   -- ❌ Wrong: String concatenation
   'SELECT * FROM User WHERE Username LIKE ''%' + @SearchTerm + '%'''

   -- ✅ Correct: Parameterized query
   CREATE FUNCTION ProxyR.Api_Users_Search
   (
       @SearchTerm NVARCHAR(50)
   )
   RETURNS TABLE
   AS
   BEGIN
       SET @SearchTerm = REPLACE(@SearchTerm, '%', '[%]');
       RETURN (
           SELECT * FROM User
           WHERE Username LIKE '%' + @SearchTerm + '%' ESCAPE '['
       );
   END;
   ```

2. **Row-Level Security**
   ```sql
   -- Create security policy
   CREATE SECURITY POLICY TenantFilter
   ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId)
   ON dbo.User;

   -- Override tenant parameter
   services.AddProxyR(options => options
       .OverrideParameter<int>("TenantId",
           context => GetUserTenant(context))
   );
   ```

### 5. Configuration Issues

#### Symptoms
- Middleware not working
- Wrong endpoint mapping
- Authentication failures

#### Solutions

1. **Middleware Order**
   ```csharp
   // ❌ Wrong order
   app.UseProxyR();
   app.UseAuthentication();

   // ✅ Correct order
   app.UseAuthentication();
   app.UseAuthorization();
   app.UseProxyR();
   ```

2. **Connection String**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=server;Database=db;User Id=user;Password=pass;TrustServerCertificate=True;"
     },
     "ProxyR": {
       "ConnectionStringName": "DefaultConnection"
     }
   }
   ```

## Debugging Tips

### 1. Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ProxyR": "Debug"
    }
  }
}
```

### 2. SQL Profiler Queries

```sql
-- Track function execution
SELECT
    OBJECT_NAME(qt.objectid) AS FunctionName,
    qs.execution_count,
    qs.total_worker_time / 1000000.0 AS TotalCPU_Seconds,
    qs.total_elapsed_time / 1000000.0 AS TotalDuration_Seconds,
    qs.total_logical_reads AS TotalLogicalReads,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE OBJECT_NAME(qt.objectid) LIKE 'Api_%'
ORDER BY qs.last_execution_time DESC;
```

### 3. Check Object Status

```sql
SELECT
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS ObjectName,
    type_desc AS ObjectType,
    create_date,
    modify_date,
    is_disabled
FROM sys.objects
WHERE OBJECT_SCHEMA_NAME(object_id) = 'ProxyR'
ORDER BY type_desc, name;
```

## Best Practices

1. **Always Test First**
   ```sql
   -- Test function directly
   SELECT * FROM ProxyR.Api_Users_Grid
   WHERE @SearchTerm = 'test';

   -- Check execution plan
   SET STATISTICS IO ON;
   SET STATISTICS TIME ON;
   ```

2. **Use Error Handling**
   ```sql
   CREATE FUNCTION ProxyR.Api_Users_Grid
   (
       @SearchTerm NVARCHAR(50)
   )
   RETURNS TABLE
   AS
   RETURN
   (
       SELECT *
       FROM dbo.User
       WHERE TRY_CAST(@SearchTerm AS NVARCHAR(50)) IS NOT NULL
         AND Username LIKE '%' + @SearchTerm + '%'
   );
   ```

3. **Monitor Performance**
   ```sql
   -- Create performance baseline
   SELECT
       OBJECT_NAME(qt.objectid) AS FunctionName,
       MAX(qs.total_elapsed_time) AS MaxDuration,
       AVG(qs.total_elapsed_time) AS AvgDuration,
       MIN(qs.total_elapsed_time) AS MinDuration
   FROM sys.dm_exec_query_stats qs
   CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
   WHERE OBJECT_SCHEMA_NAME(qt.objectid) = 'ProxyR'
   GROUP BY OBJECT_NAME(qt.objectid);
   ```

## Next Steps

- Review [Configuration Options](./configuration.html)
- Learn about [Security Best Practices](./security.html)
- Check out [Examples](./examples.html)