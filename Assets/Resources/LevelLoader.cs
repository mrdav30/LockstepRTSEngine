using UnityEngine;
using RTSLockstep;
using UnityEngine.SceneManagement;
using RTSLockstep;
using RTSLockstep;

/**
 * Singleton that handles loading level details. This includes making sure
 * that all world objects have an objectId set.
 */

public class LevelLoader : MonoSingleton<LevelLoader>
{
    #region Properties
    private bool initialised = false;
    #endregion

    #region MonoBehavior
    void Awake()
    {
        if (this != Instance)
        {
            return;
        }
        initialised = true;
        SelectPlayerMenu menu = GameObject.FindObjectOfType(typeof(SelectPlayerMenu)) as SelectPlayerMenu;
        if (!menu)
        {
        //    //we have started from inside a map, rather than the main menu
        //    //this happens if we launch Unity from inside a map file for testing
        //    AgentCommander[] players = GameObject.FindObjectsOfType(typeof(Player)) as AgentCommander[];
        //    foreach (Player commander in players)
        //    {
        //        if (commander.human)
        //        {
        //            RTSLockstep.PlayerManager.SelectPlayer(commander.username, 0, 0);
        //        }
        //    }
        //    SetObjectIds();
        }
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
        if (initialised)
        {
            if (ResourceManager.LevelName != null && ResourceManager.LevelName != "")
            {
                LoadManager.LoadGame(ResourceManager.LevelName);
            }
            //else
            //{
            //    SetObjectIds();
            //}
            Time.timeScale = 1.0f;
            ResourceManager.MenuOpen = false;
        }
    }
    #endregion
}