---
layout: default
title: Configuration Guide
---

# Configuration Guide

ProxyR offers extensive configuration options to customize how your database objects are exposed as API endpoints.

## Basic Configuration

### In `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourDatabase;Trusted_Connection=True;"
  },
  "ProxyR": {
    "Prefix": "Api_",
    "Suffix": "",
    "Seperator": "_",
    "DefaultSchema": "ProxyR",
    "IncludeSchemaInPath": false,
    "ExcludedParameters": [],
    "RequiredParameterNames": []
  }
}
```

### In `Startup.cs`

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Method 1: Using configuration section
    services.AddProxyR(Configuration.GetSection("ProxyR"));

    // Method 2: Using fluent configuration
    services.AddProxyR(options => options
        .UseConnectionString("your_connection_string")
        .UseDefaultSchema("ProxyR")
        .UseFunctionNamePrefix("Api_")
        .UseSchemaInPath()
        .RequireParameter("TenantId")
    );
}
```

## Configuration Options

### Connection Settings

```csharp
services.AddProxyR(options => options
    // Method 1: Direct connection string
    .UseConnectionString("Server=...;Database=...;")

    // Method 2: Connection string name from configuration
    .UseConnectionStringName("DefaultConnection")
);
```

### Naming Options

```csharp
services.AddProxyR(options => options
    // Prefix for database objects (default: "Api_")
    .UseFunctionNamePrefix("Api_")

    // Suffix for database objects (default: "")
    .UseFunctionNameSuffix("_V1")

    // Separator for URL segments (default: "_")
    .UseFunctionNameSeperator("_")
);
```

### Schema Options

```csharp
services.AddProxyR(options => options
    // Default schema (default: "dbo")
    .UseDefaultSchema("ProxyR")

    // Include schema in URL path (default: false)
    .UseSchemaInPath()
);
```

### Security Options

```csharp
services.AddProxyR(options => options
    // Exclude sensitive parameters
    .OverrideParameter<string>("Password", _ => null)
    .OverrideParameter<int>("TenantId", context =>
        context.User.FindFirst("TenantId")?.Value)

    // Require specific parameters
    .RequireParameter("ApiKey")
    .RequireParameter("UserId")
);
```

## Advanced Configuration

### Parameter Overrides

```csharp
services.AddProxyR(options => options
    // Override with static value
    .OverrideParameter("Environment", _ => "Production")

    // Override with context-based value
    .OverrideParameter<int>("UserId", context =>
        int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value))

    // Override with service-based value
    .OverrideParameter<string>("CurrentUser", context =>
        context.RequestServices.GetService<ICurrentUser>()?.UserName)
);
```

### Custom Parameter Modifiers

```csharp
services.AddProxyR(options => options
    .OverrideParameter("Timestamp", context =>
    {
        // Custom logic to modify parameter
        var userTimeZone = context.Request.Headers["TimeZone"].ToString();
        return DateTime.UtcNow.ToOffset(TimeSpan.Parse(userTimeZone));
    })
);
```

### OpenAPI/Swagger Integration

```csharp
services.AddOpenApi(options =>
{
    options.CopyFrom(Configuration.GetSection("OpenAPI"));
    options.UseProxyR(Configuration.GetSection("ProxyR"));
});

// In Configure method:
app.UseOpenApiDocumentation();
app.UseOpenApiUi();
```

## Environment-Specific Configuration

### Development

```json
{
  "ProxyR": {
    "DefaultSchema": "ProxyR",
    "IncludeSchemaInPath": true,
    "ExcludedParameters": []
  }
}
```

### Production

```json
{
  "ProxyR": {
    "DefaultSchema": "ProxyR",
    "IncludeSchemaInPath": false,
    "ExcludedParameters": [
      "Password",
      "SecurityStamp",
      "PrivateKey"
    ],
    "RequiredParameterNames": [
      "ApiKey",
      "TenantId"
    ]
  }
}
```

## Best Practices

1. **Security**
   - Always exclude sensitive parameters
   - Use parameter overrides for security-related values
   - Implement proper authentication/authorization

2. **Performance**
   - Configure appropriate timeouts
   - Use connection pooling
   - Consider caching strategies

3. **Maintenance**
   - Use consistent naming conventions
   - Document all configuration options
   - Version your API endpoints

4. **Testing**
   - Use different configurations for different environments
   - Test parameter overrides
   - Validate security settings

## Troubleshooting

### Common Issues

1. **Connection Problems**
   ```csharp
   services.AddProxyR(options => options
       .UseConnectionString(Configuration.GetConnectionString("DefaultConnection"))
       .WithTimeout(30) // Set command timeout
   );
   ```

2. **Parameter Mapping Issues**
   ```csharp
   services.AddProxyR(options => options
       .UseFunctionNamePrefix("Api_")
       .UseDefaultSchema("ProxyR")
       .OverrideParameter("NullValue", _ => DBNull.Value)
   );
   ```

3. **Schema Resolution**
   ```csharp
   services.AddProxyR(options => options
       .UseDefaultSchema("ProxyR")
       .UseSchemaInPath() // Enable schema in URL
   );
   ```

## Next Steps

- Learn about [Query Parameters](./query-parameters.html)
- Explore [Security Best Practices](./security.html)
- Check out [Examples](./examples.html)