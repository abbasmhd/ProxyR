using System;
using System.Collections.Generic;

namespace ProxyR.Abstractions
{

    public class ProxyROptions
    {
        /// <summary>
        ///     Uses the given connection-string for to run the SQL
        ///     table-valued functions.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Uses the connection-string with the given name,
        ///     found within the "ConnectionStrings" section of the configuration.
        /// </summary>
        public string ConnectionStringName { get; set; }

        /// <summary>
        ///     When mapping a function to a URL, the prefix will be prepended
        ///     when searching for the SQL table-valued function.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        ///     When mapping a function to a URL, the suffix will be appended
        ///     when searching for the SQL table-valued function.
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        ///     Ensures certain parameters exist, and are passed to the function.
        /// </summary>
        public IList<string> RequiredParameterNames { get; set; } = new List<string>();

        /// <summary>
        ///     Indicates whether to take the SQL schema from the first part of the path.
        /// </summary>
        public bool IncludeSchemaInPath { get; set; }

        /// <summary>
        ///     When the SQL schema is not provided on the path, what SQL schema should be used.
        ///     Defaults to "dbo".
        /// </summary>
        public string DefaultSchema { get; set; }

        /// <summary>
        ///     The string used when taking path segments, and joining them together to make the
        ///     function name. Default is empty.
        /// </summary>
        public char? FunctionNameSeperator { get; set; }

        /// <summary>
        ///     The name of the overridden parameters.
        ///     NOTE: These should be added by the options builder.
        /// </summary>
        public ICollection<string> ExcludedParameters { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    }
}
