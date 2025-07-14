using Newtonsoft.Json;
using System;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents a column creation request for the Gridly API.
    /// Contains all necessary information to create a new column in a Gridly grid.
    /// </summary>
    [Serializable]
    public class ColumnCreation
    {
        /// <summary>
        /// Gets or sets the unique identifier for the column.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Gets or sets whether this column is a target column for localization.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool isTarget { get; set; }

        /// <summary>
        /// Gets or sets the display name of the column.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the data type of the column (e.g., "text", "number", "boolean").
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string type { get; set; }

        /// <summary>
        /// Gets or sets the language code for localization columns (e.g., "en", "es", "fr").
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string languageCode { get; set; }

        /// <summary>
        /// Gets or sets the localization type for the column (e.g., "source", "target").
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string localizationType { get; set; }
    }
}
