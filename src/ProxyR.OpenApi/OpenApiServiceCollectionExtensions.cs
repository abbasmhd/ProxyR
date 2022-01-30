using System.Reflection;
using Microsoft.OpenApi.Models;
using ProxyR.OpenAPI;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Open-API documentation services, implemented by Swagger.
        /// </summary>
        public static IServiceCollection AddOpenApi(
            this IServiceCollection services,
            Action<OpenAPIOptionsBuilder> optionsFunc)
        {
            OpenAPIOptions openApiOptions;

            if (optionsFunc == null)
                throw new ArgumentNullException(nameof(optionsFunc));

            var builder = new OpenAPIOptionsBuilder();
            optionsFunc.Invoke(builder);
            openApiOptions = builder.Options;

            var documentName = openApiOptions.DocumentName
                ?? openApiOptions.ApiVersion
                ?? "docs";

            services.AddSwaggerGen(options => {
                options.SwaggerDoc(documentName, new OpenApiInfo
                {
                    Title = openApiOptions.ApiName,
                    Version = openApiOptions.ApiVersion,
                    Description = openApiOptions.ApiDescription
                });

                if (openApiOptions.UseBearerAuthentication)
                {
                    var openApiSecurityScheme = new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey
                    };

                    options.AddSecurityDefinition("Bearer", openApiSecurityScheme);

                    var security = new OpenApiSecurityRequirement
                    {
                        { openApiSecurityScheme, new List<string>() }
                    };

                    options.AddSecurityRequirement(security);
                }
                               
                if (openApiOptions.IncludeXmlFile)
                {
                    var xmlFile = openApiOptions.XmlFileName ?? $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath);
                }

                if (openApiOptions.DocumentFilters.Any())
                {
                    options.DocumentFilterDescriptors.AddRange(openApiOptions.DocumentFilters.Select(x => new FilterDescriptor {
                        Type = x.Type,
                        Arguments = x.Arguments
                    }));
                }

                if (openApiOptions.UseFullNameForSchemaIds)
                {
                    options.CustomSchemaIds(x => x.FullName);
                }
            });

            return services;
        }
    }
}
