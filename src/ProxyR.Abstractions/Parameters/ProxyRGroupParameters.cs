using Newtonsoft.Json;

namespace ProxyR.Abstractions.Parameters
{
  public class ProxyRGroupParameters : ProxyRAggregateParameters
  {
    [JsonProperty("expanded")]
    public bool Expanded { get; set; }
  }
}
