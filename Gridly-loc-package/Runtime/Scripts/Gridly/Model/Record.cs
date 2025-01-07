using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Model
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
        public string value;
    }

}
