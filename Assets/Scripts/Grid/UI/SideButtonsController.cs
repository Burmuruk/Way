using UnityEngine;

namespace Assets.Scripts.Grid.UI
{
    public class SideButtonsController : MonoBehaviour
    {
        public void ToggleVisiblity(GameObject target)
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
