using Microsoft.OpenApi.Models;
using ProxyR.Abstractions;
using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions.Commands;
using ProxyR.Abstractions.Execution;
using ProxyR.Core.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProxyR.OpenAPI.DocumentFilters
{
    public class ProxyRDocumentFilter : IDocumentFilter
    {
        private readonly ProxyROptions proxyROptions;
        private readonly OpenAPIOptions openAPIOptions;

        class ProxyRParameter
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int? MaxLength { get; set; }
        }

        class ProxyRColumn
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int? MaxLength { get; set; }

        }

        public ProxyRDocumentFilter(ProxyROptions proxyROptions, OpenAPIOptions openAPIOptions, string? connectionString = default)
        {
            this.proxyROptions = proxyROptions;
            this.openAPIOptions = openAPIOptions;

            if (connectionString != default)
            {
                proxyROptions.ConnectionString = connectionString;
            }
        }

        public void Apply(OpenApiDocument openApiDocument, DocumentFilterContext context)
        {
            var prefix = proxyROptions.Prefix ?? String.Empty;
            var suffix = proxyROptions.Suffix ?? String.Empty;
            var schemaName = proxyROptions.DefaultSchema ?? "dbo";
            var excludedParameters = proxyROptions.ExcludedParameters.ToArray() ?? Array.Empty<string>();

            var functionNameQuery = Db.Query(
                proxyROptions.ConnectionString,
                $"SELECT Name FROM sys.objects WHERE TYPE IN ('TF', 'IF') AND NAME LIKE @0 + '%' + @1",
                prefix,
                suffix);

            var functionNames = functionNameQuery.ToScalarArrayAsync<string>().Result;

            foreach (var functionName in functionNames)
            {
                var parameterQuery = Db.Query(
                    proxyROptions.ConnectionString,
                    @$"SELECT [Name] = name, [Type] = TYPE_NAME(user_type_id), [MaxLength] = max_length
                         FROM SYS.PARAMETERS
                        WHERE OBJECT_ID = OBJECT_ID('[{Sql.Sanitize(schemaName)}].[{Sql.Sanitize(functionName)}]')
                        ORDER BY PARAMETER_ID");

                var columnQuery = Db.Query(
                    proxyROptions.ConnectionString,
                    @$"SELECT [Name] = name, [Type] = TYPE_NAME(user_type_id), [MaxLength] = max_length
                         FROM sys.columns
                        WHERE object_id = OBJECT_ID('[{Sql.Sanitize(schemaName)}].[{Sql.Sanitize(functionName)}]')
                        ORDER BY column_id");

                var pathPart = functionName.Remove(0, prefix.Length);
                pathPart = pathPart.Remove(prefix.Length - suffix.Length, suffix.Length);

                var firstSegment = pathPart.Split('_').First();

                // ProxyR endpoinmt Path/URI.
                var pathName = pathPart.Replace('_', '/').Trim('/').ToLowerInvariant();
                if (proxyROptions.IncludeSchemaInPath)
                {
                    pathName = $"{schemaName}/{pathName}";
                }

                var dbParameters = parameterQuery.ToArray<ProxyRParameter>();

                var pathParameters = dbParameters
                    .Where(x => !excludedParameters.Contains(x.Name.TrimStart('@'), StringComparer.InvariantCultureIgnoreCase))
                    .Select(x => new OpenApiParameter
                    {

                        Name = x.Name.TrimStart('@').ToCamelCase(),
                        In = ParameterLocation.Query,
                        //Required = true // TODO check is_nullable
                        Schema = new OpenApiSchema()
                        {
                            Type = DbTypes.ToJsType(x.Type),
                            Title = x.Name.TrimStart('@').ToCamelCase(),
                            Description = "Description " + x.Name.TrimStart('@').ToCamelCase(),
                            MaxLength = x.MaxLength
                        }
                    })
                    .ToList();

                var dbProperties = columnQuery.ToArray<ProxyRColumn>();

                openApiDocument.Components.Schemas.Add($"{pathPart}_model", new OpenApiSchema
                {
                    Type = "object",
                    Properties = dbProperties.Select(x =>
                            (Name: x.Name.ToCamelCase(),
                            Schema: new OpenApiSchema()
                            {
                                Type = DbTypes.ToJsType(x.Type),
                                Title = x.Name.TrimStart('@').ToCamelCase(),
                                MaxLength = x.MaxLength
                            })
                        )
                        .ToDictionary(
                            keySelector: x => x.Name,
                            elementSelector: x => x.Schema,
                            comparer: StringComparer.InvariantCultureIgnoreCase)
                });

                openApiDocument.Components.Schemas.Add($"{pathPart}_array", new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = $"{pathPart}_model" }
                    }
                });

                var responseSchema = new OpenApiResponse()
                {
                    Description = "Success",
                    Content = new Dictionary<string, OpenApiMediaType>()
                    {
                        {"application/json", new OpenApiMediaType()
                            {
                                Schema = new OpenApiSchema()
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>()
                                    {
                                        { "Results",
                                            new OpenApiSchema()
                                            {
                                                Reference = new OpenApiReference
                                                {
                                                    Type = ReferenceType.Schema,
                                                    Id = $"{pathPart}_array"
                                                }
                                            }
                                        }
                                    }
                                }
                                //Schema = new OpenApiSchema
                                //{
                                //    Type = "object",
                                //    Properties = dbProperties.Select(x =>
                                //        (Name: x.Name.ToCamelCase(),
                                //        Schema: new OpenApiSchema()
                                //        {
                                //            Type = DbTypes.ToJsType(x.Type),
                                //            Title = x.Name.TrimStart('@').ToCamelCase(),
                                //            MaxLength = x.MaxLength
                                //        })
                                //    )
                                //    .ToDictionary(
                                //        keySelector: x => x.Name,
                                //        elementSelector: x => x.Schema,
                                //        comparer: StringComparer.InvariantCultureIgnoreCase)
                                //}
                            }
                        }
                    }
                };

                var openApiOperation = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag>() { new OpenApiTag() { Name = firstSegment } },
                    OperationId = functionName,
                    Parameters = pathParameters,
                    Responses = new OpenApiResponses() {
                        { "200", responseSchema },
                        { "401", new OpenApiResponse { Description = "Unauthorized" } },
                        { "403", new OpenApiResponse(){ Description = "Forbidden"} },
                        { "404", new OpenApiResponse(){ Description = "Not Found"} },
                        { "405", new OpenApiResponse(){ Description = "Method Not Allowed"} },
                    }
                };

                var pathItem = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>()
                    {
                        { OperationType.Get, openApiOperation}
                    }
                };

                openApiDocument.Paths.Add($"/{pathName}", pathItem);
                openApiDocument.Tags.Add(new OpenApiTag
                {
                    Name = openAPIOptions.ApiName,
                    Description = openAPIOptions.ApiDescription
                });
            }
        }
    }
}
