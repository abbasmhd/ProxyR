---
layout: default
title: Query Parameters
---

# Working with Query Parameters

ProxyR provides flexible ways to handle query parameters in your API endpoints. This guide explains how to work with parameters in your database functions and how they map to API requests.

## Parameter Types

ProxyR supports several ways to pass parameters to your endpoints:

1. **Query String Parameters**
   - Automatically mapped from URL query string
   - Case-insensitive matching
   - Support for multiple values

2. **Request Body Parameters**
   - Sent as JSON in POST requests
   - Nested object support
   - Array parameter support

3. **OData-Style Parameters**
   - Built-in support for `$filter`, `$orderby`, `$top`, `$skip`
   - Complex filtering expressions
   - Standard OData syntax

## Query String Parameters

### Basic Usage

```sql
CREATE FUNCTION ProxyR.Api_Users_Grid
(
    @SearchTerm NVARCHAR(50) = NULL,
    @Status BIT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT Id, Username, Email
    FROM dbo.User
    WHERE (@SearchTerm IS NULL OR Username LIKE '%' + @SearchTerm + '%')
      AND (@Status IS NULL OR IsActive = @Status)
);
```

Access via:
```
GET /users/grid?searchTerm=john&status=true
```

### Optional Parameters

All parameters with default values are optional:

```sql
CREATE FUNCTION ProxyR.Api_Products_Grid
(
    @CategoryId INT = NULL,           -- Optional category filter
    @MinPrice DECIMAL(18,2) = 0,      -- Optional minimum price
    @MaxPrice DECIMAL(18,2) = NULL,   -- Optional maximum price
    @InStock BIT = NULL               -- Optional stock status
)
RETURNS TABLE
AS
RETURN
(
    SELECT *
    FROM dbo.Product
    WHERE (@CategoryId IS NULL OR CategoryId = @CategoryId)
      AND (Price >= @MinPrice)
      AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
      AND (@InStock IS NULL OR IsInStock = @InStock)
);
```

## Complex Parameters

### Array Parameters

Handle multiple values using table-valued parameters or delimited strings:

```sql
CREATE TYPE dbo.IntList AS TABLE
(
    Value INT
);

CREATE FUNCTION ProxyR.Api_Products_ByCategories
(
    @Categories dbo.IntList READONLY
)
RETURNS TABLE
AS
RETURN
(
    SELECT p.*
    FROM dbo.Product p
    INNER JOIN @Categories c ON p.CategoryId = c.Value
);
```

### JSON Parameters

Work with complex JSON data:

```sql
CREATE FUNCTION ProxyR.Api_Orders_Search
(
    @Filter NVARCHAR(MAX)  -- JSON filter object
)
RETURNS TABLE
AS
RETURN
(
    SELECT o.*
    FROM dbo.Order o
    CROSS APPLY OPENJSON(@Filter) WITH (
        StartDate DATE '$.dateRange.start',
        EndDate DATE '$.dateRange.end',
        StatusList NVARCHAR(MAX) '$.statuses' AS JSON
    ) f
    WHERE (f.StartDate IS NULL OR o.OrderDate >= f.StartDate)
      AND (f.EndDate IS NULL OR o.OrderDate <= f.EndDate)
      AND (
          f.StatusList IS NULL OR
          o.Status IN (
              SELECT value
              FROM OPENJSON(f.StatusList)
              WITH (value NVARCHAR(50) '$')
          )
      )
);
```

## OData Support

ProxyR automatically handles OData query parameters:

| Parameter | Description | Example |
|-----------|-------------|---------|
| `$filter` | Filter records | `$filter=age gt 18` |
| `$orderby` | Sort records | `$orderby=name desc` |
| `$top` | Limit results | `$top=10` |
| `$skip` | Skip records | `$skip=20` |

Example function supporting OData:

```sql
CREATE FUNCTION ProxyR.Api_Users_List
(
    @Filter NVARCHAR(MAX) = NULL,
    @OrderBy NVARCHAR(MAX) = NULL,
    @Skip INT = 0,
    @Take INT = 100
)
RETURNS TABLE
AS
RETURN
(
    SELECT Id, Username, Email, CreatedDate
    FROM dbo.User
    WHERE @Filter IS NULL OR Id IN (
        -- Your filter logic here
    )
    ORDER BY
        CASE WHEN @OrderBy = 'username' THEN Username END,
        CASE WHEN @OrderBy = 'email' THEN Email END,
        CASE WHEN @OrderBy = 'created' THEN CreatedDate END
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY
);
```

## Best Practices

1. **Parameter Naming**
   - Use clear, descriptive names
   - Follow consistent casing (camelCase recommended)
   - Prefix boolean parameters with verbs (is, has, should)

2. **Default Values**
   - Always provide sensible defaults
   - Use NULL for optional filters
   - Consider business requirements for defaults

3. **Validation**
   - Validate parameter ranges
   - Handle NULL values gracefully
   - Provide clear error messages

4. **Performance**
   - Index filtered columns
   - Use appropriate parameter types
   - Consider parameter sniffing issues

## Security Considerations

1. **Input Validation**
   ```sql
   CREATE FUNCTION ProxyR.Api_Users_Search
   (
       @SearchTerm NVARCHAR(50)
   )
   RETURNS TABLE
   AS
   BEGIN
       -- Sanitize input
       SET @SearchTerm = REPLACE(@SearchTerm, '%', '[%]');
       SET @SearchTerm = REPLACE(@SearchTerm, '_', '[_]');

       RETURN (
           SELECT *
           FROM dbo.User
           WHERE Username LIKE '%' + @SearchTerm + '%'
       );
   END;
   ```

2. **Parameter Restrictions**
   ```json
   {
     "ProxyR": {
       "ExcludedParameters": ["Password", "Salt", "SecurityStamp"],
       "RequiredParameterNames": ["TenantId"]
     }
   }
   ```

## Next Steps

- Learn about [Security Best Practices](./security.html)
- Explore [Configuration Options](./configuration.html)
- Check out [Examples](./examples.html)