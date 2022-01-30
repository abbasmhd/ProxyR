using Microsoft.OpenApi.Interfaces;
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
        private readonly ProxyROptions options;

        class ProxyRParameter
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }

        class ProxyRColumn
        {
            public string Name { get; set; }

            public string Type { get; set; }
        }

        public ProxyRDocumentFilter(ProxyROptions options, string? connectionString = default)
        {
            this.options = options;

            if (connectionString != default)
            {
                options.ConnectionString = connectionString;
            }
        }

        public void Apply(OpenApiDocument openApiDocument, DocumentFilterContext context)
        {

            // Tags are for group the operations
            var openApiTags = new List<OpenApiTag> {
                new OpenApiTag {
                    Name = "ProxyR Endpoints",
                    Description = "List all ProxyR endpoints"
                }
            };

            var prefix = options.Prefix ?? String.Empty;
            var suffix = options.Suffix ?? String.Empty;
            var schemaName = options.DefaultSchema ?? "dbo";
            var excludedParameters = options.ExcludedParameters.ToArray() ?? Array.Empty<string>();

            var functionNameQuery = Db.Query(
                options.ConnectionString,
                $"SELECT Name FROM sys.objects WHERE TYPE IN ('TF', 'IF') AND NAME LIKE @0 + '%' + @1",
                prefix,
                suffix);

            var functionNames = functionNameQuery.ToScalarArrayAsync<string>().Result;

            foreach (var functionName in functionNames)
            {
                var parameterQuery = Db.Query(
                    options.ConnectionString,
                    $"SELECT [Name] = name, [Type] = TYPE_NAME(user_type_id), [MaxLength] = max_length  FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[{Sql.Sanitize(schemaName)}].[{Sql.Sanitize(functionName)}]') ORDER BY PARAMETER_ID");

                var columnQuery = Db.Query(
                    options.ConnectionString,
                    $"SELECT [Name] = name, [Type] = TYPE_NAME(user_type_id) FROM sys.columns WHERE object_id = OBJECT_ID('[{Sql.Sanitize(schemaName)}].[{Sql.Sanitize(functionName)}]') ORDER BY column_id");

                var pathPart = functionName.Remove(0, prefix.Length);
                pathPart = pathPart.Remove(prefix.Length - suffix.Length, suffix.Length);

                var firstSegment = pathPart.Split('_').First();

                // ProxyR endpoinmt Path/URI.
                var pathName = pathPart.Replace('_', '/').Trim('/').ToLowerInvariant();

                var dbParameters = parameterQuery.ToArray<ProxyRColumn>();
                var dbProperties = columnQuery.ToArray<ProxyRParameter>();

                var pathParameters = dbParameters
                    .Where(x => !excludedParameters.Contains(x.Name.TrimStart('@'), StringComparer.InvariantCultureIgnoreCase))
                    .Select(x => new OpenApiParameter
                    {
                        Name = x.Name.TrimStart('@').ToCamelCase(),
                        In = ParameterLocation.Query,
                        Required = true
                    })
                    //.Cast<IOpenApiElement>()
                    .ToList();

                var resultName = $"{pathPart.ToPascalCase()}Result";

                //var resultSchema = new Schema
                //{
                //    Properties = new Dictionary<string, Schema>
                //    {
                //        ["results"] = new Schema
                //        {
                //            Type = "array",
                //            Items = new Schema
                //            {
                //                Properties = dbProperties
                //                    .Select(x => (
                //                        Name: x.Name.ToCamelCase(),
                //                        Schema: new Schema
                //                        {
                //                            Type = DbTypes.ToJsType(x.Type)
                //                        }))
                //                    .ToDictionary(
                //                        keySelector: x => x.Name,
                //                        elementSelector: x => x.Schema,
                //                        comparer: StringComparer.InvariantCultureIgnoreCase)
                //            }
                //        }
                //    }
                //};

                var pathItem = new OpenApiPathItem();

                pathItem.Operations = new Dictionary<OperationType, OpenApiOperation>();



                var openApiOperation = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag>() { new OpenApiTag() { Name = firstSegment } },
                    OperationId = functionName,
                    //Consumes = null,
                    //Produces = new[] { "application/json" },
                    Parameters = pathParameters,
                    Responses = new OpenApiResponses() {
                        {"200", new OpenApiResponse()
                            {
                                Description = "Success",
                                Content = new Dictionary<string, OpenApiMediaType>()
                                {
                                    {"application/json", new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Reference = new OpenApiReference()
                                                {
                                                    Type = ReferenceType.Schema,
                                                    Id = $"definitions/{resultName}"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                pathItem.Operations.Add(OperationType.Get, openApiOperation);

                //openApiDocument.Definitions.Add(resultName, resultSchema);

                openApiDocument.Paths.Add($"/{pathName}", pathItem);
            }
        }
    }
}
