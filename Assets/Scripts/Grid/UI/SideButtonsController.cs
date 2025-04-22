using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xolito.Utilities;

namespace Assets.Scripts.Grid.UI
{
    public class SideButtonsController : MonoBehaviour
    {
        [Header("Layer settings")]
        [SerializeField] Image pLayerImg;
        [SerializeField] TextMeshProUGUI pLayerTxt;
        [SerializeField] int layerTime = 500;

        private void Awake()
        {
            var grid = FindObjectOfType<GridController>();
            grid.OnLayerChanged += Show_layer;
            pLayerImg.gameObject.SetActive(false);
        }

        private void Show_layer(int layer)
        {
            pLayerImg.gameObject.SetActive(true);
            pLayerTxt.text = layer.ToString();
            pLayerImg.CrossFadeAlpha(0, 1, true);
            Task.Delay(layerTime).GetAwaiter().OnCompleted(() => Disable_LayerImg());
        }

        private void Disable_LayerImg()
        {
            pLayerImg.gameObject.SetActive(false);
        }

        public void ToggleVisiblity(GameObject target)
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
