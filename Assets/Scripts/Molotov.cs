using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Molotov : MonoBehaviour
{
    public float delay = 3f;
    public float radius = 5f;
    public bool willBurn = false;

    public AudioClip ExplosionSound;
    public AudioSource sfx;
    public float shotVolume;

    public GameObject explosionEffect;
    //public GameObject lingerEffect;

    float countdown;
    bool hasExploded = false;

    void Start()
    {
        countdown = delay;

        Debug.Log("Molotov Start!");
        //Motion playerController = gameObject.GetComponent<Motion>();
    }
    
    void Update()
    {
        countdown -= Time.deltaTime;
        if(countdown <= 0f && !hasExploded)
        {
            Explode();
            hasExploded = true;
        }
    }

    void Explode()
    {
        //Debug.Log("BOOM!");
        if (gameObject.CompareTag("Molotov"))
        {
            //Show effect
            Instantiate(explosionEffect, transform.position, transform.rotation);

            //Get nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider nearbyObject in colliders)
            {
                //Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                //GameObject Player = GameObject.FindGameObjectWithTag("Player");
                Motion playerScript = nearbyObject.gameObject.transform.root.GetComponent<Motion>();

                if (playerScript != null)
                {
                    //Damage
                    playerScript.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, 250, 4, PhotonNetwork.LocalPlayer.ActorNumber);

                    //playerScript.photonView.RPC("ApplyBurn", RpcTarget.All, 4);
                    //playerScript.current_health -= 150;

                    //Instantiate(lingerEffect, transform.position, transform.rotation);
                }

                else
                {
                    Debug.Log("PlayerScript not found!");
                }
                //Remove grenade
                Destroy(gameObject);
            }
        }

        if (gameObject.CompareTag("Grenade"))
        {
            //Show effect
            Instantiate(explosionEffect, transform.position, transform.rotation);

            //Sound
            sfx.clip = ExplosionSound;
            sfx.volume = shotVolume;
            sfx.Play();

            //Get nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider nearbyObject in colliders)
            {
                //Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                Motion playerScript = nearbyObject.gameObject.transform.root.GetComponent<Motion>();

                if(playerScript != null)
                {
                    //Damage
                    playerScript.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, 350, 0, PhotonNetwork.LocalPlayer.ActorNumber);
                    playerScript.RefreshHealthBar();
                    playerScript.RefreshHealth(playerScript.ui_health);
                }

                else
                {
                    Debug.Log("PlayerScript not found!");
                }
                //Remove grenade
                Destroy(gameObject);
            }
        }

        if (gameObject.CompareTag("Stun"))
        {
            //Show effect
            Instantiate(explosionEffect, transform.position, transform.rotation);

            //Get nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider nearbyObject in colliders)
            {
                //Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                Motion playerScript = nearbyObject.gameObject.transform.root.GetComponent<Motion>();

                if (playerScript != null)
                {
                    //if (!playerScript.photonView.IsMine)
                    //{
                        playerScript.photonView.RPC("ReturnEffects", RpcTarget.All);
                    //}
                }

                else
                {
                    Debug.Log("PlayerScript not found!");
                }
                //Remove grenade
                Destroy(gameObject);
            }
        }

        if (gameObject.CompareTag("Knockback"))
        {
            //Show effect
            Instantiate(explosionEffect, transform.position, transform.rotation);

            //Get nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider nearbyObject in colliders)
            {
                //Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                Motion playerScript = nearbyObject.gameObject.transform.root.GetComponent<Motion>();

                if (playerScript != null)
                {
                    playerScript.photonView.RPC("Knockback", RpcTarget.All);
                }

                else
                {
                    Debug.Log("PlayerScript not found!");
                }
                //Remove grenade
                Destroy(gameObject);
            }
        }

        if(gameObject.CompareTag("Smoke"))
        {
            //Show effect
            Instantiate(explosionEffect, transform.position, transform.rotation);

            Destroy(gameObject);
        }
    }

    [PunRPC]
    private void TakeDamage(int p_damage, int p_burn, int p_actor)
    {
        GetComponent<Motion>().TakeDamage(p_damage, p_burn, p_actor);
    }

    [PunRPC]

    public void ApplyBurn(int ticks)
    {
        GetComponent<Motion>().ApplyBurn(4);
    }

    [PunRPC]

    public IEnumerator Burn()
    {
        GetComponent<Motion>().Burn();
        yield return null;
    }

    [PunRPC]

    public IEnumerator ReturnEffects()
    {
        GetComponent<Motion>().ReturnEffects();
        yield return null;
    }

    [PunRPC]

    public void Knockback()
    {
        GetComponent<Motion>().Knockback();
    }

}
