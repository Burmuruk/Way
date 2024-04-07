using System.Collections.Generic;
using UnityEngine;
//using TMPro;
using UnityEngine.SceneManagement;
using Xolito.Utilities;

namespace Xolito.Core
{
    public class LevelController : MonoBehaviour
    {
        PlayerManager manager1;

        [SerializeField] GameObject coinsCounter;
        [SerializeField] GameObject staminaCounter;
        [Space]
        [SerializeField] GameObject startPointUp;
        [SerializeField] GameObject endPointUp;
        [SerializeField] GameObject startPointDown;
        [SerializeField] GameObject endPointDown;
        [Space]
        [SerializeField] public GameObject[] coins;

        float currentCoins = 0;
        int currentLevel = 0;

        private void Awake()
        {
            manager1 = GameObject.FindObjectOfType<PlayerManager>();
        }

        public bool Change_NextLevel()
        {
            Change_Level(currentLevel + 1);
            return true;
        }

        private void Change_Level(int level)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            
            currentLevel = level;
        }

        public void Change_FirstLevel()
        {
            SceneManager.LoadScene(0);

         
        }

        public void Restart_Level()
        {
            foreach (GameObject coin in coins)
            {
                coin.SetActive(true);
            }

            Restart_Players();
        }

        public void Restart_Players()
        {
            var (p1, p2) = (
                Find_StartPoint(startPointUp, "White"),
                Find_StartPoint(startPointDown, "Black"));

            manager1.Respawn(p1, p2);

            Vector3 Find_StartPoint(GameObject point, string end)
            {
                var position = Vector3.zero;

                if (!point)
                {
                    var restult = GameObject.Find("StartPoint" + end);

                    if (restult != null) 
                        return restult.transform.position + Vector3.up * .3f;
                }
                else
                    return point.transform.position + Vector3.up * .3f;

                return Vector3.zero;
            }
        }

        public void AddStartPoint(GameObject point, ColorType color)
        {
            if (color == ColorType.Black)
                startPointDown = point;
            else if (color == ColorType.White)
                startPointUp = point;
        }

        public void Add_Coin()
        {
            currentCoins++;
            //coinsCounter.GetComponent<TMPro.TextMeshProUGUI>().text = coins.ToString();
        }
    }
}