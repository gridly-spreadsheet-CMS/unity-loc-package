using System;
using System.Collections.Generic;
using UnityEngine; // Ensure this is included for the Serializable attribute

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model
{
    [System.Serializable]
    public class GridlyPath
    {
        [NonSerialized]
        public List<GridlyPath> children; // Changed from List<object> to List<GridlyPath>
        public string name;
    }
}
