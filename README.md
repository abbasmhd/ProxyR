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
dotnet add package ProxyR.Middleware
```

### Basic Configuration

In your `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddProxyR(options => options
        .UseConnectionString("your_connection_string")
        .UseDefaultSchema("dbo")
        .UseFunctionNamePrefix("Api_")
    );
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseProxyR();
}
```

### Configuration Options

In your `appsettings.json`:

```json
{
  "ProxyR": {
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
