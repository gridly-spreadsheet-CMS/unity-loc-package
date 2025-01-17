using System.Collections.Generic;
using UnityEngine;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    [System.Serializable]
    public class Column
    {
        public string id;
        public string description;
        public bool editable;
        public bool isSource;
        public bool isTarget;
        public string languageCode;
        public string localizationType;
        public string name;
        public string type;
        public string dependsOn;
    }

    [System.Serializable]
    public class View
    {
        public string id;
        public List<Column> columns;
        public string gridId;
        public string gridStatus;
        public string name;
    }
}
