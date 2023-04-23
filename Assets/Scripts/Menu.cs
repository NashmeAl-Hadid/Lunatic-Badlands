using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
 
[System.Serializable]
public class ProfileData
{
    public string username;
    public int level;
    public int xp;

    public ProfileData()
    {
        this.username = "DEFAULT USERNAME";
        this.level = 0;
        this.xp = 0;
    }

    public ProfileData(string u, int l, int x)
    {
        this.username = u;
        this.level = l;
        this.xp = x;
    }

    object[] ConvertToObjectArr()
    {
        object[] ret = new object[3];

        return ret;
    }
}

public class Menu : MonoBehaviourPunCallbacks
{

    public TMP_InputField usernameField;
    public TMP_InputField roonnameField;
    public Slider maxPlayerSlider;
    public TextMeshProUGUI maxPlayersValue;
    public static ProfileData myProfile = new ProfileData();

    public GameObject tabMain;
    public GameObject tabRooms;
    public GameObject tabCreate;

    public GameObject buttonRoom;

    private List<RoomInfo> roomList;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        //Over here the file from Data.cs is loaded into the player profile.
        myProfile = Data.LoadProfile();
        //We read the saved file data and set it to the username field for later use :3.
        usernameField.text = myProfile.username;

        Connect();
    }

    public override void OnConnectedToMaster()
    {
        //Debug.Log("CONNECTED!");

        //This allows us to connect to the server fully so we can create and access lobbies.
        PhotonNetwork.JoinLobby();
        base.OnConnectedToMaster();
    }

    public override void OnJoinedRoom()
    {
        StartGame();

        base.OnJoinedRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Create();

        base.OnJoinRandomFailed(returnCode, message);
    }

    public void Connect()
    {
        //Debug.Log("Trying to Connect...");
        PhotonNetwork.GameVersion = "0.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Join()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void Create()
    {
        //This makes the room and adds all the properies for it like lobby name, map, and max players.
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte) maxPlayerSlider.value;

        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        properties.Add("map", 0);
        options.CustomRoomProperties = properties;

        PhotonNetwork.CreateRoom(roonnameField.text, options);
    }

    public void ChangeMaxPlayerSlider (float t_value)
    {
        //Slider for setting max players for lobby.
        maxPlayersValue.text = Mathf.RoundToInt(t_value).ToString();
    }

    public void TabCloseAll()
    {
        tabMain.SetActive(false);
        tabRooms.SetActive(false);
        tabCreate.SetActive(false);
    }

    public void TabOpenMain()
    {
        TabCloseAll();
        tabMain.SetActive(true);
    }

    public void TabOpenRooms()
    {
        TabCloseAll();
        tabRooms.SetActive(true);
    }

    public void TabOpenCreate()
    {
        TabCloseAll();
        tabCreate.SetActive(true);
    }

    private void ClearRoomList()
    {
        //This is called to refresh the lobby list so it deletes previously made or ended lobbies.
        Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");
        foreach (Transform a in content) Destroy(a.gameObject);
    }

    private void VerifyUsername()
    {
        if(string.IsNullOrEmpty(usernameField.text))
        {
            myProfile.username = "RANDOM_USER_" + Random.Range(100, 1000);
        }
        else
        {
            myProfile.username = usernameField.text;
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> p_list)
    {
        //This is called to update the lobbies but only once we are connected.
            roomList = p_list;
            ClearRoomList();
        //We clear them here first

            //Debug.Log("LOADED ROOMS @ " + Time.time);
            Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");

            foreach (RoomInfo a in roomList)
            {
            //This shows the proper data from each lobby onto the list of lobbies with the correct updated information whenever someone leaves or enters the room (updates each time menu is loaded I reckon?).
                GameObject newRoomButton = Instantiate(buttonRoom, content) as GameObject;

                newRoomButton.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = a.Name;
                newRoomButton.transform.Find("Players").GetComponent<TextMeshProUGUI>().text = a.PlayerCount + " / " + a.MaxPlayers;

                newRoomButton.GetComponent<Button>().onClick.AddListener(delegate { JoinRoom(newRoomButton.transform); });
            }

        base.OnRoomListUpdate(roomList);
    }

    public void JoinRoom(Transform p_button)
    {
        //Debug.Log("JOINING ROOM @ " + Time.time);
        //Allows you to actually join the room by the end of it :3.
        string t_roomName = p_button.Find("Name").GetComponent<TextMeshProUGUI>().text;

        VerifyUsername();
        
        PhotonNetwork.JoinRoom(t_roomName);
    }

    public void StartGame()
    {
        //if(string.IsNullOrEmpty(usernameField.text))
        //{
        //    myProfile.username = "RANDOM_USER " + Random.Range(100, 1000);
        //}
        //else
        //{
        //    myProfile.username = usernameField.text;
        //}

        VerifyUsername();

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            //This ensures we save the file once we load into a level.
            Data.SaveProfile(myProfile);
            PhotonNetwork.LoadLevel(1);
        }
    }

}
