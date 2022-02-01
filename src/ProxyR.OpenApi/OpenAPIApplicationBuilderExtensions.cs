using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProxyR.OpenAPI;

namespace Microsoft.AspNetCore.Builder
{
    public static class OpenAPIExtensions
    {
        /// <summary>
        /// Generates the Open-API documentation JSON if the endpoint is requested.
        /// By default, the end-point is "docs.json".
        /// </summary>
        public static IApplicationBuilder UseOpenApiDocumentation(
            this IApplicationBuilder pipeline,
            Action<OpenAPIOptionsBuilder>? optionsFunc = null)
        {
            OpenAPIOptions openApiOptions;

            if (optionsFunc != null)
            {
                var builder = new OpenAPIOptionsBuilder();
                optionsFunc.Invoke(builder);
                openApiOptions = builder.Options;
            }
            else
            {
                var openApiOptionsService = pipeline.ApplicationServices.GetRequiredService<IOptions<OpenAPIOptions>>();
                openApiOptions = openApiOptionsService.Value;
            }

            var jsonPath = openApiOptions.DocumentRoute?.Trim('/');
            if (String.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = "{documentName}.json";
            }

            pipeline.UseSwagger(options => {
                options.RouteTemplate = jsonPath;
            });

            return pipeline;
        }

        /// <summary>
        /// Provides the UI for exploring the Open-API documentation.
        /// </summary>
        public static IApplicationBuilder UseOpenApiUi(
            this IApplicationBuilder pipeline,
            Action<OpenAPIOptionsBuilder>? optionsFunc = null)
        {
            OpenAPIOptions openApiOptions;

            if (optionsFunc != null)
            {
                var builder = new OpenAPIOptionsBuilder();
                optionsFunc.Invoke(builder);
                openApiOptions = builder.Options;
            }
            else
            { 
                var openApiOptionsService = pipeline.ApplicationServices.GetRequiredService<IOptions<OpenAPIOptions>>();
                openApiOptions = openApiOptionsService.Value;
            }

            var jsonPath = openApiOptions.DocumentRoute?.Trim('/');
            if (String.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = "{documentName}.json";
            }

            jsonPath = ResolveJsonPath(jsonPath, openApiOptions);

            var uiPath = openApiOptions.UiRoute?.Trim('/');
            if (String.IsNullOrWhiteSpace(uiPath))
            {
                uiPath = "docs";
            }

            pipeline.UseSwaggerUI(options => {
                options.DocumentTitle = openApiOptions.ApiName;
                options.RoutePrefix = uiPath;
                options.SwaggerEndpoint(jsonPath, openApiOptions.ApiName);
            });

            return pipeline;
        }

        private static string ResolveJsonPath(string jsonPath, OpenAPIOptions openApiOptions)
        {
            jsonPath = jsonPath.Replace("{documentName}",
                openApiOptions.DocumentName
                ?? openApiOptions.ApiVersion
                ?? "docs");

            jsonPath = $"/{jsonPath}";

            return jsonPath;
        }
    }
}
