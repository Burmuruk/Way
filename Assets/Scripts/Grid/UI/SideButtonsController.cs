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
        [SerializeField] TextMeshProUGUI layerBox;
        Task layerTask;
        bool shouldHideLayer = false;

        private void Awake()
        {
            var grid = FindObjectOfType<GridController>();
            grid.OnLayerChanged += Show_layer;
            pLayerImg.gameObject.SetActive(false);
        }

        private void Show_layer(int layer)
        {
            if (layerTask != null && layerTask.Status == TaskStatus.Running)
            {
                shouldHideLayer = false;
            }
            else
                shouldHideLayer = true;


            pLayerImg.gameObject.SetActive(true);
            pLayerTxt.text = layer.ToString();
            pLayerImg.CrossFadeAlpha(0, 1, true);
            layerBox.text = pLayerTxt.text;
            
            layerTask = Task.Delay(layerTime);
            layerTask.GetAwaiter().OnCompleted(() => Disable_LayerImg());
        }

        private void Disable_LayerImg()
        {
            if (!shouldHideLayer)
            {
                shouldHideLayer = true;
                return;
            }

            pLayerImg.gameObject.SetActive(false);
            pLayerImg.CrossFadeAlpha(1, .05f, true);
        }

        public void ToggleVisiblity(GameObject target)
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
