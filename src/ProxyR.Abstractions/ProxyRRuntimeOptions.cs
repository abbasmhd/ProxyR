using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ProxyR.Abstractions
{
    /// <summary>
    /// Represents the options for the ProxyR runtime.
    /// </summary>
    public class ProxyRRuntimeOptions
    {
        /// <summary>
        /// Gets or sets the list of parameter modifiers.
        /// </summary>
        /// <returns>The list of parameter modifiers.</returns>
        public IList<Action<HttpContext, IDictionary<string, object>>> ParameterModifiers { get; set; } = new List<Action<HttpContext, IDictionary<string, object>>>();
    }
}
