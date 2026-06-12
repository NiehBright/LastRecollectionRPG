using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

namespace MuryotaisuDoor
{  
    public class MuryotaisuDoorOpen : MonoBehaviour
    {
        private Animator animator;

        private bool InDoor;

        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.zKey.isPressed)
            {
                animator.SetBool("doorOpenFlag", true);
            } else {
                animator.SetBool("doorOpenFlag", false);
            }

            if (InDoor)
            {
                GetComponent<Renderer>().material.color = Color.red;
                animator.SetBool("doorOpenFlag", true);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                InDoor = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                GetComponent<Renderer>().material.color = Color.yellow;
                InDoor = false;
            }
        }

    }
}