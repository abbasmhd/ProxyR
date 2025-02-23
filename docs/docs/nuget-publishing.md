---
layout: default
title: NuGet Package Publishing
---

# Publishing ProxyR Packages to NuGet

This guide explains how to publish ProxyR packages to both GitHub Packages and NuGet.org.

## Package Configuration

### 1. Update Project File

Ensure your `.csproj` files include the necessary package metadata:

```xml
<PropertyGroup>
    <PackageId>ProxyR.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>abbasmhd</Authors>
    <Company>ProxyR</Company>
    <Description>A powerful .NET middleware that automatically exposes SQL Server functions and views as REST API endpoints. ProxyR simplifies API creation by automatically mapping database objects to RESTful endpoints with minimal configuration.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://abbasmhd.github.io/ProxyR/</PackageProjectUrl>
    <RepositoryUrl>https://github.com/abbasmhd/ProxyR.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>api;rest;sql-server;middleware;dotnet;database;proxy;rest-api;aspnetcore;webapi;database-first;auto-api</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageReleaseNotes>https://github.com/abbasmhd/ProxyR/releases</PackageReleaseNotes>
    <PackageIcon>icon.svg</PackageIcon>
    <PackageIconUrl>https://abbasmhd.github.io/ProxyR/assets/images/icon.svg</PackageIconUrl>
    <DocumentationUrl>https://github.com/abbasmhd/ProxyR/wiki</DocumentationUrl>
    <Copyright>Copyright © 2024 ProxyR</Copyright>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <DebugType>portable</DebugType>
    <IncludeSource>true</IncludeSource>
</PropertyGroup>

<PropertyGroup Condition="'$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>

<ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="/" />
    <None Include="../../docs/assets/images/icon.svg" Pack="true" PackagePath="/" />
</ItemGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
</ItemGroup>
```

### 2. Package Versioning

We use Semantic Versioning (SemVer):
- MAJOR version for incompatible API changes
- MINOR version for backwards-compatible functionality
- PATCH version for backwards-compatible bug fixes

## Automated Publishing

### Release Process

1. Create and push a new version tag:
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

2. The NuGet workflow will automatically:
   - Build and test the solution
   - Create NuGet packages
   - Publish to GitHub Packages
   - Publish to NuGet.org (only for tagged releases)

### Required Secrets

1. `GITHUB_TOKEN` (automatically provided)
   - Used for publishing to GitHub Packages
   - No configuration needed

2. `NUGET_API_KEY`
   - Required for publishing to NuGet.org
   - Create at [NuGet.org](https://www.nuget.org)
   - Add to repository secrets

## Manual Publishing

### Local Package Creation

```bash
dotnet pack --configuration Release
```

### Publishing to NuGet.org

```bash
dotnet nuget push "bin/Release/ProxyR.Core.1.0.0.nupkg" --source "https://api.nuget.org/v3/index.json" --api-key YOUR_API_KEY
```

### Publishing to GitHub Packages

```bash
dotnet nuget push "bin/Release/ProxyR.Core.1.0.0.nupkg" --source "https://nuget.pkg.github.com/abbasmhd/index.json" --api-key YOUR_GITHUB_TOKEN
```

## Package Installation

### From NuGet.org

```bash
dotnet add package ProxyR.Core
```

### From GitHub Packages

1. Authenticate with GitHub Packages:
   ```bash
   dotnet nuget add source --username USERNAME --password GITHUB_TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/abbasmhd/index.json"
   ```

2. Install the package:
   ```bash
   dotnet add package ProxyR.Core
   ```

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Verify API keys are correct
   - Check token permissions (needs `packages: write`)
   - Ensure source URLs are correct

2. **Package Validation Errors**
   - Verify package metadata
   - Check version numbers
   - Ensure README.md is included
   - Validate package contents

3. **Build Issues**
   - Clean solution
   - Restore packages
   - Check for dependency conflicts

4. **Publishing Issues**
   - Verify tag format (must start with 'v')
   - Check workflow logs
   - Confirm secret availability

## Best Practices

1. Always update package version numbers
2. Include detailed release notes
3. Test packages before tagging
4. Keep documentation up to date
5. Follow semantic versioning
6. Use meaningful tag messages

## Next Steps

- Review [Configuration Guide](./configuration.html)
- Check [Security Best Practices](./security.html)
- Explore [Example Implementations](./examples.html)