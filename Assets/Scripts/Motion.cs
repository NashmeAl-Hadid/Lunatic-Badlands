using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class Motion : MonoBehaviourPunCallbacks, IPunObservable
{

    #region Variables

    public float speed;
    public float sprintModifier;
    public float crouchModifier;
    public float slideModifier;
    public float jumpForce;
    public float jetForce;
    public float jetWait;
    public float jetRecovery;
    public float lengthOfSlide;
    public int max_health;
    public float max_fuel;
    public float throwForce = 40f;
    public Camera normalCam;
    public Camera weaponCam;
    public GameObject cameraParent;
    public GameObject grenadePrefab;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;

    [HideInInspector]public ProfileData playerProfile;
    public TextMeshPro playerUsername;

    public float slideAmount;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;

    private Transform ui_healthbar;
    private Transform ui_fuelbar;
    private TextMeshProUGUI ui_ammo;
    private TextMeshProUGUI ui_username;
    private TextMeshProUGUI ui_abilityTimer;
    private TextMeshProUGUI ui_grenadeTimer;
    [HideInInspector] public TextMeshProUGUI ui_health;

    [HideInInspector] public Rigidbody rig;

    private Vector3 targetWeaponBopPosition;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrentPos;

    private float movementCounter;
    private float idleCounter;

    [HideInInspector] public float baseFOV;
    private float sprintFOVModifier = 1.5f;
    private Vector3 origin;

    [HideInInspector] public int current_health;
    private float current_fuel;
    private float current_recovery;

    private Manager manager;
    private Weapon weapon;

    private bool crouched;

    private bool sliding;
    private float slide_time;
    private Vector3 slide_dir;

    private bool isAiming;

    private float aimAngle;
    private bool canJet;

    private Vector3 normalCamTarget;
    private Vector3 weaponCamTarget;

    #region New Variables

    public GameObject throwPos;
    //public AudioSource voice_source;
    public AudioClip[] voice_lines;
    public float voiceVolume;

    public float grenadeCooldown = 10f;
    public float abilityCooldown = 8f;

    public float grenadeTimer;
    public float abilityTimer;

    //string[] Powerups = { "Speed", "Jump", "Confuser" };

    #endregion

    #endregion

    #region New Variables

    public GameObject pickupEffect;
    public List<int> burnTickTimers = new List<int>();
    //public byte pointValue = 1;

    private GameObject grenadeThrown;
    private Molotov grenade;

    private bool canSlide = true;
    [HideInInspector] public bool startAim = false;
    [HideInInspector] public bool holdAim = false;
    [HideInInspector] public bool endAim = false;

    private float baseSpeed;
    private float baseJumpForce;
    private bool isBoosted = false;

    private float boostedSpeed;
    private float boostedJumpForce;
    private float reducedSpeed;
    private float reducedJumpForce;
    private bool isStunned = false;

    public List<GameObject> playerName = new List<GameObject>();

    #endregion 

    #region Photon Callbacks

    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            aimAngle = (int)p_stream.ReceiveNext() / 100f;
        }
    }

    #endregion

    #region Callbacks

    private void Start()
    {
        baseSpeed = speed;
        boostedSpeed = baseSpeed + 125;
        boostedJumpForce = jumpForce + 150;

        reducedSpeed = baseSpeed - 62.5f;
        reducedJumpForce = baseJumpForce - 75;

        playerName.AddRange(GameObject.FindGameObjectsWithTag("PlayerName"));

        manager = GameObject.Find("Manager").GetComponent<Manager>();

        weapon = GetComponent<Weapon>();
        current_health = max_health;
        current_fuel = max_fuel;
        cameraParent.SetActive(photonView.IsMine);

        if(!photonView.IsMine)
        {
            gameObject.layer = 13;
            standingCollider.layer = 13;
            crouchingCollider.layer = 13;
        }

        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        rig = GetComponent<Rigidbody>();

        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrentPos = weaponParentOrigin;

        if(photonView.IsMine)
        {
            ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
            ui_fuelbar = GameObject.Find("HUD/Fuel/Bar").transform;
            ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<TextMeshProUGUI>();
            ui_username = GameObject.Find("HUD/Username/Username Text").GetComponent<TextMeshProUGUI>();

            ui_abilityTimer = GameObject.Find("HUD/AbilityTimer/Text").GetComponent<TextMeshProUGUI>();
            ui_grenadeTimer = GameObject.Find("HUD/GrenadeTimer/Text").GetComponent<TextMeshProUGUI>();
            ui_health = GameObject.Find("HUD/Health/Text").GetComponent<TextMeshProUGUI>();

            RefreshHealthBar();
            RefreshHealth(ui_health);

            ui_username.text = Menu.myProfile.username;

            photonView.RPC("SyncProfile", RpcTarget.All, Menu.myProfile.username, Menu.myProfile.level, Menu.myProfile.xp);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }

        foreach (GameObject playerName in playerName)
        {
            Vector3 targetPosition = new Vector3(playerName.transform.position.x, transform.position.y, playerName.transform.position.z);
            playerName.transform.LookAt(targetPosition);
        }

        if (abilityTimer > 0)
        {
            abilityTimer -= Time.deltaTime;
        }

        if (abilityTimer < 0)
        {
            abilityTimer = 0;
        }

        if (grenadeTimer > 0)
        {
            grenadeTimer -= Time.deltaTime;
        }

        if (grenadeTimer < 0)
        {
            grenadeTimer = 0;
        }

        //Axes
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);
        holdAim = Input.GetMouseButton(1);
        startAim = Input.GetMouseButtonDown(1);
        endAim = Input.GetMouseButtonUp(1);

        //AIM NEEDS TO SLOW YOU DOWN SHITHEAD
        if (startAim == true)
        {
            speed = speed * weapon.currentGunData.weaponSlow / 100f;
        }
        if (endAim == true)
        {
            if (!isBoosted)
                speed = baseSpeed;

            else speed = boostedSpeed;
        }

        if (Input.GetKeyDown(KeyCode.E) && grenadeTimer == 0)
        {
            photonView.RPC("ThrowGrenade", RpcTarget.All);
            grenadeTimer = grenadeCooldown;
            RefreshGrenadeTimerUI();
        }

        if (gameObject.name == "Pyromaniac(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-500, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        } else if (gameObject.name == "Pyromaniac 1(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-500, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        }

        if (gameObject.name == "Floyd(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-250, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        } else if(gameObject.name == "Floyd 1(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-250, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        }

        if (gameObject.name == "UncleSam(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-250, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        } else if (gameObject.name == "UncleSam 1(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TakeDamage(-250, 1, -1);
                if (current_health > max_health)
                {
                    current_health = max_health;
                }
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        }

        if (gameObject.name == "Morrison(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                IEnumerator agilityBoost()
                {
                    jumpForce += 100f;
                    speed += 100f;
                    //baseFOV += 20f;

                    yield return new WaitForSeconds(5);

                    jumpForce -= 100f;
                    speed -= 100f;
                    //baseFOV -= 20f;
                }
                StartCoroutine(agilityBoost());
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        } else if (gameObject.name == "Morrison 1(Clone)" && abilityTimer == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                IEnumerator agilityBoost()
                {
                    jumpForce += 100f;
                    speed += 100f;
                    //baseFOV += 20f;

                    yield return new WaitForSeconds(5);

                    jumpForce -= 100f;
                    speed -= 100f;
                    //baseFOV -= 20f;
                }
                StartCoroutine(agilityBoost());
                abilityTimer = abilityCooldown;
                RefreshAbilityTimerUI();
            }
        }

        //States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.05f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        //Pause
        if (pause)
        {
            GameObject.Find("Pause Menu").GetComponent<Pause>().TogglePause();
        }

        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            crouch = false;
            pause = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isCrouching = false;
        }

        //Crouching
        if (crouch)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        //Jumping
        if (isJumping)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
            current_recovery = 0f;
        }

        //if(Input.GetKeyDown(KeyCode.U))
        //{
        //    TakeDamage(100, 1, -1);
        //}

        //Head Bob
        //float t_aim_adjust = 1f;

        if(!isGrounded) //Airborne
        {
            Headbob(idleCounter, 0.025f, 0.025f);
            idleCounter += 0;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 2f * 0.2f);
            StartCoroutine(CanJetTimer());
            //canJet = true;
        }

        else if(sliding) //Sliding
        {
            Headbob(movementCounter, 0.15f, 0.075f);
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 10f * 0.2f);
        }
        else if (t_hmove == 0 && t_vmove == 0) //Idling
        {
            Headbob(idleCounter, 0.025f, 0.025f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 2f * 0.2f);

        }
        else if(!isSprinting && !crouched) //Walking
        {
            Headbob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 2f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 8f * 0.2f);
        } 
        else if (crouched) //Crouching
        {
            Headbob(movementCounter, 0.01f, 0.02f);
            movementCounter += Time.deltaTime * 1.75f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 6f * 0.2f);
        }
        else //Sprinting
        {
            Headbob(movementCounter, 0.15f, 0.075f);
            movementCounter += Time.deltaTime * 7f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBopPosition, Time.deltaTime * 10f * 0.2f);
        }

        //UI Refreshes
        RefreshHealthBar();
        RefreshHealth(ui_health);
        weapon.RefreshAmmo(ui_ammo);
        RefreshAbilityTimerUI();
        RefreshGrenadeTimerUI();
    }

    IEnumerator CanJetTimer()
    {
        yield return new WaitForSeconds(0.3f);
        canJet = true;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        //Axes
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool slide = Input.GetKey(KeyCode.LeftControl);
        bool jet = Input.GetKey(KeyCode.Space);

        //States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0;
        bool isSliding = isSprinting && slide && !sliding;
        isAiming = holdAim && !isSliding && !isSprinting;

        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
        }

        //Movement
        Vector3 t_direction = Vector3.zero;
        float t_adjustSpeed = speed;

        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);

            if (isSprinting)
            {
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                if (isGrounded) t_adjustSpeed *= sprintModifier;
                else t_adjustSpeed = baseSpeed + 30;
            }
            else if (crouched)
            {
                t_adjustSpeed *= crouchModifier;
            }
        }
        else
        {
            t_direction = slide_dir;
            t_adjustSpeed *= slideModifier;
            slide_time -= Time.deltaTime;
            if (slide_time <= 0)
            {
                sliding = false;
                weaponParentCurrentPos -= Vector3.down * (slideAmount - crouchAmount);
            }
        }

        Vector3 t_targetVelocity = t_direction * t_adjustSpeed * Time.fixedDeltaTime;
        t_targetVelocity.y = rig.velocity.y;
        rig.velocity = t_targetVelocity;

        //Sliding
        if (isSliding && canSlide)
        {
            sliding = true;
            slide_dir = t_direction; 
            slide_time = lengthOfSlide;
            weaponParentCurrentPos += Vector3.up * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("SetCrouch", RpcTarget.All, true);

            //Adjust Camera
            weaponParentCurrentPos += Vector3.down * 0.5f;
            StartCoroutine(SlideTimer());
            canSlide = false;
        }

        IEnumerator SlideTimer()
        {
            yield return new WaitForSeconds(2.5f);
            canSlide = true;
        }

        //Jetting
        //if(!isGrounded)
        //{
        //    canJet = true;
        //    Debug.Log("YOU CAN JET!");
        //}
        if(isGrounded)
        {
            canJet = false;
        }

        if(canJet && jet && current_fuel > 0)
        {
            rig.AddForce(Vector3.up * jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            current_fuel = Mathf.Max(0, current_fuel - Time.fixedDeltaTime);
        }
        if(isGrounded)
        {
            if(current_recovery < jetWait)
            {
                current_recovery = Mathf.Min(jetWait, current_recovery + Time.fixedDeltaTime);
            } else
            {
                current_fuel = Mathf.Min(max_fuel, current_fuel + Time.fixedDeltaTime * jetRecovery);
            }
        }

        ui_fuelbar.localScale = new Vector3(current_fuel / max_fuel, 1, 1);

        //Aiming
        isAiming = weapon.Aim(isAiming);

        //Camera
        if (sliding)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * 0.5f, Time.deltaTime * 8f);

            weaponCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin + Vector3.down * 0.5f, Time.deltaTime * 8f);

            normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
            weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        }
        else
        {
            if (isSprinting)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
            }
            else if (isAiming)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 8f);
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
            }
            if (crouched)
            {
                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime);
            }
            else
            {
                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin, Time.deltaTime);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin, Time.deltaTime);
            }
        }
    }

    private void LateUpdate()
    {
        normalCam.transform.localPosition = normalCamTarget;
        weaponCam.transform.localPosition = weaponCamTarget;
    }

    #endregion

    #region Methods

    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }

    private void RefreshAbilityTimerUI()
    {
        ui_abilityTimer.text = abilityTimer.ToString("00");
    }

    private void RefreshGrenadeTimerUI()
    {
        ui_grenadeTimer.text = grenadeTimer.ToString("00");
    }

    public void RefreshHealth(TextMeshProUGUI ui_health)
    {
        int t_current = max_health;
        int t_max = current_health;

        ui_health.text = "Health: " + t_max.ToString() + " / " + t_current.ToString();
    }

    void Headbob(float p_z, float p_x_intensity, float p_y_intesntiy)
    {
        float t_aim_adjust = 1f;
        if (isAiming) t_aim_adjust = 0.1f;
        targetWeaponBopPosition = weaponParentCurrentPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intesntiy * t_aim_adjust, 0);
    }

    public void RefreshHealthBar()
    {
        float t_health_ration = (float)current_health / (float)max_health;
        ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ration, 1, 1), Time.deltaTime * 8f);
    }

    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp)
    {
        playerProfile = new ProfileData(p_username, p_level, p_xp);
        playerUsername.text = playerProfile.username;
    }

    [PunRPC]

    void SetCrouch (bool p_state)
    {
        if (crouched == p_state) return;

        crouched = p_state;

        if(crouched)
        {
            standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            weaponParentCurrentPos += Vector3.down * crouchAmount;
        }

        else
        {
            standingCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPos -= Vector3.down * crouchAmount;
        }
    }

    [PunRPC]
    public void PickupHealth()
    {
        current_health += 600;
    }

    [PunRPC]
    public void BoostSpeed()
    {
        StartCoroutine(Boost());
    }

    //[PunRPC]
    //public void DoublePoints()
    //{
    //    StartCoroutine(Double());
    //}


    [PunRPC]
    
    void ThrowGrenade()
    {
        grenadeThrown = Instantiate(grenadePrefab, throwPos.transform.position, throwPos.transform.rotation);
        grenade = grenadeThrown.GetComponent<Molotov>();
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        rb.AddForce(throwPos.transform.forward * throwForce, ForceMode.VelocityChange);
    }

    #endregion

    #region Public Methods

    public void TakeDamage(int p_damage, int p_burn, int p_actor)
    {
        if (photonView.IsMine)
        {
            current_health -= p_damage;
            RefreshHealthBar();
            RefreshHealth(ui_health);

            if (current_health <= 0)
            {
                int number = Random.Range(0, 1);
                //weapon.sfx.Stop();
                //weapon.sfx.clip = voice_lines[number];
                //weapon.sfx.Play();
                //Debug.Log("SOUND!" + voice_lines[number].name);

                manager.Spawn(Manager.playerSpawnNum);
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
                //StartCoroutine(SoundWait());
                AudioSource.PlayClipAtPoint(voice_lines[number], transform.position, 1000);
                //voice_source.volume = voiceVolume;

                if (p_actor >= 0)
                {
                    if (gameObject.layer == 11)
                    {
                        int value = Random.Range(2, 3);
                        AudioSource.PlayClipAtPoint(voice_lines[(value)], transform.position, 1000);
                        //voice_source.volume = voiceVolume;
                        manager.ChangeStat_S(p_actor, 0, 1);

                        //weapon.sfx.Stop();
                        //weapon.sfx.clip = voice_lines[value];
                        //weapon.sfx.Play();
                        //Debug.Log("SOUND!" + voice_lines[value].name);
                    }
                }

                PhotonNetwork.Destroy(gameObject);
            }
        }
        if (burnTickTimers.Count <= 0)
        {
            burnTickTimers.Add(p_burn);
            //while (burnTickTimers.Count > 0)
            //{
            //    for (int i = 0; i < burnTickTimers.Count; i++)
            //    {
            //        burnTickTimers[i]--;
            //    }
            //    Debug.Log("ASDF");
            //    gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, 900, 4, 50, PhotonNetwork.LocalPlayer.ActorNumber);
            //    RefreshHealthBar();
            //    burnTickTimers.RemoveAll(i => i == 0);
            //}
            StartCoroutine(Burn());
        }
        else
        {
            burnTickTimers.Add(p_burn);
        }
    }

    [PunRPC]

    public void ApplyBurn(int ticks)
    {
        if (burnTickTimers.Count <= 0)
        {
            burnTickTimers.Add(ticks);
            StartCoroutine(Burn());
        }
        else
        {
            burnTickTimers.Add(ticks);
        }
    }

    [PunRPC]

    public IEnumerator Burn()
    {
        if (photonView.IsMine)
        {
            if(grenade.willBurn == true) {
                while (burnTickTimers.Count > 0)
                {
                    for (int i = 0; i < burnTickTimers.Count; i++)
                    {
                       burnTickTimers[i]--;
                    }
                    gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, 50, 0, PhotonNetwork.LocalPlayer.ActorNumber);
                    RefreshHealthBar();
                    RefreshHealth(ui_health);
                    burnTickTimers.RemoveAll(i => i == 0);
                    yield return new WaitForSeconds(0.75f);
                }
            }
        }
    }

    [PunRPC]
    public IEnumerator SoundWait()
    {
        yield return new WaitForSeconds(1f);
    }

    [PunRPC]

    public IEnumerator ReturnEffects()
    {
        isStunned = true;
        speed = reducedSpeed;
        jumpForce = reducedJumpForce;
       //baseFOV -= 30;

        yield return new WaitForSeconds(5f);

        isStunned = false;
        speed += baseSpeed;
        jumpForce += baseJumpForce;
        //baseFOV += 30;
    }

    [PunRPC]

    public IEnumerator Boost()
    {
        isBoosted = true;
        speed = boostedSpeed;
        jumpForce = boostedJumpForce;
        //baseFOV += 30;

        yield return new WaitForSeconds(8f);

        isBoosted = false;
        speed = baseSpeed;
        jumpForce = baseJumpForce;
        //baseFOV -= 30;
    }

    //[PunRPC]

    //public IEnumerator Double()
    //{
    //    pointValue = 2;

    //    yield return new WaitForSeconds(15f);

    //    pointValue = 1;

    //}

    [PunRPC]

    public void Knockback()
    {
        rig.AddForce(transform.up * 500);
    }

    //    [PunRPC]
    //    IEnumerator OnTriggerEnter(Collider other)
    //    {
    //        if (other.gameObject.CompareTag("Powerup"))
    //        {
    //            Instantiate(pickupEffect, other.transform.position, other.transform.rotation);

    //            Destroy(other.gameObject);

    //            Debug.Log("jaiurjeiar");
    //        }

    //        else if (other.gameObject.CompareTag("Health"))
    //        {
    //            current_health += 500;



    //            Destroy(gameObject);
    //        }

    //        else if (other.gameObject.CompareTag("DoublePoints"))
    //        {
    //            jumpUpgrade.jump += 10f;

    //            yield return new WaitForSeconds(10);

    //            if (jumpUpgrade.jump != 5)
    //            {
    //                jumpUpgrade.jump -= 10f;
    //            }

    //            Destroy(gameObject);
    //        }
    //        else if (other.gameObject.CompareTag("DoubleDamage"))
    //        {
    //            Confuser.speed -= 17f;

    //            yield return new WaitForSeconds(10);

    //            if (Confuser.speed != 8.5)
    //            {
    //                Confuser.speed += 17f;
    //            }
    //            Destroy(gameObject);
    //        }
    //        else if (other.gameObject.CompareTag("Boost")) {


    //            yield return new WaitForSeconds(10);

    //            if(upgrade.effect != number)
    //            {
    //                upgrade.effect = total;
    //            }
    //            Destroy(gameObject);
    //        }
    //    }
    //}

    #endregion
}
