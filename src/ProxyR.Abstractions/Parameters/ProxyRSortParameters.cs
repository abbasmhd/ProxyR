using System;
using Newtonsoft.Json;
using ProxyR.Core.Extensions;

namespace ProxyR.Abstractions.Parameters
{
    public class ProxyRSortParameters
    {
        /// <summary>
        /// Gets or sets the ColumnName.
        /// </summary>
        [JsonProperty("selector")]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the literal value.
        /// Column Position.
        /// </summary>
        [JsonIgnore]
        public string Literal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order is descending.
        /// </summary>
        /// <param name="IsDescending">A boolean value indicating whether the order is descending.</param>
        /// <returns>A boolean value indicating whether the order is descending.</returns>
        [JsonProperty("desc")]
        public bool IsDescending { get; set; }

        /// <summary>
        /// Generates a string representation of the OrderByClause object.
        /// </summary>
        /// <returns>A string representation of the OrderByClause object.</returns>
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