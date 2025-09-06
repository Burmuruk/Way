using System;
using UnityEngine;

namespace Xolito.Utilities
{
    [ExecuteAlways]
    public class GridLoader : MonoBehaviour
    {
        [SerializeField] string fileName;
        [SerializeField] bool load;

        private void FixedUpdate()
        {
            if (load)
            {
                Load();
                load = false;
            }
        }

        private void Load()
        {
            var gridSaver = new GridSaver();
            gridSaver.LoadFromTxt(fileName, Destroy);
        }
    } 
}
