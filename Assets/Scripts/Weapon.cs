﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{

    #region Variables

    public Gun[] loadout;
    [HideInInspector] public Gun currentGunData;

    public Transform weaponParent;
    public GameObject bulletHolePrefab;
    public LayerMask canBeShot;
    public AudioSource sfx;
    public AudioClip hitmarkerSound;
    public bool isAiming = false;

    private float currentCooldown;
    private int currentIndex;
    private GameObject currentWeapon;
    public GameObject sniperOverlay;

    private Image hitmarkerImage;
    private float hitmarkerWait;

    private bool isReloading;
    private bool isScoped = false;

    private Color CLEARWHITE = new Color(1, 1, 1, 0);

    //private GameObject particleEffect;

    #endregion

    #region Callbacks

    private void Start()
    {
        sniperOverlay = GameObject.Find("ScopeOverlay");
        foreach (Gun a in loadout) a.Initialise();
        hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
        hitmarkerImage.color = CLEARWHITE;
        Equip(0);
    }

    void Update()
    {
        if (Pause.paused && photonView.IsMine) return;

        if(photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            photonView.RPC("Equip", RpcTarget.All, 0);
        }

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("Equip", RpcTarget.All, 1);
        }

        if(photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2) && loadout[currentIndex].ToString() == "Machine Gun 2")
        {
            isScoped = !isScoped;
            sniperOverlay.SetActive(isScoped);
        }

        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                //Aim(Input.GetMouseButton(1));

                if (loadout[currentIndex].burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0)
                    {
                        if (loadout[currentIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                        else StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0)
                    {
                        if (loadout[currentIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                        else StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    StartCoroutine(Reload(loadout[currentIndex].reload));
                }
                //Cooldown
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }

            //Weapon Position Elasticity
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }

        if(photonView.IsMine)
        {
            if(hitmarkerWait > 0)
            {
                hitmarkerWait -= Time.deltaTime;
            }
            else if(hitmarkerImage.color.a > 0)
            {
                hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, CLEARWHITE, Time.deltaTime * 1.5f);
            }
        }
    }

    #endregion

    #region Methods

    IEnumerator Reload(float p_wait)
    {
        sfx.clip = currentGunData.reloadSound;
        sfx.Play();
        isReloading = true;
        currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);

        isReloading = false;
    }

    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null)
        {
            if(isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }
        currentIndex = p_ind;

        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        if (photonView.IsMine) ChangeLayersRecursively(t_newWeapon, 10);
        else ChangeLayersRecursively(t_newWeapon, 0);

        currentWeapon = t_newWeapon;
        currentGunData = loadout[p_ind];
    }

    private void ChangeLayersRecursively(GameObject p_target, int p_layer)
    {
        p_target.layer = p_layer;
        foreach (Transform a in p_target.transform) ChangeLayersRecursively(a.gameObject, p_layer);
    }

    public bool Aim(bool p_isAiming)
    {
        if (!currentWeapon) return false;
        if (isReloading) p_isAiming = false;

        isAiming = p_isAiming;
        Transform t_anchor = currentWeapon.transform.Find("Anchor");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

        if (p_isAiming)
        {
            //Aim
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            //Hip
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        return isAiming;
    }

    [PunRPC]
    void Shoot()
    {
        Transform t_spawn = transform.Find("Cameras/Player Camera");

        //Cooldown
        currentCooldown = loadout[currentIndex].firerate;

        for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
        {
            //Bloom
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();

            //Particle Effect
            //Instantiate(particleEffect, weaponParent.transform.position, weaponParent.transform.rotation);
            Debug.Log("PARTRICLE");

            //Raycast
            RaycastHit t_hit = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
            {
                GameObject t_newHole = Instantiate(bulletHolePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newHole, 5f);

                if (photonView.IsMine)
                {
                    //Shooting Other Player On Network
                    if (t_hit.collider.gameObject.layer == 13)
                    {
                        //RPC Call To Damage Player Goes Here
                        //Give Damage
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, 1, PhotonNetwork.LocalPlayer.ActorNumber);

                        //Show Hitmarker
                        hitmarkerImage.color = Color.white;
                        sfx.PlayOneShot(hitmarkerSound);
                        hitmarkerWait = 1f;
                    }

                    //Shooting Target
                    if (t_hit.collider.gameObject.layer == 12)
                    {
                        //Show Hitmarker
                        hitmarkerImage.color = Color.white;
                        sfx.PlayOneShot(hitmarkerSound);
                        hitmarkerWait = 1f;
                    }
                }
            }
        }

        //Sound
        //sfx.Stop();
        sfx.clip = currentGunData.gunshotSound;
        sfx.pitch = 1 - currentGunData.pitchRandomizaiton + Random.Range(-currentGunData.pitchRandomizaiton, currentGunData.pitchRandomizaiton);
        sfx.volume = currentGunData.shotVolume;
        sfx.Play();

        //Gun FX
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickBack;
        if(currentGunData.recovery)
        {
            //Animation
        }
    }

    [PunRPC]
    private void TakeDamage(int p_damage, int p_burn, int p_actor)
    {
        GetComponent<Motion>().TakeDamage(p_damage, p_burn, p_actor);
    }

    #endregion

    #region Public Methods

    public void RefreshAmmo(TextMeshProUGUI p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString() + " / " + t_stash.ToString();
    }

    #endregion

}
