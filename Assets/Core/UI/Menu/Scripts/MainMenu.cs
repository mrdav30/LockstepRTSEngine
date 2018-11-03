using UnityEngine;
using System.Collections;
using RTSLockstep;
using UnityEngine.SceneManagement;

public class MainMenu : Menu
{

    #region MonoBehavior
    protected override void Start()
    {
        base.Start();
        Cursor.visible = true;
    }

    void OnEnable()
    {
        //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        Cursor.visible = true;
        if (RTSLockstep.PlayerManager.GetPlayerName() == null)
        {
            //no commander yet selected so enable SetPlayerMenu
            GetComponent<MainMenu>().enabled = false;
            GetComponent<SelectPlayerMenu>().enabled = true;
        }
        else
        {
            //commander selected so enable MainMenu
            GetComponent<MainMenu>().enabled = true;
            GetComponent<SelectPlayerMenu>().enabled = false;
        }
    }
    #endregion

    protected override void SetButtons()
    {
        buttons = new string[] { "New Game", "Load Game", "Change Player", "Quit Game" };
    }

    protected override void HandleButton(string text)
    {
        base.HandleButton(text);
        switch (text)
        {
            case "New Game":
                NewGame();
                break;
            case "Load Game":
                LoadGame();
                break;
            case "Change Player":
                ChangePlayer();
                break;
            case "Quit Game":
                ExitGame();
                break;
            default: break;
        }
    }

    private void NewGame()
    {
        ResourceManager.MenuOpen = false;
        SceneManager.LoadScene("SampleMap");
        //makes sure that the loaded level runs at normal speed
        Time.timeScale = 1.0f;
    }

    private void ChangePlayer()
    {
        GetComponent<MainMenu>().enabled = false;
        GetComponent<SelectPlayerMenu>().enabled = true;
        SelectionList.LoadEntries(RTSLockstep.PlayerManager.GetPlayerNames());
    }

    protected override void HideCurrentMenu()
    {
        GetComponent<MainMenu>().enabled = false;
    }
}