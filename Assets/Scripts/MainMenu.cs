using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{

    public Menu launcher;

    private void Start()
    {
        Pause.paused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void JoinMatch()
    {
        launcher.Join();
    }

    public void CreateMatch()
    {
        launcher.Create();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayerSpawn1()
    {
        Manager.playerSpawnNum = 0;
    }

    public void PlayerSpawn2()
    {
        Manager.playerSpawnNum = 1;
    }

    public void PlayerSpawn3()
    {
        Manager.playerSpawnNum = 2;
    }

    public void PlayerSpawn4()
    {
        Manager.playerSpawnNum = 3;
    }

    public void PlayerSpawn5()
    {
        Manager.playerSpawnNum = 4;
    }

    public void PlayerSpawn6()
    {
        Manager.playerSpawnNum = 5;
    }

    public void PlayerSpawn7()
    {
        Manager.playerSpawnNum = 6;
    }

    public void PlayerSpawn8()
    {
        Manager.playerSpawnNum = 7;
    }

}
