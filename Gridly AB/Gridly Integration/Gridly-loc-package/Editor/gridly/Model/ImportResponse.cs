using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    /// <summary>
    /// Represents the response from a Gridly import operation.
    /// Contains information about the number of records added, updated, and any errors that occurred.
    /// </summary>
    public class ImportResponse
    {
        /// <summary>
        /// Gets or sets the number of records that were updated during the import operation.
        /// </summary>
        public int Updated { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the number of records that were added during the import operation.
        /// </summary>
        public int Added { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets any error message that occurred during the import operation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Returns a formatted string representation of the import response.
        /// </summary>
        /// <returns>A string containing the number of records updated and added.</returns>
        public override string ToString()
        {
            return $"Number of records updated: {Updated}\nNumber of records added: {Added}";
        }
    }
}
