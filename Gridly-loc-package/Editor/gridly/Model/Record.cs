using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    [Serializable]
    public class Record
    {
        public string id;
        public List<Cell> cells = new List<Cell>();
        public string path;
    }

    [Serializable]
    public class Cell
    {
        public string columnId;
        public object value; // Change from string to object
        public string dependencyStatus;
        public string sourceStatus;

        // Optional: Helper method to get `value` as a string
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
