---
layout: default
title: Home
---

# ProxyR - SQL Server to REST API Middleware

[![.NET](https://github.com/abbasmhd/ProxyR/actions/workflows/dotnet.yml/badge.svg)](https://github.com/abbasmhd/ProxyR/actions/workflows/dotnet.yml)
[![GitHub Pages](https://github.com/abbasmhd/ProxyR/actions/workflows/pages.yml/badge.svg)](https://github.com/abbasmhd/ProxyR/actions/workflows/pages.yml)

ProxyR is a powerful .NET middleware that automatically exposes SQL Server table-valued functions, inline table-valued functions, and views as REST API endpoints. It simplifies the process of creating APIs from your database functions and views with minimal configuration.

## Why ProxyR?

- **Zero-Code API Creation**: Turn your SQL Server functions into REST endpoints without writing any additional code
- **Flexible Configuration**: Customize your API endpoints with prefixes, suffixes, and routing options
- **Built-in Documentation**: Automatic OpenAPI/Swagger documentation generation
- **Security First**: Built-in parameter exclusion and modification capabilities
- **Database-Agnostic**: Works with any SQL Server database
- **Convention-Based**: Simple and consistent naming conventions for automatic API mapping

## Quick Start

### 1. Install the Package

```bash
dotnet add package ProxyR.Middleware
```

### 2. Configure Your Services

Add ProxyR to your service collection in `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddProxyR(options => options
        .UseConnectionString("your_connection_string")
        .UseDefaultSchema("ProxyR")
        .UseFunctionNamePrefix("Api_")
    );
}
```

### 3. Create Your Database Objects

Follow our [naming conventions](./docs/naming-conventions.html) to automatically expose your database objects as API endpoints:

```sql
-- Create a view for basic data
CREATE VIEW ProxyR.Api_Users_View AS
SELECT Id, Username, Email
FROM dbo.User;

-- Create a function for searchable grid
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
);
```

These will automatically be exposed as:
- `GET /users` - Returns all users
- `GET /users/grid?searchTerm=john` - Returns filtered users

### 4. Enable the Middleware

Add ProxyR to your application pipeline:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseProxyR();
}
```

## Documentation

Check out our comprehensive documentation:

- [Getting Started Guide](./docs/getting-started.html) - Quick setup and basic usage
- [Database Setup](./docs/database-setup.html) - How to structure your database
- [Naming Conventions](./docs/naming-conventions.html) - Recommended naming patterns
- [Configuration Guide](./docs/configuration.html) - Detailed configuration options
- [Query Parameters](./docs/query-parameters.html) - Working with parameters and filters
- [Security Guide](./docs/security.html) - Security best practices and considerations
- [Examples](./docs/examples.html) - Common use cases and examples
- [Troubleshooting](./docs/troubleshooting.html) - Common issues and solutions
- [API Reference](./docs/api-reference.html) - Complete API documentation

## Community

- [GitHub Repository](https://github.com/abbasmhd/ProxyR)
- [Issue Tracker](https://github.com/abbasmhd/ProxyR/issues)
- [Contributing Guidelines](./docs/contributing.html)

## License

ProxyR is licensed under the MIT License. See the [LICENSE](https://github.com/abbasmhd/ProxyR/blob/main/LICENSE) file for details.
