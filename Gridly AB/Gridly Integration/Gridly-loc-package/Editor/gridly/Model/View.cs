using System.Collections.Generic;
using UnityEngine;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents a column definition within a Gridly view.
    /// Contains metadata about the column's properties and behavior.
    /// </summary>
    [System.Serializable]
    public class Column
    {
        /// <summary>
        /// Gets or sets the unique identifier for this column.
        /// </summary>
        public string id;
        
        /// <summary>
        /// Gets or sets the description of this column.
        /// </summary>
        public string description;
        
        /// <summary>
        /// Gets or sets whether this column is editable by users.
        /// </summary>
        public bool editable;
        
        /// <summary>
        /// Gets or sets whether this column is a source column for localization.
        /// </summary>
        public bool isSource;
        
        /// <summary>
        /// Gets or sets whether this column is a target column for localization.
        /// </summary>
        public bool isTarget;
        
        /// <summary>
        /// Gets or sets the language code for localization columns (e.g., "en", "es", "fr").
        /// </summary>
        public string languageCode;
        
        /// <summary>
        /// Gets or sets the localization type for this column (e.g., "source", "target").
        /// </summary>
        public string localizationType;
        
        /// <summary>
        /// Gets or sets the display name of this column.
        /// </summary>
        public string name;
        
        /// <summary>
        /// Gets or sets the data type of this column (e.g., "text", "number", "boolean").
        /// </summary>
        public string type;
        
        /// <summary>
        /// Gets or sets the ID of the column this column depends on, if any.
        /// </summary>
        public string dependsOn;
    }

    /// <summary>
    /// Represents a view in the Gridly system.
    /// Contains a collection of columns and metadata about the grid view.
    /// </summary>
    [System.Serializable]
    public class View
    {
        /// <summary>
        /// Gets or sets the unique identifier for this view.
        /// </summary>
        public string id;
        
        /// <summary>
        /// Gets or sets the collection of columns that make up this view.
        /// </summary>
        public List<Column> columns;
        
        /// <summary>
        /// Gets or sets the ID of the grid this view belongs to.
        /// </summary>
        public string gridId;
        
        /// <summary>
        /// Gets or sets the current status of the grid.
        /// </summary>
        public string gridStatus;
        
        /// <summary>
        /// Gets or sets the display name of this view.
        /// </summary>
        public string name;
    }
}
