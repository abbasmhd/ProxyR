using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProxyR.Abstractions
{
    public class ProxyRQueryParameters
    {
        [JsonProperty("group")]
        public IList<ProxyRGroupParameters> Grouping { get; set; } = new List<ProxyRGroupParameters>();

        [JsonProperty("dataField")]
        public string DataField { get; set; }

        [JsonProperty("filter")]
        public JArray Filter { get; set; }

        [JsonProperty("sort")]
        public IList<ProxyRSortParameters> Sort { get; set; } = new List<ProxyRSortParameters>();

        [JsonProperty("skip")]
        public int? Skip { get; set; }

        [JsonProperty("take")]
        public int? Take { get; set; }

        [JsonProperty("searchValue")]
        public string SearchValue { get; set; }

        [JsonProperty("searchOperation")]
        public string SearchOperation { get; set; }

        [JsonProperty("requireTotalCount")]
        public bool RequireTotalCount { get; set; }

        [JsonProperty("isLoadingAll")]
        public bool IsLoadingAll { get; set; }

        [JsonProperty("userData")]
        public IDictionary<string, object> UserData { get; set; } = new Dictionary<string, object>();

        [JsonProperty("selectFields")]
        public string[] SelectFields { get; set; }
    }
}
