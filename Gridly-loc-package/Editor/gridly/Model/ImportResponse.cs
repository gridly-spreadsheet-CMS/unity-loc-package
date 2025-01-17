using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    public class ImportResponse
    {
        public int Updated { get; set; } = 0;
        public int Added { get; set; } = 0;
        public override string ToString()
        {
            return $"Number of records updated: {Updated}\nNumber of records added: {Added}";
        }
        public string Error { get; set; }
    }
}
