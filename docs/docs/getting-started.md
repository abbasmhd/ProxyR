---
layout: default
title: Getting Started
---

# Getting Started with ProxyR

This guide will help you get up and running with ProxyR in your .NET application.

## Prerequisites

- .NET 8.0 or later
- SQL Server (any edition)
- Visual Studio 2022 or VS Code

## Installation

1. Create a new ASP.NET Core Web API project or open an existing one
2. Install the ProxyR NuGet package:

```bash
dotnet add package Abbasmhd.ProxyR
```

Note: The package is published under the namespace `Abbasmhd.ProxyR` to follow NuGet naming conventions. This is the official package name and should be used instead of other variations.

## Basic Setup

### 1. Add Required Namespaces

In your `Startup.cs` or `Program.cs`, add the following namespace:

```csharp
using ProxyR.Middleware;
```

### 2. Configure Services

Add ProxyR to your services in one of these ways:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Method 1: Using configuration section (recommended)
    services.AddProxyR(Configuration.GetSection("ProxyR"));

    // Method 2: Using fluent configuration
    services.AddProxyR(options => options
        .UseConnectionString("your_connection_string")
        .UseDefaultSchema("ProxyR")
        .UseFunctionNamePrefix("Api_")
    );

    // Method 3: Using connection string from configuration
    services.AddProxyR(options => options
        .UseConnectionStringName("DefaultConnection")
        .UseDefaultSchema("ProxyR")
        .UseFunctionNamePrefix("Api_")
    );

    // Optional: Add OpenAPI/Swagger support
    services.AddOpenApi(options =>
    {
        options.CopyFrom(Configuration.GetSection("OpenAPI"));
        options.UseProxyR(Configuration.GetSection("ProxyR"));
    });
}
```

### 3. Configure Middleware

Add ProxyR to your application pipeline:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... other middleware ...

    app.UseProxyR();

    // Optional: Add OpenAPI/Swagger UI
    if (env.IsDevelopment())
    {
        app.UseOpenApiDocumentation();
        app.UseOpenApiUi();
    }
}
```

### 4. Configuration File

Add ProxyR settings to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourDatabase;Trusted_Connection=True;"
  },
  "ProxyR": {
    "ConnectionStringName": "DefaultConnection",
    "Prefix": "Api_",
    "Suffix": "",
    "Seperator": "_",
    "DefaultSchema": "ProxyR",
    "IncludeSchemaInPath": false,
    "ExcludedParameters": [],
    "RequiredParameterNames": []
  },
  "OpenAPI": {
    "ApiName": "Your API Name",
    "ApiDescription": "Your API Description",
    "ApiVersion": "v1",
    "DocumentName": "v1",
    "UseBearerAuthentication": false
  }
}
```

## Creating Your First API Endpoint

### 1. Create Database Objects

Following our [naming conventions](./naming-conventions.html), create your database objects:

```sql
-- Create base tables
CREATE TABLE dbo.User (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL
);

-- Create view for basic data access
CREATE VIEW ProxyR.Api_Users_View AS
SELECT Id, Username, Email
FROM dbo.User;

-- Create function for searchable grid
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
        Email
    FROM dbo.User
    WHERE
        (@SearchTerm IS NULL OR
         Username LIKE '%' + @SearchTerm + '%' OR
         Email LIKE '%' + @SearchTerm + '%')
);
```

### 2. Access Your API

Your database objects are now automatically exposed as REST endpoints:

| Endpoint | Method | Description | Example |
|----------|--------|-------------|---------|
| `/users` | GET | Get all users | `GET /users` |
| `/users/grid` | GET | Get filtered users | `GET /users/grid?searchTerm=john&sortBy=Email` |

### 3. Test with Swagger

If you've enabled OpenAPI/Swagger, visit:
```
https://your-app/swagger
```

## Naming Conventions

ProxyR uses a convention-based approach to map database objects to API endpoints:

1. **Schema**: Use the `ProxyR` schema for all API-exposed objects
2. **Prefix**: Use `Api_` prefix for all objects
3. **Resource**: Use plural nouns (Users, Roles, etc.)
4. **Operation**: Use suffixes like `_View`, `_Grid`, `_Details`

For detailed naming guidelines, see our [Naming Conventions Guide](./naming-conventions.html).

## Next Steps

- Learn about [Configuration Options](./configuration.html)
- Explore [Security Best Practices](./security.html)
- Check out [Examples](./examples.html)
- Read the [API Reference](../api/index.html)