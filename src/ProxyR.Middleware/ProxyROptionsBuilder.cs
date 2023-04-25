using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyR.Abstractions;
using ProxyR.Core.Extensions;

namespace ProxyR.Middleware
{
    /// <summary>
    /// Class used to build a ProxyROptions object.
    /// </summary>
    public class ProxyROptionsBuilder
    {

        /// <summary>
        /// Gets the ProxyROptions object.
        /// </summary>
        /// <returns>The ProxyROptions object.</returns>
        public ProxyROptions Options { get; } = new ProxyROptions();

        /// <summary>
        /// Gets the ProxyR runtime options.
        /// </summary>
        /// <returns>The ProxyR runtime options.</returns>
        public ProxyRRuntimeOptions RuntimeOptions { get; } = new ProxyRRuntimeOptions();


        /// <summary>
        /// Binds the configuration section to the ProxyROptionsBuilder.
        /// </summary>
        /// <param name="section">The configuration section to bind.</param>
        /// <returns>The ProxyROptionsBuilder instance.</returns>
        public ProxyROptionsBuilder BindConfiguration(IConfigurationSection section)
        {
            section.Bind(Options);

            return this;
        }

        /// <summary>
        /// Copies the given <see cref="ProxyROptions"/> to the current instance.
        /// </summary>
        /// <param name="options">The <see cref="ProxyROptions"/> to copy.</param>
        /// <returns>The current <see cref="ProxyROptionsBuilder"/> instance.</returns>
        public ProxyROptionsBuilder CopyFrom(ProxyROptions options)
        {
            options.Clone(Options);

            return this;
        }

        /// <summary>
        /// Sets the connection string for the ProxyR options.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        /// <returns>The ProxyR options builder.</returns>
        public ProxyROptionsBuilder UseConnectionString(string connectionString)
        {
            Options.ConnectionString = connectionString;

            return this;
        }

        /// <summary>
        /// Sets the connection string name to be used for the proxy.
        /// </summary>
        /// <param name="connectionStringName">The connection string name.</param>
        /// <returns>The current <see cref="ProxyROptionsBuilder"/>.</returns>
        public ProxyROptionsBuilder UseConnectionStringName(string connectionStringName)
        {
            Options.ConnectionStringName = connectionStringName;

            return this;
        }

        /// <summary>
        /// Sets the prefix for the function names.
        /// </summary>
        /// <param name="prefix">The prefix to use for the function names.</param>
        /// <returns>The current <see cref="ProxyROptionsBuilder"/>.</returns>
        public ProxyROptionsBuilder UseFunctionNamePrefix(string prefix)
        {
            Options.Prefix = prefix;

            return this;
        }

        /// <summary>
        /// Sets the suffix to be used for function names.
        /// </summary>
        /// <param name="suffix">The suffix to be used for function names.</param>
        /// <returns>The current <see cref="ProxyROptionsBuilder"/>.</returns>
        public ProxyROptionsBuilder UseFunctionNameSuffix(string suffix)
        {
            Options.Suffix = suffix;

            return this;
        }

        /// <summary>
        ///     During a request being executed, a parameter can be added or override an existing parameter.
        ///     This can be useful for ensuring parameters such as UserPassword or UserActive cannot be given
        ///     over the URL, query-string or request-body.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="getter">A function to get the value of the parameter.</param>
        /// <returns>The current <see cref="ProxyROptionsBuilder"/>.</returns>
        public ProxyROptionsBuilder OverrideParameter<T>(string name, Func<HttpContext, T> getter)
        {
            // Make sure we don't have a 
            // misconceived '@' character at the front.
            name = name.Trim().TrimStart('@');

            // Add the name of the overridden parameter.
            if (!Options.ExcludedParameters.Contains(name, StringComparer.InvariantCultureIgnoreCase))
            {
                Options.ExcludedParameters.Add(name);
            }

            // Add the modifier to override the parameter.
            RuntimeOptions.ParameterModifiers.Add((context, parameters) => parameters[name] = getter(context));

            return this;
        }

        /// <summary>
        /// Adds a required parameter to the ProxyROptionsBuilder.
        /// </summary>
        /// <param name="name">The name of the required parameter.</param>
        /// <returns>The ProxyROptionsBuilder instance.</returns>
        public ProxyROptionsBuilder RequireParameter(string name)
        {
            Options.RequiredParameterNames.Add(name);

            return this;
        }

        /// <summary>
        /// Enables the use of the schema in the path.
        /// </summary>
        /// <returns>The ProxyROptionsBuilder instance.</returns>
        public ProxyROptionsBuilder UseSchemaInPath()
        {
            Options.IncludeSchemaInPath = true;

            return this;
        }

        /// <summary>
        /// Sets the default schema for the ProxyR options.
        /// </summary>
        /// <param name="schemaName">The name of the schema.</param>
        /// <returns>The ProxyR options builder.</returns>
        public ProxyROptionsBuilder UseDefaultSchema(string schemaName)
        {
            Options.DefaultSchema = schemaName;

            return this;
        }

        /// <summary>
        /// Sets the function name seperator for the ProxyR options.
        /// </summary>
        /// <param name="seperator">The seperator to use.</param>
        /// <returns>The ProxyR options builder.</returns>
        public ProxyROptionsBuilder UseFunctionNameSeperator(char seperator)
        {
            Options.Seperator = seperator;

            return this;
        }

    }
}
