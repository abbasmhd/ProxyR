---
layout: default
title: Security Guide
---

# Security Best Practices

This guide covers security best practices when using ProxyR in your application.

## Database Security

### 1. Schema Isolation

Keep your API endpoints isolated in a dedicated schema:

```sql
-- Create dedicated schema
CREATE SCHEMA [ProxyR] AUTHORIZATION [dbo]

-- Create dedicated user with limited permissions
CREATE USER [ProxyRUser] WITH PASSWORD = 'your_secure_password'

-- Grant execute permission only on ProxyR schema
GRANT EXECUTE ON SCHEMA::[ProxyR] TO [ProxyRUser]

-- Deny direct table access
DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProxyRUser]
```

### 2. Row-Level Security

Implement row-level security using session context:

```sql
-- Enable row level security
ALTER TABLE dbo.User
ADD TenantId INT NOT NULL;

CREATE SECURITY POLICY TenantIsolation
ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.User;

-- Create filtered view
CREATE VIEW ProxyR.Api_Users_View AS
SELECT Id, Username, Email
FROM dbo.User
WHERE TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT);
```

## Parameter Security

### 1. Exclude Sensitive Parameters

```csharp
services.AddProxyR(options => options
    .UseDefaultSchema("ProxyR")
    // Exclude sensitive parameters
    .OverrideParameter<string>("Password", _ => null)
    .OverrideParameter<string>("SecurityStamp", _ => null)
    .OverrideParameter<string>("PrivateKey", _ => null)
);
```

### 2. Required Parameters

```csharp
services.AddProxyR(options => options
    // Require security-related parameters
    .RequireParameter("TenantId")
    .RequireParameter("ApiKey")
    // Override with secure values
    .OverrideParameter<int>("TenantId", context =>
        int.Parse(context.User.FindFirst("TenantId")?.Value))
);
```

### 3. Input Validation

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
    SET @SearchTerm = REPLACE(@SearchTerm, '[', '[[]');
    SET @SearchTerm = REPLACE(@SearchTerm, ']', '[]]');

    RETURN (
        SELECT Id, Username, Email
        FROM dbo.User
        WHERE Username LIKE '%' + @SearchTerm + '%' ESCAPE '['
    );
END;
```

## Authentication & Authorization

### 1. JWT Authentication

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Configuration["Jwt:Issuer"],
                ValidAudience = Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
            };
        });

    services.AddProxyR(options => options
        .OverrideParameter<string>("UserId", context =>
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
    );
}

public void Configure(IApplicationBuilder app)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseProxyR();
}
```

### 2. Role-Based Access

```csharp
services.AddProxyR(options => options
    .OverrideParameter<bool>("IsAdmin", context =>
        context.User.IsInRole("Administrator"))
);

// In your SQL function
CREATE FUNCTION ProxyR.Api_Users_AdminView
(
    @IsAdmin BIT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        Id,
        Username,
        Email,
        CASE WHEN @IsAdmin = 1
            THEN SecurityStamp
            ELSE NULL
        END AS SecurityStamp
    FROM dbo.User
);
```

## HTTPS and TLS

### 1. Enforce HTTPS

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseHttpsRedirection();
    app.UseHsts();

    app.UseProxyR();
}
```

### 2. Secure Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=server;Database=db;User Id=user;Password=pass;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

## API Security

### 1. Rate Limiting

```csharp
services.AddProxyR(options => options
    .UseRateLimiting(new RateLimitOptions
    {
        PermitLimit = 100,
        Window = TimeSpan.FromMinutes(1)
    })
);
```

### 2. Request Validation

```csharp
services.AddProxyR(options => options
    .UseRequestValidation(validation =>
    {
        validation.MaxRequestSize = 1024 * 1024; // 1MB
        validation.AllowedContentTypes = new[]
        {
            "application/json",
            "application/x-www-form-urlencoded"
        };
    })
);
```

## Monitoring and Logging

### 1. Audit Logging

```sql
CREATE TRIGGER [ProxyR].[TR_AuditLog] ON [ProxyR].[Api_Users_View]
INSTEAD OF SELECT AS
BEGIN
    -- Log access
    INSERT INTO [Audit].[ApiAccess] (
        Timestamp,
        Username,
        Action,
        Resource
    )
    VALUES (
        GETUTCDATE(),
        SYSTEM_USER,
        'SELECT',
        'Users'
    );

    -- Execute original query
    SELECT Id, Username, Email
    FROM dbo.User;
END;
```

### 2. Error Handling

```csharp
services.AddProxyR(options => options
    .UseErrorHandling(error =>
    {
        error.HideErrorDetails = true; // In production
        error.LogLevel = LogLevel.Error;
        error.OnError = (context, exception) =>
        {
            // Custom error logging
            logger.LogError(exception, "ProxyR error");
            return new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred",
                Type = "https://api.example.com/errors/internal"
            };
        };
    })
);
```

## Best Practices Checklist

1. **Database Security**
   - [ ] Use dedicated schema
   - [ ] Implement row-level security
   - [ ] Use least-privilege accounts
   - [ ] Encrypt sensitive data

2. **Parameter Security**
   - [ ] Exclude sensitive parameters
   - [ ] Validate all inputs
   - [ ] Use parameter overrides
   - [ ] Implement required parameters

3. **Authentication**
   - [ ] Use JWT or OAuth
   - [ ] Implement role-based access
   - [ ] Secure token storage
   - [ ] Regular token rotation

4. **Network Security**
   - [ ] Enforce HTTPS
   - [ ] Use secure connection strings
   - [ ] Implement rate limiting
   - [ ] Configure CORS properly

5. **Monitoring**
   - [ ] Implement audit logging
   - [ ] Monitor API usage
   - [ ] Set up alerts
   - [ ] Regular security reviews

## Next Steps

- Review [Configuration Options](./configuration.html)
- Implement [Examples](./examples.html)
- Check [Troubleshooting](./troubleshooting.html)