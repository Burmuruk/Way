using System;
using System.Collections;
using System.Drawing.Text;
using Unity.Loading;
using UnityEditor;
using UnityEngine;
using Xolito.Control;
using Xolito.Movement;

namespace Xolito.Core
{
    public class InteractableObject : MonoBehaviour
    {
        [SerializeField] Interaction interaction = Interaction.None;

        public event Action OnInteract;

        public Interaction Interaction { get => interaction; set => interaction = value; }

        //private void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if (collision.transform.name.Contains("Xolito"))
        //    {
        //        LevelController level = GameObject.FindObjectOfType<LevelController>();

        //        if (interaction == Interaction.Damage)
        //        {
        //            level.Restart_Level();
        //        }
        //        else if (interaction == Interaction.EndPoint)
        //        {
        //            level.Change_NextLevel();
        //        }
        //        else if (interaction == Interaction.Coin)
        //        {
        //            level.Add_Coin();
        //            gameObject.SetActive(false);
        //        }
        //    }
        //}

       
        private void OnCollisionStay2D(Collision2D collision) {
            if (collision.transform.name.Contains("Xolito")){

              if (interaction == Interaction.Checkpoint) {
                if (DetectPlayerAbove(collision.collider) == true) {
                    collision.collider.GetComponent<PlayerController>().DisablePlayerMovement();
                }
                //collision.GetComponent<PlayerController>().canMove = false;
              }

            }
            
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.name.Contains("Xolito"))
            {
                LevelController level = GameObject.FindObjectOfType<LevelController>();

                if (interaction == Interaction.Damage)
                {
                    level.Restart_Level();
                }
                else if (interaction == Interaction.EndPoint)
                {
                    if (FindObjectOfType<PlayerManager>() is var pm && pm != null) {
                        if (pm.EndReached)
                            level.Change_NextLevel();
                        else 
                            pm.RegisterEnd(collision.GetComponent<PlayerController>());

                        if (pm.EndReached)
                            level.Change_NextLevel();
                    }
                }
                else if (interaction == Interaction.Coin)
                {
                    level.Add_Coin();
                }
               
                else if(interaction == Interaction.JumpPad) {
                    collision.GetComponent<Mover>().JumpPad();
                }
                OnInteract?.Invoke();
            }

        }
        private bool DetectPlayerAbove(Collider2D xolito) {
            var collider = GetComponent<Collider2D>();
            var newPos = new Vector2(collider.bounds.center.x, collider.bounds.center.y + (collider.bounds.extents.y + collider.bounds.extents.x /2));
                var names = Physics2D.OverlapBoxAll(newPos, .02f * Vector2.one, 0);

            foreach (var colName in names) {
                if(colName == xolito) {
                    var dis = (xolito.bounds.center - (collider.bounds.center + (collider.bounds.extents.x * Vector3.one))).x;
                    dis = MathF.Abs(dis);
                    
                    if (dis < .4f || dis > .5f) return false;
                    return true;
                }
            } 
            return false;

        }
    }
}