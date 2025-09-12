using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xolito.Control;

namespace Xolito.Core
{
    public class PlayerManager : MonoBehaviour
    {
        #region AUDIO //Borrar si rompe algo
        MenuManager menuManager;


        private bool canCheck = false;

        #endregion

        #region Variables
        [Header("References")]
        [SerializeField] PlayerController[] players = default;

        bool[] playerAtEnd = new bool[2];
        float xDirection = 0;


        #endregion

        public bool EndReached { get => playerAtEnd[0] && playerAtEnd[1]; }

        private void Awake()
        {
            menuManager = GameObject.FindObjectOfType<MenuManager>();
        }

        private void Start()
        {
            canCheck = true;
        }

        void Update()
        {
            if (canCheck)
            {
                Check_Movement();
                Check_Jump();
                Check_Dash();
            }
        }

        private void OnEnable()
        {
            menuManager.onEnteredMenu += Disable_Inputs;
            menuManager.onExitedMenu += Enable_Inputs;
        }

        private void OnDisable()
        {
            menuManager.onEnteredMenu -= Disable_Inputs;
            menuManager.onExitedMenu -= Enable_Inputs;
        }

        private void Check_Dash()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                foreach (PlayerController player in players)
                {
                    player.Dash();
                }
            }
        }

        private void Check_Jump()
        {
            if (Input.GetButton("Jump"))
            {
                foreach (PlayerController player in players)
                {
                    player.Jump();
                }
            }
        }

        private void Check_Movement()
        {
            xDirection = Input.GetAxisRaw("Horizontal");


            if (xDirection != 0)
            {
                try
                {
                    players[0]?.Move(xDirection);
                    players[1]?.Move(-xDirection);


                }
                catch (System.Exception)
                {

                }
            }
            else
            {
                players[0]?.animatorXolos.SetBool("isMoving", false);
                players[1]?.animatorXolos.SetBool("isMoving", false);
            }
        }

        public void Respawn(Vector3 start1, Vector3 start2)
        {
            try
            {
                players[0].transform.position = start1;
                players[0].Clear_Velocity();
                players[0].CanMove = true;
                players[0].registerStop = true;
                players[1].transform.position = start2;
                players[1].Clear_Velocity();
                players[1].CanMove = true;
                players[1].registerStop = true;
            }
            catch (System.Exception)
            {

            }
        }

        public void RegisterEnd(PlayerController playerController) {
            for (int i = 0; i < players.Length; i++)
            {
                if (playerController == players[i]) {
                    playerAtEnd[i] = true;
                    players[i].CanMove = false;

                    if (!EndReached) {
                        int idx = i == 0 ? 1 : 0;
                        players[idx].CanMove = true;
                        players[idx].registerStop = false;
                    }
                }
            }

        }

        private void Disable_Inputs() => canCheck = false;
        private void Enable_Inputs() => canCheck = true;
    }
}