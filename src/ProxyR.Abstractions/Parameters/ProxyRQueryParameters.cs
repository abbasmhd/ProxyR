using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProxyR.Abstractions.Parameters
{
    public class ProxyRQueryParameters
    {
        /// <summary>
        /// Gets or sets the grouping parameters for the proxy request.
        /// </summary>
        /// <returns>The grouping parameters for the proxy request.</returns>
        [JsonProperty("group")]
        public IList<ProxyRGroupParameters> Grouping { get; set; } = new List<ProxyRGroupParameters>();

        /// <summary>
        /// Gets or sets the list of fields to select.
        /// </summary>
        /// <returns>The list of fields to select.</returns>
        [JsonProperty("select")]
        public IList<string> Fields { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the filter property of the object.
        /// </summary>
        /// <param name="Filter">The filter property of the object.</param>
        /// <returns>The filter property of the object.</returns>
        [JsonProperty("filter")]
        public JArray Filter { get; set; }

        /// <summary>
        /// Gets or sets the list of sort parameters for the request.
        /// </summary>
        /// <returns>The list of sort parameters for the request.</returns>
        [JsonProperty("orderby")]
        public IList<ProxyRSortParameters> Sort { get; set; } = new List<ProxyRSortParameters>();

        /// <summary>
        /// Gets or sets the number of items to take.
        /// </summary>
        /// <param name="Take">The number of items to take.</param>
        /// <returns>The number of items to take.</returns>
        [JsonProperty("take")]
        public int? Take { get; set; }

        /// <summary>
        /// Gets or sets the number of records to skip.
        /// </summary>
        /// <param name="Skip">The number of records to skip.</param>
        /// <returns>The number of records to skip.</returns>
        [JsonProperty("skip")]
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the total should be shown.
        /// </summary>
        /// <param name="showTotal">A boolean value indicating whether the total should be shown.</param>
        /// <returns>A boolean value indicating whether the total should be shown.</returns>
        [JsonProperty("showTotal")]
        public bool ShowTotal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to load all.
        /// </summary>
        /// <param name="loadAll">A boolean value indicating whether to load all.</param>
        /// <returns>A boolean value indicating whether to load all.</returns>
        [JsonProperty("loadAll")]
        public bool LoadAll { get; set; }

        /// <summary>
        /// Gets or sets the user data associated with the object.
        /// Parameters from the query-string (that don't start with '$').
        /// </summary>
        /// <returns>The user data associated with the object.</returns>
        [JsonProperty("userData")]
        public IDictionary<string, object> UserData { get; set; } = new Dictionary<string, object>();

    }
}
