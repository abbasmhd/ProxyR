using Newtonsoft.Json;

namespace ProxyR.Abstractions.Parameters
{
    public class ProxyRGroupParameters
    {
        /// <summary>
        /// Cloumn Name.
        /// </summary>
        [JsonProperty("selector")]
        public string ColumnName { get; set; }

        [JsonProperty("expanded")]
        public bool Expanded { get; set; }
    }
}
