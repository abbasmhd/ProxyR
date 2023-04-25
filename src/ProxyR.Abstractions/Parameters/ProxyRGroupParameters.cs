using Newtonsoft.Json;

namespace ProxyR.Abstractions.Parameters
{
    public class ProxyRGroupParameters
    {
        /// <summary>
        /// Gets or sets the ColumnName property of the object.
        /// </summary>
        /// <param name="ColumnName">The name of the column.</param>
        /// <returns>The ColumnName property of the object.</returns>
        [JsonProperty("selector")]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is expanded; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("expanded")]
        public bool Expanded { get; set; }
    }
}
