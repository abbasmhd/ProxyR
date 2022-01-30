using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyR.OpenAPI
{
    public class OpenAPIOptions
    {
        public string? ApiName { get; set; }

        public string? ApiDescription { get; set; }

        public string? ApiVersion { get; set; }

        public string? DocumentName { get; set; }

        public string? DocumentRoute { get; set; }

        public string? UiRoute { get; set; }

        public bool IncludeXmlFile { get; set; }

        public bool UseBearerAuthentication { get; set; }

        public bool UseFullNameForSchemaIds { get; set; }

        public string? XmlFileName { get; set; }

        [JsonIgnore]
        public ICollection<OpenAPIDocumentFilter> DocumentFilters { get; private set; } = new HashSet<OpenAPIDocumentFilter>();
    }
}
