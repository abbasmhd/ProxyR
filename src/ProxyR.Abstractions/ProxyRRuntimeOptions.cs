using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ProxyR.Abstractions
{
    public class ProxyRRuntimeOptions
    {
        public IList<Action<HttpContext, IDictionary<string, object>>> ParameterModifiers { get; set; } = new List<Action<HttpContext, IDictionary<string, object>>>();
    }
}
