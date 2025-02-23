---
layout: default
title: Database Setup
---

# Database Setup Guide

This guide explains how to structure your database for optimal use with ProxyR.

## Schema Setup

1. Create a dedicated schema for your API endpoints:

```sql
CREATE SCHEMA [ProxyR] AUTHORIZATION [dbo]
```

2. Create a dedicated database user for the API:

```sql
CREATE USER [ProxyRUser] WITH PASSWORD = 'your_secure_password'
GRANT EXECUTE ON SCHEMA::[ProxyR] TO [ProxyRUser]
```

## Basic Structure

Your database should follow this basic structure:

```
Database
├── Tables (dbo schema)
│   ├── Core business tables
│   └── Relationship tables
└── ProxyR Schema
    ├── Views (for basic data access)
    └── Functions (for filtered/complex queries)
```

## Example Setup

Here's a complete example of setting up a basic user management system:

```sql
-- Create base tables in dbo schema
CREATE TABLE dbo.User (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.Role (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL
);

CREATE TABLE dbo.UserRole (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    AssignedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UserRole PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES dbo.User(Id),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(Id)
);

-- Create API views in ProxyR schema
CREATE VIEW ProxyR.Api_Users_View AS
SELECT
    u.Id,
    u.Username,
    u.Email,
    u.CreatedDate,
    STRING_AGG(r.Name, ', ') AS Roles
FROM dbo.User u
LEFT JOIN dbo.UserRole ur ON u.Id = ur.UserId
LEFT JOIN dbo.Role r ON ur.RoleId = r.Id
GROUP BY u.Id, u.Username, u.Email, u.CreatedDate;

CREATE VIEW ProxyR.Api_Roles_View AS
SELECT
    r.Id,
    r.Name,
    r.Description,
    COUNT(ur.UserId) AS UserCount
FROM dbo.Role r
LEFT JOIN dbo.UserRole ur ON r.Id = ur.RoleId
GROUP BY r.Id, r.Name, r.Description;

-- Create API functions in ProxyR schema
CREATE FUNCTION ProxyR.Api_Users_Grid
(
    @SearchTerm NVARCHAR(50) = NULL,
    @RoleId INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT DISTINCT
        u.Id,
        u.Username,
        u.Email,
        u.CreatedDate,
        STRING_AGG(r.Name, ', ') WITHIN GROUP (ORDER BY r.Name) AS Roles
    FROM dbo.User u
    LEFT JOIN dbo.UserRole ur ON u.Id = ur.UserId
    LEFT JOIN dbo.Role r ON ur.RoleId = r.Id
    WHERE
        (@SearchTerm IS NULL OR
         u.Username LIKE '%' + @SearchTerm + '%' OR
         u.Email LIKE '%' + @SearchTerm + '%')
        AND
        (@RoleId IS NULL OR EXISTS (
            SELECT 1 FROM dbo.UserRole ur2
            WHERE ur2.UserId = u.Id AND ur2.RoleId = @RoleId
        ))
    GROUP BY u.Id, u.Username, u.Email, u.CreatedDate
);

CREATE FUNCTION ProxyR.Api_Roles_Grid
(
    @SearchTerm NVARCHAR(50) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        r.Id,
        r.Name,
        r.Description,
        COUNT(ur.UserId) AS UserCount
    FROM dbo.Role r
    LEFT JOIN dbo.UserRole ur ON r.Id = ur.RoleId
    WHERE
        @SearchTerm IS NULL OR
        r.Name LIKE '%' + @SearchTerm + '%' OR
        r.Description LIKE '%' + @SearchTerm + '%'
    GROUP BY r.Id, r.Name, r.Description
);
```

## Best Practices

1. **Schema Separation**
   - Keep business logic in the `dbo` schema
   - Keep API endpoints in the `ProxyR` schema
   - Use views for simple queries
   - Use functions for complex queries with parameters

2. **Performance**
   - Create appropriate indexes on frequently queried columns
   - Use filtered indexes for common parameter values
   - Consider materialized views for complex aggregations

3. **Security**
   - Use schema-level security
   - Create dedicated API users
   - Implement row-level security if needed
   - Never expose sensitive columns directly

4. **Maintenance**
   - Document all objects
   - Use consistent naming
   - Keep functions focused and simple
   - Consider versioning for major changes

## Next Steps

- Learn about [Naming Conventions](./naming-conventions.html)
- Explore [Configuration Options](./configuration.html)
- Check out [Security Best Practices](./security.html)