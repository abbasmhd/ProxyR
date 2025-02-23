---
layout: default
title: Naming Conventions
---

# Naming Conventions and Database Structure

ProxyR follows a specific naming convention to automatically map your database objects to REST API endpoints. This guide will help you structure your database objects for optimal use with ProxyR.

## Schema Structure

We recommend organizing your database objects in the following way:

```sql
CREATE SCHEMA [ProxyR] AUTHORIZATION [dbo]
```

## Naming Patterns

### Views
Views should follow this naming pattern:
```
ProxyR.Api_[Resource]_View
```

Example:
```sql
CREATE VIEW ProxyR.Api_Users_View AS
SELECT Id, Username, Email
FROM dbo.User
```

### Table-Valued Functions
Functions should follow this naming pattern:
```
ProxyR.Api_[Resource]_[Operation]
```

Example:
```sql
CREATE FUNCTION ProxyR.Api_Users_Grid
(
    @SearchTerm NVARCHAR(50) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT Id, Username, Email
    FROM dbo.User
    WHERE (@SearchTerm IS NULL OR Username LIKE '%' + @SearchTerm + '%')
)
```

## Example Database Structure

Here's a complete example of how your database objects should be organized:

```
Database
├── Tables
│   ├── dbo.User
│   ├── dbo.Role
│   └── dbo.UserRole
├── Views
│   ├── ProxyR.Api_Users_View
│   └── ProxyR.Api_Roles_View
└── Functions
    └── Table-valued Functions
        ├── ProxyR.Api_Users_Grid
        └── ProxyR.Api_Roles_Grid
```

## URL Mapping

The naming convention automatically maps to REST endpoints as follows:

| Database Object | HTTP Method | URL Endpoint | Description |
|----------------|-------------|--------------|-------------|
| `ProxyR.Api_Users_View` | GET | `/users` | Get all users |
| `ProxyR.Api_Users_Grid` | GET | `/users/grid` | Get filtered user grid |
| `ProxyR.Api_Roles_View` | GET | `/roles` | Get all roles |
| `ProxyR.Api_Roles_Grid` | GET | `/roles/grid` | Get filtered roles grid |

## Configuration

In your `appsettings.json`, configure ProxyR to use these conventions:

```json
{
  "ProxyR": {
    "Prefix": "Api_",
    "Suffix": "",
    "Seperator": "_",
    "DefaultSchema": "ProxyR",
    "IncludeSchemaInPath": false
  }
}
```

## Best Practices

1. **Schema Separation**: Keep your API-exposed objects in a separate schema (ProxyR) for better organization and security
2. **Consistent Naming**: Always use the `Api_` prefix for objects that should be exposed as endpoints
3. **Resource Names**: Use plural nouns for resource names (Users, Roles, etc.)
4. **Operation Types**: Common operation suffixes:
   - `_View` for basic views
   - `_Grid` for searchable/filterable results
   - `_Details` for detailed information
   - `_List` for simple lists
   - `_Search` for complex search operations

## Security Considerations

1. Create a dedicated database user for the API with appropriate permissions:
```sql
CREATE USER [ProxyRUser] WITH PASSWORD = 'your_secure_password'
GRANT EXECUTE ON SCHEMA::[ProxyR] TO [ProxyRUser]
```

2. Only grant access to the ProxyR schema:
```sql
DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProxyRUser]
```

## Example Implementation

Here's a complete example of creating an API endpoint:

```sql
-- Create the view for basic data
CREATE VIEW ProxyR.Api_Users_View AS
SELECT Id, Username, Email, CreatedDate
FROM dbo.User;

-- Create the function for searchable grid
CREATE FUNCTION ProxyR.Api_Users_Grid
(
    @SearchTerm NVARCHAR(50) = NULL,
    @SortBy NVARCHAR(50) = 'Username',
    @SortDirection NVARCHAR(4) = 'ASC'
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        Id,
        Username,
        Email,
        CreatedDate
    FROM dbo.User
    WHERE
        (@SearchTerm IS NULL OR
         Username LIKE '%' + @SearchTerm + '%' OR
         Email LIKE '%' + @SearchTerm + '%')
);
```

These objects will automatically be exposed as:
- `GET /users` - Returns all users
- `GET /users/grid?searchTerm=john&sortBy=Email&sortDirection=DESC` - Returns filtered and sorted users