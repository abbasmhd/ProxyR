using System;
using System.Collections.Generic;

namespace ProxyR.Abstractions
{
    /// <summary>
    /// Represents the options for a ProxyR request.
    /// </summary>
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
        ///     Indicates whether to take the SQL schema from the first part of the path.
        /// </summary>
        public bool IncludeSchemaInPath { get; set; }

        /// <summary>
        ///     When the SQL schema is not provided on the path, what SQL schema should be used.
        ///     Defaults to "dbo".
        /// </summary>
        public string DefaultSchema { get; set; }

        /// <summary>
        ///     The string used when taking path segments, and joining them together to make the name.
        ///     Default is empty.
        /// </summary>
        public char? Seperator { get; set; }

        /// <summary>
        ///     Ensures certain parameters exist, and are passed to the function.
        /// </summary>
        public ICollection<string> RequiredParameterNames { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        ///     The name of the overridden parameters.
        ///     NOTE: These should be added by the options builder.
        /// </summary>
        public ICollection<string> ExcludedParameters { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        ///     The maximum value accepted for $take / "take" paging.
        ///     When null, no limit is applied. It is recommended to configure
        ///     a limit in production to prevent resource exhaustion.
        ///     Default is null (unbounded).
        /// </summary>
        public int? MaxTake { get; set; }

        /// <summary>
        ///     Whether the "loadAll" request flag is honored. When false,
        ///     the flag is ignored and paging is always applied.
        ///     Default is true (existing behavior).
        /// </summary>
        public bool AllowLoadAll { get; set; } = true;
    }
}
