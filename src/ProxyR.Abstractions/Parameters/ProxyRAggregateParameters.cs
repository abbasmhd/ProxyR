using Newtonsoft.Json;

namespace ProxyR.Abstractions.Parameters
{
  public class ProxyRAggregateParameters
  {
    /// <summary>
    /// Cloumn Name.
    /// </summary>
    [JsonProperty("selector")]
    public string ColumnName { get; set; }

  }
}