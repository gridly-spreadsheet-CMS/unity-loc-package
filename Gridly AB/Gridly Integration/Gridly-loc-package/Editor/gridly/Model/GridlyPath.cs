using System;
using System.Collections.Generic;
using UnityEngine; // Ensure this is included for the Serializable attribute

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents a hierarchical path structure in Gridly.
    /// Used for organizing records in a tree-like structure within the grid.
    /// </summary>
    [System.Serializable]
    public class GridlyPath
    {
        /// <summary>
        /// Gets or sets the child paths in the hierarchy.
        /// Marked as NonSerialized to prevent Unity serialization issues with recursive structures.
        /// </summary>
        [NonSerialized]
        public List<GridlyPath> children; // Changed from List<object> to List<GridlyPath>
        
        /// <summary>
        /// Gets or sets the name of this path node.
        /// </summary>
        public string name;
    }
}
