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

            pipeline.Use(async (context, next) => {
                context.Items["welcomeJson:openApi"] = ResolveJsonPath(jsonPath, openApiOptions);
                await next();
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

            //pipeline.Map($"/{uiPath}/swagger-logo.svg", c => c.Use(async (httpContext, n) => {
            //    httpContext.Response.ContentType = "image/svg+xml";
            //    await httpContext.Response.Body.WriteAsync(Resources.SwaggerLogoSvg, 0, Resources.SwaggerLogoSvg.Length);
            //}));

            //pipeline.Map($"/{uiPath}/swagger-material.css", c => c.Use(async (httpContext, n) => {
            //    httpContext.Response.ContentType = "text/css";
            //    await httpContext.Response.Body.WriteAsync(Resources.SwaggerMaterialStyles, 0, Resources.SwaggerMaterialStyles.Length);
            //}));

            //pipeline.Map($"/{uiPath}/swagger-personal.css", c => c.Use(async (httpContext, n) => {
            //    var bodyText = Resources.SwaggerDepotnetStyles;
            //    var processedBodyText = bodyText.Replace("{{uiPath}}", uiPath);
            //    var bodyBytes = Encoding.UTF8.GetBytes(processedBodyText);

            //    httpContext.Response.ContentType = "text/css";
            //    await httpContext.Response.Body.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            //}));

            pipeline.UseSwaggerUI(options => {
                options.DocumentTitle = openApiOptions.ApiName;
                options.RoutePrefix = uiPath;
                options.SwaggerEndpoint(jsonPath, openApiOptions.ApiName);
                //options.InjectStylesheet($"/{uiPath}/swagger-material.css");
                //options.InjectStylesheet($"/{uiPath}/swagger-personal.css");
            });

            pipeline.Use(async (context, next) => {
                context.Items["welcomeJson:openApiUI"] = $"/{uiPath}";
                await next();
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
