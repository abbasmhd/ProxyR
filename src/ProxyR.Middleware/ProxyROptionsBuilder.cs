using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyR.Abstractions;
using ProxyR.Core.Extensions;

namespace ProxyR.Middleware
{
    public class ProxyROptionsBuilder
    {

        public ProxyROptions Options { get; } = new ProxyROptions();

        public ProxyRRuntimeOptions RuntimeOptions { get; } = new ProxyRRuntimeOptions();

        /// <summary>
        ///     Bind the options from a configuration section.
        /// </summary>
        public ProxyROptionsBuilder BindConfiguration(IConfigurationSection section)
        {
            section.Bind(Options);

            return this;
        }

        /// <summary>
        ///     Copies the options from a source.
        /// </summary>
        public ProxyROptionsBuilder CopyFrom(ProxyROptions options)
        {
            options.Clone(Options);

            return this;
        }

        /// <summary>
        ///     Uses the given connection-string for to run the SQL
        ///     table-valued functions.
        /// </summary>
        public ProxyROptionsBuilder UseConnectionString(string connectionString)
        {
            Options.ConnectionString = connectionString;

            return this;
        }

        /// <summary>
        ///     Uses the connection-string with the given name,
        ///     found within the "ConnectionStrings" section of the configuration.
        /// </summary>
        public ProxyROptionsBuilder UseConnectionStringName(string connectionStringName)
        {
            Options.ConnectionStringName = connectionStringName;

            return this;
        }

        /// <summary>
        ///     When mapping a function to a URL, the prefix will be prepended
        ///     when searching for the SQL table-valued function.
        /// </summary>
        public ProxyROptionsBuilder UseFunctionNamePrefix(string prefix)
        {
            Options.Prefix = prefix;

            return this;
        }

        /// <summary>
        ///     When mapping a function to a URL, the suffix will be appended
        ///     when searching for the SQL table-valued function.
        /// </summary>
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
        ///     Ensures certain parameters exist, and are passed to the function.
        /// </summary>
        public ProxyROptionsBuilder RequireParameter(string name)
        {
            Options.RequiredParameterNames.Add(name);

            return this;
        }

        /// <summary>
        ///     Indicates whether to take the SQL schema from the first part of the path.
        /// </summary>
        public ProxyROptionsBuilder UseSchemaInPath()
        {
            Options.IncludeSchemaInPath = true;

            return this;
        }

        /// <summary>
        ///     When the SQL schema is not provided on the path, what SQL schema should be used.
        ///     Defaults to "dbo".
        /// </summary>
        public ProxyROptionsBuilder UseDefaultSchema(string schemaName)
        {
            Options.DefaultSchema = schemaName;

            return this;
        }

        /// <summary>
        ///     The string used when taking path segments, and joining them together to make the
        ///     function name. Default is empty.
        /// </summary>
        public ProxyROptionsBuilder UseFunctionNameSeperator(char seperator)
        {
            Options.Seperator = seperator;

            return this;
        }

    }
}
