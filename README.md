# ProxyR

[![.NET](https://github.com/abbasmhd/ProxyR/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/abbasmhd/ProxyR/actions/workflows/dotnet.yml)

A powerful .NET middleware that automatically exposes SQL Server table-valued functions, inline table-valued functions, and views as REST API endpoints.

## Features

- **Automatic API Generation**: Automatically creates REST endpoints from SQL Server functions and views
- **Schema Support**: Flexible schema handling with options for schema inclusion in paths
- **Customizable Routing**: Configurable prefix, suffix, and separator options for endpoint URLs
- **Parameter Handling**: Support for required parameters and parameter overrides
- **OpenAPI Integration**: Built-in Swagger/OpenAPI documentation
- **Security**: Parameter exclusion and modification capabilities for enhanced security
- **Database Connection**: Flexible connection string configuration

## Getting Started

### Installation

Add the ProxyR middleware to your ASP.NET Core project:

```bash
# Install latest pre-release version
dotnet add package Abbasmhd.ProxyR --prerelease

# Or install specific version
dotnet add package Abbasmhd.ProxyR --version 0.0.1-alpha
```

Note: The package is published under the namespace `Abbasmhd.ProxyR` to follow NuGet naming conventions. This is currently a pre-release version (0.0.1-alpha) and the API may change without notice.

### Basic Configuration

In your `Startup.cs` or `Program.cs`:

```csharp
using ProxyR.Middleware;  // Add this using statement

public void ConfigureServices(IServiceCollection services)
{
    // Method 1: Using configuration section
    services.AddProxyR(Configuration.GetSection("ProxyR"));

    // Method 2: Using fluent configuration
    services.AddProxyR(options => options
        .UseConnectionString("your_connection_string")
        .UseDefaultSchema("dbo")
        .UseFunctionNamePrefix("Api_")
    );
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Add ProxyR middleware to the pipeline
    app.UseProxyR();
}
```

### Configuration Options

In your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourDatabase;Trusted_Connection=True;"
  },
  "ProxyR": {
    "ConnectionStringName": "DefaultConnection",  // Reference connection string by name
    "Prefix": "Api_",
    "Suffix": "",
    "Seperator": "_",
    "DefaultSchema": "dbo",
    "IncludeSchemaInPath": false,
    "ExcludedParameters": [],
    "RequiredParameterNames": []
  }
}
```

## OpenAPI/Swagger Integration

ProxyR includes built-in support for OpenAPI documentation. Configure it in your `Startup.cs`:

```csharp
services.AddOpenApi(options =>
{
    options.CopyFrom(Configuration.GetSection("OpenAPI"));
    options.UseProxyR(Configuration.GetSection("ProxyR"), connectionString);
});

// In Configure method:
if (env.IsDevelopment())
{
    app.UseOpenApiDocumentation();
    app.UseOpenApiUi();
}
```

## Features in Detail

### Function Mapping

- Table-valued functions (TVF)
- Inline table-valued functions (iTVF)
- Views
- Automatic parameter mapping
- Query string support
- Request body parameter support

### Security Features

- Parameter exclusion
- Parameter override capabilities
- Connection string configuration options
- Schema-based access control

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and feature requests, please use the GitHub issues page.
