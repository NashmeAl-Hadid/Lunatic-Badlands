using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

    public class Pickup : MonoBehaviourPunCallbacks
    {
        public float cooldown;
        private bool isDisabled;
        private float wait;

        public GameObject healthDisplay;
        public List<GameObject> targets;

        private void Start()
        {
            foreach (Transform t in healthDisplay.transform) Destroy(t.gameObject);
            //GameObject newDisplay = Instantiate(healthDisplay, healthDisplay.transform.position, healthDisplay.transform.rotation) as GameObject;
            //newDisplay.transform.SetParent(healthDisplay.transform);
        }
        private void Update()
        {
            if (isDisabled)
            {
                if (wait > 0)
                {
                    wait -= Time.deltaTime;
                }
                else
                {
                    //reenable
                    Enabled();
                }
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Detected someone hit");
            if (other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
            {
                Debug.Log("Detected player");
                Motion playerController = other.attachedRigidbody.gameObject.GetComponent<Motion>(); // gets tank script
                
                if(gameObject.CompareTag("Heal"))
                {
                    playerController.photonView.RPC("PickupHealth", RpcTarget.All);
                    if (playerController.current_health > playerController.max_health)
                    {
                        playerController.current_health = playerController.max_health;
                    }
                    photonView.RPC("Disable", RpcTarget.All);
                }

                if(gameObject.CompareTag("Boost"))
                {
                    playerController.photonView.RPC("Boost", RpcTarget.All);
                    photonView.RPC("Disable", RpcTarget.All);
                }

                //if(gameObject.CompareTag("Double"))
                //{
                //    playerController.photonView.RPC("DoublePoints", RpcTarget.All);
                //    photonView.RPC("Disable", RpcTarget.All);
                //}
            }
        }

    [PunRPC]
        public void Disable()
        {
            isDisabled = true;
            wait = cooldown;

            foreach (GameObject a in targets) a.SetActive(false);
        }

        private void Enabled()
        {
            isDisabled = false;
            wait = 0;
            foreach (GameObject a in targets) a.SetActive(true);
        }
    }
