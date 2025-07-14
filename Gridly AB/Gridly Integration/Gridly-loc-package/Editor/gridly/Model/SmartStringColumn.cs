using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents a smart string column in the Gridly system.
    /// Used for handling string-based columns with additional metadata.
    /// </summary>
    [Serializable]
    internal class SmartStringColumn
    {
        /// <summary>
        /// Gets or sets the unique identifier for this column.
        /// </summary>
        public string id;
        
        /// <summary>
        /// Gets or sets the display name of this column.
        /// </summary>
        public string name;
        
        /// <summary>
        /// Gets or sets the data type of this column.
        /// </summary>
        public string type;
    }
}
