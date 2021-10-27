using System;
using Newtonsoft.Json;

namespace ProxyR.Abstractions
{

    public class ProxyRSortParameters
    {

        [JsonIgnore]
        public string Literal { get; set; }

        [JsonProperty("selector")]
        public string ColumnName { get; set; }

        [JsonProperty("desc")]
        public bool IsDescending { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Literal) && !string.IsNullOrWhiteSpace(ColumnName))
            {
                throw new InvalidOperationException($"Both {nameof(Literal)} and {nameof(ColumnName)} properties cannot be set.");
            }

            if (string.IsNullOrWhiteSpace(Literal) && string.IsNullOrWhiteSpace(ColumnName))
            {
                throw new InvalidOperationException($"Either {nameof(Literal)} or {nameof(ColumnName)} properties need a value, and both do not.");
            }

            var result = string.Empty;

            if (Literal != null)
            {
                result += $"{Literal} ";
            }
            else if (ColumnName != null)
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