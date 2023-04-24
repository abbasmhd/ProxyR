using System;
using System.Collections.Generic;

namespace ProxyR.Middleware.Helpers
{
    internal class ParameterBuilder
    {
        /// <summary>
        ///     The collection of parameters being built.
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Counter that increments with each unnamed parameter.
        /// </summary>
        public int ParameterCounter { get; private set; }

        /// <summary>
        ///     Adds a parameter with a value, and returns the generated parameter name.
        /// </summary>
        public string Add(object value)
        {
            var name = $"@{ParameterCounter}";
            Parameters.Add(name, value);
            ParameterCounter++;

            return name;
        }
    }
}
