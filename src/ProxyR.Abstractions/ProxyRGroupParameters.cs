using Newtonsoft.Json;

namespace ProxyR.Abstractions
{
    public class ProxyRGroupParameters
    {
        [JsonProperty("selector")]
        public string ColumnName { get; set; }

        [JsonProperty("isExpanded")]
        public bool IsExpanded { get; set; }
    }
}
