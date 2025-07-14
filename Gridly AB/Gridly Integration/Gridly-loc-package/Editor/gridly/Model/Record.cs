using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents a record in the Gridly grid system.
    /// Contains an ID, path, and a collection of cells that hold the actual data.
    /// </summary>
    [Serializable]
    public class Record
    {
        /// <summary>
        /// Gets or sets the unique identifier for this record.
        /// </summary>
        public string id;
        
        /// <summary>
        /// Gets or sets the collection of cells that contain the record's data.
        /// </summary>
        public List<Cell> cells = new List<Cell>();
        
        /// <summary>
        /// Gets or sets the hierarchical path of this record within the grid structure.
        /// </summary>
        public string path;
    }

    /// <summary>
    /// Represents a single cell within a Gridly record.
    /// Contains the actual data value and metadata about the cell's state.
    /// </summary>
    [Serializable]
    public class Cell
    {
        /// <summary>
        /// Gets or sets the ID of the column this cell belongs to.
        /// </summary>
        public string columnId;
        
        /// <summary>
        /// Gets or sets the actual value stored in this cell.
        /// Can be of various types including string, boolean, or array.
        /// </summary>
        public object value; // Change from string to object
        
        /// <summary>
        /// Gets or sets the dependency status of this cell.
        /// Indicates whether the cell's value depends on other cells.
        /// </summary>
        public string dependencyStatus;
        
        /// <summary>
        /// Gets or sets the source status of this cell.
        /// Indicates the origin or status of the cell's data.
        /// </summary>
        public string sourceStatus;

        /// <summary>
        /// Converts the cell's value to a string representation.
        /// Handles different data types including strings, booleans, and arrays.
        /// </summary>
        /// <returns>A string representation of the cell's value, or empty string if null.</returns>
        public string GetValueAsString()
        {
            if (value is string stringValue)
            {
                return stringValue;
            }
            else if (value is bool boolValue)
            {
                return boolValue.ToString().ToLower(); // Convert boolean to "true" or "false"
            }
            else if (value is IList<object> arrayValue)
            {
                return string.Join(", ", arrayValue); // Convert array to a comma-separated string
            }
            return value?.ToString() ?? string.Empty; // Handle other types or null values
        }
    }
}
