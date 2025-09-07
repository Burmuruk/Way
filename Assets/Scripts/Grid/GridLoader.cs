using System;
using UnityEngine;

namespace Xolito.Utilities
{
    public class GridLoader : MonoBehaviour
    {
        [SerializeField] string fileName;
        [SerializeField] bool load;

        private void OnDrawGizmos()
        {
            if (load)
            {
                try
                {
                    Load();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    load = false;
                }
            }
        }

        private void Load()
        {
            var gridSaver = new GridSaver();
            gridSaver.LoadFromTxt("Assets/Data/" + fileName + ".json", Destroy);
        }
    } 
}
