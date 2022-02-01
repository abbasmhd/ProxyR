using Microsoft.Extensions.Configuration;
using ProxyR.Abstractions;
using ProxyR.Core.Extensions;
using ProxyR.OpenAPI.DocumentFilters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProxyR.OpenAPI
{
    public class OpenAPIOptionsBuilder
    {
        public OpenAPIOptions Options { get; } = new OpenAPIOptions();

        /// <summary>
        /// Copies the options from a configuration section.
        /// </summary>
        public OpenAPIOptionsBuilder CopyFrom(IConfigurationSection section)
        {
            section.Bind(Options);
            return this;
        }

        /// <summary>
        /// Copies the options from a source.
        /// </summary>
        public OpenAPIOptionsBuilder CopyFrom(OpenAPIOptions options)
        {
            options.Clone(Options);
            return this;
        }

        /// <summary>
        /// Defines if an XML file should be imported with comments etc.
        /// If the filename is not given, the filename with be the same as the assembly.
        /// </summary>
        public OpenAPIOptionsBuilder UseXmlFile(string? fileName = default)
        {
            Options.IncludeXmlFile = true;
            Options.XmlFileName = fileName;
            return this;
        }

        /// <summary>
        /// Display name of the API.
        /// </summary>
        public OpenAPIOptionsBuilder UseApiName(string value)
        {
            Options.ApiName = value;
            return this;
        }

        /// <summary>
        /// Human-readable summary describing the API.
        /// </summary>
        public OpenAPIOptionsBuilder UseApiDescription(string value)
        {
            Options.ApiDescription = value;
            return this;
        }

        /// <summary>
        /// The version of the API.
        /// </summary>
        public OpenAPIOptionsBuilder UseApiVersion(string value)
        {
            Options.ApiVersion = value;
            return this;
        }

        /// <summary>
        /// The name of the OpenAPI document.
        /// </summary>
        public OpenAPIOptionsBuilder UseDocumentName(string value)
        {
            Options.DocumentName = value;
            return this;
        }

        /// <summary>
        /// The route/path the document will be available at.
        /// </summary>
        public OpenAPIOptionsBuilder UseDocumentRoute(string value)
        {
            Options.DocumentRoute = value;
            return this;
        }

        /// <summary>
        /// The route/path the UI will be available at.
        /// </summary>
        public OpenAPIOptionsBuilder UseUIRoute(string value)
        {
            Options.UiRoute = value;
            return this;
        }

        /// <summary>
        /// Adds a DocumentFilter component, that will make changes 
        /// to the OpenAPI document before it is served.
        /// </summary>
        public OpenAPIOptionsBuilder UseDocumentFilter<TFilter>(params object[] arguments) where TFilter : IDocumentFilter
        {
            Options.DocumentFilters.Add(new OpenAPIDocumentFilter
            {
                Type = typeof(TFilter),
                Arguments = arguments
            });

            return this;
        }

        /// <summary>
        /// Given the configuration for ProxyR, will map those functions in the OpenAPI document.
        /// </summary>
        public OpenAPIOptionsBuilder UseProxyR(IConfigurationSection section, string? connectionString = default)
        {
            var options = new ProxyROptions();
            section.Bind(options);

            UseProxyR(options, connectionString);

            return this;
        }

        /// <summary>
        /// Given the configuration for ProxyR, will map those functions in the OpenAPI document.
        /// </summary>
        public OpenAPIOptionsBuilder UseProxyR(ProxyROptions proxyROptions, string? connectionString = default)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                UseDocumentFilter<ProxyRDocumentFilter>(proxyROptions, this.Options);
            }
            else
            {
                UseDocumentFilter<ProxyRDocumentFilter>(proxyROptions, this.Options, connectionString);
            }

            return this;
        }
    }
}
