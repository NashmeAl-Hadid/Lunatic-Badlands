﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{

    public string name;
    public int damage;
    public int ammo;
    public int clipsize;
    public float firerate;
    public float bloom;
    public float weaponSlow;
    public float recoil;
    public float kickBack;
    public float aimSpeed;
    public float reload;
    public int burst; // 0 semi | 1 auto | 2+ burst fire
    public int pellets;
    [Range(0, 1)] public float mainFOV;
    [Range(0, 1)] public float weaponFOV;
    public AudioClip gunshotSound;
    public AudioClip reloadSound;
    public float pitchRandomizaiton;
    public float shotVolume;
    public GameObject prefab;
    public bool recovery;

    private int stash; //current ammo
    private int clip; //current clip


    public void Initialise()
    {
        stash = ammo;
        clip = clipsize;
    }

    public bool FireBullet()
    {
        if(clip > 0)
        {
            clip -= 1;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Reload()
    {
        stash += clip;
        clip = Mathf.Min(clipsize, stash);
        stash -= clip;
    }

    public int GetStash()
    {
        return stash;
    }
    public int GetClip()
    {
        return clip;
    }
}
