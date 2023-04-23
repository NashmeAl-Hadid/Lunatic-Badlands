using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class PlayerInfo
{
    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;

    public PlayerInfo (ProfileData p, int a, short k, short d)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
    }
}

public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}

public class Manager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public int mainmenu = 0;
    public int killcount = 2;
    public int matchLength = 100;
    public bool perpetual = false;

    public GameObject mapcam;

    public string[] player_prefab_string;
    public GameObject player_prefab;
    public static int playerSpawnNum = 0;
    public Transform[] spawn_points;

    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;

    private TextMeshProUGUI ui_mykills;
    private TextMeshProUGUI ui_mydeaths;
    private TextMeshProUGUI ui_timer;
    private Transform ui_leaderboard;
    private Transform ui_endgame;

    private int currentMatchTime;
    private Coroutine timerCoroutine;

    private GameState state = GameState.Waiting;

    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat,
        NewMatch,
        RefreshTimer
    }

    private void Start()
    {
        mapcam.SetActive(false);

        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        NewPlayer_S(Menu.myProfile);
        Spawn(playerSpawnNum);
    }

    private void Update()
    {
        if(state == GameState.Ending)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if (ui_leaderboard.gameObject.activeSelf) ui_leaderboard.gameObject.SetActive(false);
            else Leaderboard(ui_leaderboard);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {
            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                break;

            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;

            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;

            case EventCodes.NewMatch:
                NewMatch_R();
                break;

            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainmenu);
    }

    public void Spawn(int playerNumber)
    {
        Transform t_spawn = spawn_points[Random.Range(0, spawn_points.Length)];
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(player_prefab_string[playerNumber], t_spawn.position, t_spawn.rotation);
        }
        else
        {
            Debug.Log("WORKING");
            GameObject newPlayer = Instantiate(player_prefab, t_spawn.position, t_spawn.rotation) as GameObject;
        }
    }

    private void InitializeUI()
    {
        ui_mykills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<TextMeshProUGUI>();
        ui_mydeaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<TextMeshProUGUI>();
        ui_timer = GameObject.Find("HUD/Timer/Text").GetComponent<TextMeshProUGUI>();
        ui_leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        ui_endgame = GameObject.Find("Canvas").transform.Find("End Game").transform;

        RefreshMyStats();
    }

    private void RefreshMyStats()
    {
        //Debug.Log(playerInfo.Count.ToString() + " " + myind.ToString() + " " + Time.time.ToString());

        if(playerInfo.Count > myind)
        {
            ui_mykills.text = $"{playerInfo[myind].kills} kills";
            ui_mydeaths.text = $"{playerInfo[myind].deaths} deaths";
        }
        else
        {
            ui_mykills.text = "0 kills";
            ui_mydeaths.text = "0 deaths";
        }
    }

    private void Leaderboard (Transform p_lb)
    {
        //Clean Up
        for (int i = 2; i < p_lb.childCount; i++)
        {
            Destroy(p_lb.GetChild(i).gameObject);
        }

        //Set Details
        p_lb.Find("Header/Mode").GetComponent<TextMeshProUGUI>().text = "Free For All";
        p_lb.Find("Header/Map").GetComponent<TextMeshProUGUI>().text = "Western Town";

        //Cache Prefab
        GameObject playercard = p_lb.GetChild(1).gameObject;
        playercard.SetActive(false);

        //Sort
        List<PlayerInfo> sorted = SortPlayers(playerInfo);

        //Display
        bool t_alternateColours = false;
        foreach(PlayerInfo a in sorted)
        {
            GameObject newcard = Instantiate(playercard, p_lb) as GameObject;

            if (t_alternateColours) newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
            t_alternateColours = !t_alternateColours;

            //newcard.transform.Find("Map").GetComponent<TextMeshProUGUI>().text = a.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = a.profile.username;
            //newcard.transform.Find("Score Value").GetComponent<TextMeshProUGUI>().text = (a.kills * 100).ToString();
            newcard.transform.Find("Kill Value").GetComponent<TextMeshProUGUI>().text = a.kills.ToString();
            newcard.transform.Find("Death Value").GetComponent<TextMeshProUGUI>().text = a.deaths.ToString();

            newcard.SetActive(true);
        }

        //Activate
        p_lb.gameObject.SetActive(true);
    }

    private List<PlayerInfo> SortPlayers (List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < p_info.Count)
        {
            //Set Defaults
            short highest = -1;
            PlayerInfo selection = p_info[0];

            //Grab Next Highest Player
            foreach (PlayerInfo a in p_info)
            {
                if (sorted.Contains(a)) continue;
                if(a.kills > highest)
                {
                    selection = a;
                    highest = a.kills;
                }
            }

            //Add Player
            sorted.Add(selection);
        }

        return sorted;
    }

    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(mainmenu);
    }

    private void StateCheck()
    {
        if(state == GameState.Ending)
        {
            EndGame();
        }
    }

    private void ScoreCheck()
    {
        //Define Temporary Variables
        bool detectwin = false;

        //Check to see if any player has met the win conditions.
        foreach (PlayerInfo player in playerInfo)
        {
           if(player.kills >= killcount)
            {
                detectwin = true;
                Debug.Log("good");
                break;
            }
        }

        //Did we find a winner?
        if(detectwin)
        {
            //Are we the master client? Is the game still going?
            if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                //If so, tell the other players that a winner has been detected.
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }

    private void InitializeTimer()
    {
        currentMatchTime = matchLength;
        RefreshTimerUI();

        if(PhotonNetwork.IsMasterClient)
        {
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        string minutes = (currentMatchTime / 60).ToString("00");
        string seconds = (currentMatchTime % 60).ToString("00");
        ui_timer.text = $"{minutes}:{seconds}";
    }

    private void EndGame()
    {
        //Set Game State To Ending
        state = GameState.Ending;

        //Set timer to 0
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentMatchTime = 0;
        RefreshTimerUI();

        //Disable Room
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();

            if (!perpetual)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }

        //Activate Map Camera
        mapcam.SetActive(true);

        //Show End Game UI
        ui_endgame.gameObject.SetActive(true);
        Leaderboard(ui_endgame.Find("Leaderboard"));

        //Wait X seconds and then return to main menu
        StartCoroutine(End(6f));
    }

    public void NewPlayer_S (ProfileData p)
    {
        object[] package = new object[6];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short) 0;
        package[5] = (short) 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    public void NewPlayer_R (object[] data)
    {
        PlayerInfo p = new PlayerInfo(
            new ProfileData(
                (string)data[0],
                (int)data[1],
                (int)data[2]
            ),
            (int)data[3],
            (short)data[4],
            (short)data[5]
        );

        playerInfo.Add(p);

        UpdatePlayers_S((int)state, playerInfo);
    }

    public void UpdatePlayers_S (int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];

        package[0] = state;
        for(int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[6];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent (
            (byte)EventCodes.UpdatePlayers,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
                );
    }

    public void UpdatePlayers_R (object[] data)
    {
        state = (GameState)data[0];
        playerInfo = new List<PlayerInfo>();

        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[]) data[i];

            PlayerInfo p = new PlayerInfo(
                new ProfileData(
                    (string)extract[0],
                    (int)extract[1],
                    (int)extract[2]
                ),
                (int)extract[3],
                (short)extract[4],
                (short)extract[5]
            );

            playerInfo.Add(p);

            if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor) myind = i - 1;
        }

        StateCheck();
    }

    public void ChangeStat_S (int actor, byte stat, byte amt)
    {
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ChangeStat_R (object[] data)
    {
        int actor = (int) data[0];
        byte stat = (byte) data[1];
        byte amt = (byte) data[2];

        for (int i = 0; i < playerInfo.Count; i++)
        {
            if(playerInfo[i].actor == actor)
            {
                switch(stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        break;

                    case 1: //deaths
                        playerInfo[i].deaths += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        break;
                }

                if (i == myind) RefreshMyStats();
                if (ui_leaderboard.gameObject.activeSelf) Leaderboard(ui_leaderboard);

                break;
            }
        }
        ScoreCheck();
        return;
    }

    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void NewMatch_R()
    {
        //Set Game State To Waiting
        state = GameState.Waiting;

        //Deactivate Map Camera
        mapcam.SetActive(false);

        //Hide End Game UI
        ui_endgame.gameObject.SetActive(false);

        //Reset Scores
        foreach(PlayerInfo p in playerInfo)
        {
            p.kills = 0;
            p.deaths = 0;
        }

        //Reset UI
        RefreshMyStats();

        //Reinitialize Time
        InitializeTimer();

        //Spawn
        Spawn(playerSpawnNum);
    }

    public void RefreshTimer_S()
    {
        object[] package = new object[] { currentMatchTime };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void RefreshTimer_R(object[] data)
    {
        currentMatchTime = (int)data[0];
        RefreshTimerUI();
    }

    private IEnumerator Timer ()
    {
        yield return new WaitForSeconds(1f);

        currentMatchTime -= 1;
        
        if(currentMatchTime <= 0)
        {
            timerCoroutine = null;
            UpdatePlayers_S((int)GameState.Ending, playerInfo);
        }
        else
        {
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private IEnumerator End (float p_wait)
    {
        yield return new WaitForSeconds(p_wait);

        if (perpetual)
        {
            //New Match
            if (PhotonNetwork.IsMasterClient)
            {
                NewMatch_S();
            }
        }
        else
        {
            //Disconnect
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }
}
