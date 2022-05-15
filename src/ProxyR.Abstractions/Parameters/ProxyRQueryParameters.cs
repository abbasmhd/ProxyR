using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProxyR.Abstractions.Parameters
{
  public class ProxyRQueryParameters
  {
    [JsonProperty("group")]
    public IList<ProxyRGroupParameters> Grouping { get; set; } = new List<ProxyRGroupParameters>();

    /// <summary>
    /// The Selected Fields to be returned.
    /// </summary>
    [JsonProperty("select")]
    public IList<string> Fields { get; set; } = new List<string>();

    [JsonProperty("filter")]
    public JArray Filter { get; set; }

    [JsonProperty("orderby")]
    public IList<ProxyRSortParameters> Sort { get; set; } = new List<ProxyRSortParameters>();

    /// <summary>
    /// Paginations number of records in a page.
    /// </summary>
    [JsonProperty("take")]
    public int? Take { get; set; }

    /// <summary>
    /// Paginations skip x pages return page number x+1.
    /// </summary>
    [JsonProperty("skip")]
    public int? Skip { get; set; }

    [JsonProperty("showTotal")]
    public bool ShowTotal { get; set; }

    [JsonProperty("loadAll")]
    public bool LoadAll { get; set; }

    /// <summary>
    /// Parameters from the query-string (that don't start with '$').
    /// </summary>
    [JsonProperty("userData")]
    public IDictionary<string, object> UserData { get; set; } = new Dictionary<string, object>();

  }
}
