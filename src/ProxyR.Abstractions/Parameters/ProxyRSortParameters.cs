using System;
using Newtonsoft.Json;
using ProxyR.Core.Extensions;

namespace ProxyR.Abstractions.Parameters
{
  public class ProxyRSortParameters : ProxyRAggregateParameters
  {
    /// <summary>
    /// Column Position.
    /// </summary>
    [JsonIgnore]
    public string Literal { get; set; }

    [JsonProperty("desc")]
    public bool IsDescending { get; set; }

    public override string ToString()
    {
      if (Literal.HasContent() && ColumnName.HasContent())
      {
        throw new InvalidOperationException($"Both {nameof(Literal)} and {nameof(ColumnName)} properties cannot be set.");
      }

      if (Literal.IsNullOrWhiteSpace() && ColumnName.IsNullOrWhiteSpace())
      {
        throw new InvalidOperationException($"Either {nameof(Literal)} or {nameof(ColumnName)} properties need a value, and both do not.");
      }

      var result = string.Empty;

      if (Literal.HasContent())
      {
        result += $"{Literal} ";
      }
      else if (ColumnName.HasContent())
      {
        result += $"[{ColumnName}] ";
      }

      if (IsDescending)
      {
        result += "DESC";
      }

      return result.Trim();
    }
  }
}