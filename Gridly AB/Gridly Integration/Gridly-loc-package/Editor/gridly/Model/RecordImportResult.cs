using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Localization.Tables;

namespace Assets.Gridly_AB.Gridly_Integration.Gridly_loc_package.Editor.gridly.Model
{
    /// <summary>
    /// Represents the result of processing a single record for import.
    /// </summary>
    public class RecordImportResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public string Error { get; set; }
        public StringTable TableToSave { get; set; }

        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}
